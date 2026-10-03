using System;
using System.Collections.Generic;
using System.Numerics;
using Townscape.Generation.Geometry;
using Townscape.Generation.Maths;

namespace Townscape.Generation.Ground
{
    /// <summary>Counters that tests use to check the mesh is well formed.</summary>
    public sealed class GroundMeshStats
    {
        public int SnappedVertices { get; internal set; }

        public int TopTriangles { get; internal set; }

        public int HoleTriangles { get; internal set; }

        /// <summary>Triangles that snapping turned upside down (they are flipped back, but should be rare).</summary>
        public int FlippedTriangles { get; internal set; }

        public int StepFaces { get; internal set; }
    }

    /// <summary>
    /// Builds the detailed village ground as one continuous, chunked mesh.
    /// </summary>
    /// <remarks>
    /// <list type="number">
    /// <item>Lay a square grid over the core. Where a grid edge crosses a region boundary (kerb
    /// line, river wall, path edge), snap its nearer end onto the boundary, so boundaries run
    /// along vertices and come out straight.</item>
    /// <item>Split each cell into two triangles, choosing the diagonal that follows an edge.</item>
    /// <item>Classify each triangle by its centroid and give its corners that region's heights.</item>
    /// <item>Wherever neighbouring triangles disagree on height, fill the gap with a vertical
    /// face: kerbs, verge edges and embankment walls all fall out of this one rule.</item>
    /// </list>
    /// </remarks>
    public sealed class GroundMeshGenerator
    {
        public const float SkirtBottom = -6f;

        private const float SnapFraction = 0.75f;
        private const float StepThreshold = 0.004f;

        private readonly GroundModel _model;
        private readonly GenerationSettings _settings;

        public GroundMeshGenerator(GroundModel model, GenerationSettings settings)
        {
            _model = model;
            _settings = settings;
        }

        public GroundMeshStats Stats { get; } = new GroundMeshStats();

        public IReadOnlyList<MeshData> Generate()
        {
            var cell = _settings.CellSize;
            var cells = (int)MathF.Round(_settings.CoreHalfExtent * 2f / cell);
            var origin = new Vector2(-_settings.CoreHalfExtent, -_settings.CoreHalfExtent);
            var grid = new Grid(cells, origin, cell);

            PlaceVertices(grid);
            BuildTriangles(grid);

            var chunksPerSide = (cells + _settings.ChunkCells - 1) / _settings.ChunkCells;
            var builders = new MeshBuilder[chunksPerSide * chunksPerSide];
            for (var i = 0; i < builders.Length; i++)
            {
                builders[i] = new MeshBuilder();
            }

            MeshBuilder ChunkFor(int ci, int cj) =>
                builders[((cj / _settings.ChunkCells) * chunksPerSide) + (ci / _settings.ChunkCells)];

            EmitTops(grid, ChunkFor);
            EmitSteps(grid, ChunkFor);
            EmitSkirts(grid, ChunkFor);

            var meshes = new List<MeshData>();
            for (var cj = 0; cj < chunksPerSide; cj++)
            {
                for (var ci = 0; ci < chunksPerSide; ci++)
                {
                    var builder = builders[(cj * chunksPerSide) + ci];
                    if (!builder.IsEmpty)
                    {
                        meshes.Add(builder.Build($"Ground {ci},{cj}"));
                    }
                }
            }

            return meshes;
        }

        private void PlaceVertices(Grid grid)
        {
            var vertexCount = (grid.Cells + 1) * (grid.Cells + 1);
            var regions = new ISurfaceRegion[vertexCount];
            var hits = new ContourHit[vertexCount];
            var hasHit = new bool[vertexCount];
            var snap = new bool[vertexCount];
            var snapDistance = grid.CellSize * SnapFraction;

            for (var j = 0; j <= grid.Cells; j++)
            {
                for (var i = 0; i <= grid.Cells; i++)
                {
                    var index = grid.Vertex(i, j);
                    var p = grid.Origin + new Vector2(i * grid.CellSize, j * grid.CellSize);
                    grid.Positions[index] = p;
                    regions[index] = _model.Classify(p);
                    hasHit[index] = _model.TryNearestContour(p, snapDistance, out hits[index]);
                }
            }

            // Every grid edge whose ends lie in different regions crosses a boundary. Snapping the
            // end nearer that boundary onto it guarantees the boundary runs along mesh vertices
            // rather than zig-zagging across cells.
            void ConsiderEdge(int a, int b)
            {
                if (ReferenceEquals(regions[a], regions[b]))
                {
                    return;
                }

                var aDistance = hasHit[a] ? hits[a].Distance : float.MaxValue;
                var bDistance = hasHit[b] ? hits[b].Distance : float.MaxValue;
                if (aDistance <= bDistance && hasHit[a])
                {
                    snap[a] = true;
                }
                else if (hasHit[b])
                {
                    snap[b] = true;
                }
            }

            for (var j = 0; j <= grid.Cells; j++)
            {
                for (var i = 0; i <= grid.Cells; i++)
                {
                    if (i < grid.Cells)
                    {
                        ConsiderEdge(grid.Vertex(i, j), grid.Vertex(i + 1, j));
                    }

                    if (j < grid.Cells)
                    {
                        ConsiderEdge(grid.Vertex(i, j), grid.Vertex(i, j + 1));
                    }
                }
            }

            for (var j = 0; j <= grid.Cells; j++)
            {
                for (var i = 0; i <= grid.Cells; i++)
                {
                    var index = grid.Vertex(i, j);
                    var onBorder = i == 0 || j == 0 || i == grid.Cells || j == grid.Cells;
                    grid.Contour[index] = -1;
                    if (snap[index] && !onBorder)
                    {
                        grid.Positions[index] = hits[index].Point;
                        grid.Contour[index] = hits[index].ContourId;
                        Stats.SnappedVertices++;
                    }

                    grid.BaseHeights[index] = _model.PointAt(grid.Positions[index]).BaseHeight;
                }
            }
        }

        private void BuildTriangles(Grid grid)
        {
            for (var j = 0; j < grid.Cells; j++)
            {
                for (var i = 0; i < grid.Cells; i++)
                {
                    var v00 = grid.Vertex(i, j);
                    var v10 = grid.Vertex(i + 1, j);
                    var v11 = grid.Vertex(i + 1, j + 1);
                    var v01 = grid.Vertex(i, j + 1);
                    var cellIndex = grid.CellIndex(i, j);
                    var first = cellIndex * 2;

                    if (UseRisingDiagonal(grid, v00, v10, v11, v01))
                    {
                        // Diagonal v00-v11. Triangle 0 owns the left and top sides, triangle 1 the right and bottom.
                        grid.Triangles[first] = MakeTriangle(grid, v00, v01, v11);
                        grid.Triangles[first + 1] = MakeTriangle(grid, v00, v11, v10);
                        grid.Sides[cellIndex] = new CellSides(first, first + 1, first + 1, first, v00, v11);
                    }
                    else
                    {
                        // Diagonal v10-v01. Triangle 0 owns the left and bottom sides, triangle 1 the top and right.
                        grid.Triangles[first] = MakeTriangle(grid, v00, v01, v10);
                        grid.Triangles[first + 1] = MakeTriangle(grid, v01, v11, v10);
                        grid.Sides[cellIndex] = new CellSides(first, first + 1, first, first + 1, v10, v01);
                    }
                }
            }
        }

        private static bool UseRisingDiagonal(Grid grid, int v00, int v10, int v11, int v01)
        {
            if (grid.Contour[v00] >= 0 && grid.Contour[v00] == grid.Contour[v11])
            {
                return true;
            }

            if (grid.Contour[v10] >= 0 && grid.Contour[v10] == grid.Contour[v01])
            {
                return false;
            }

            return Vector2.DistanceSquared(grid.Positions[v00], grid.Positions[v11])
                <= Vector2.DistanceSquared(grid.Positions[v10], grid.Positions[v01]);
        }

        private Triangle MakeTriangle(Grid grid, int a, int b, int c)
        {
            var pa = grid.Positions[a];
            var pb = grid.Positions[b];
            var pc = grid.Positions[c];
            var centroid = (pa + pb + pc) / 3f;
            var region = _model.Classify(centroid);

            return new Triangle(
                a,
                b,
                c,
                region,
                region.TopAt(centroid),
                region.HeightAt(new GroundPoint(pa, grid.BaseHeights[a])),
                region.HeightAt(new GroundPoint(pb, grid.BaseHeights[b])),
                region.HeightAt(new GroundPoint(pc, grid.BaseHeights[c])));
        }

        private void EmitTops(Grid grid, Func<int, int, MeshBuilder> chunkFor)
        {
            for (var j = 0; j < grid.Cells; j++)
            {
                for (var i = 0; i < grid.Cells; i++)
                {
                    var builder = chunkFor(i, j);
                    var first = grid.CellIndex(i, j) * 2;
                    for (var t = first; t < first + 2; t++)
                    {
                        var triangle = grid.Triangles[t];
                        if (triangle.Region.Kind == RegionKind.Hole)
                        {
                            Stats.HoleTriangles++;
                            continue;
                        }

                        var a = Lift(grid, triangle.A, triangle.HeightA);
                        var b = Lift(grid, triangle.B, triangle.HeightB);
                        var c = Lift(grid, triangle.C, triangle.HeightC);
                        if (Vector3.Cross(b - a, c - a).Y < 0f)
                        {
                            Stats.FlippedTriangles++;
                        }

                        builder.AddTriangleFacing(a, b, c, Vector3.UnitY, triangle.Top);
                        Stats.TopTriangles++;
                    }
                }
            }
        }

        private void EmitSteps(Grid grid, Func<int, int, MeshBuilder> chunkFor)
        {
            for (var j = 0; j < grid.Cells; j++)
            {
                for (var i = 0; i < grid.Cells; i++)
                {
                    var builder = chunkFor(i, j);
                    var sides = grid.Sides[grid.CellIndex(i, j)];
                    var first = grid.CellIndex(i, j) * 2;

                    // The cell's own diagonal.
                    EmitStep(grid, builder, first, first + 1, sides.DiagonalA, sides.DiagonalB);

                    // Shared with the cell to the left.
                    if (i > 0)
                    {
                        var leftSides = grid.Sides[grid.CellIndex(i - 1, j)];
                        EmitStep(grid, builder, sides.Left, leftSides.Right, grid.Vertex(i, j), grid.Vertex(i, j + 1));
                    }

                    // Shared with the cell below.
                    if (j > 0)
                    {
                        var belowSides = grid.Sides[grid.CellIndex(i, j - 1)];
                        EmitStep(grid, builder, sides.Bottom, belowSides.Top, grid.Vertex(i, j), grid.Vertex(i + 1, j));
                    }
                }
            }
        }

        private void EmitStep(Grid grid, MeshBuilder builder, int triangleA, int triangleB, int p, int q)
        {
            var a = grid.Triangles[triangleA];
            var b = grid.Triangles[triangleB];
            if (a.Region.Kind == RegionKind.Hole || b.Region.Kind == RegionKind.Hole)
            {
                return;
            }

            var aP = a.HeightOf(p);
            var aQ = a.HeightOf(q);
            var bP = b.HeightOf(p);
            var bQ = b.HeightOf(q);
            if (MathF.Abs(aP - bP) < StepThreshold && MathF.Abs(aQ - bQ) < StepThreshold)
            {
                return;
            }

            var aIsUpper = aP + aQ >= bP + bQ;
            var upper = aIsUpper ? a : b;
            var lower = aIsUpper ? b : a;

            var pFlat = grid.Positions[p];
            var qFlat = grid.Positions[q];
            var topP = GeoMath.At(pFlat, MathF.Max(aP, bP));
            var topQ = GeoMath.At(qFlat, MathF.Max(aQ, bQ));
            var bottomP = GeoMath.At(pFlat, MathF.Min(aP, bP));
            var bottomQ = GeoMath.At(qFlat, MathF.Min(aQ, bQ));

            // Face the lower side: that is where the wall or kerb is seen from.
            var towardsLower = grid.Centroid(lower) - ((pFlat + qFlat) * 0.5f);
            var facing = new Vector3(towardsLower.X, 0f, towardsLower.Y);

            builder.AddQuadFacing(topP, topQ, bottomQ, bottomP, facing, StepMaterial(upper.Region, lower.Region));
            Stats.StepFaces++;
        }

        private static SurfaceMaterial StepMaterial(ISurfaceRegion upper, ISurfaceRegion lower)
        {
            // Dropping straight into the river from anything but a sloping bank: an embankment wall.
            if (lower.Kind == RegionKind.RiverBed && upper.Kind != RegionKind.RiverBank)
            {
                return SurfaceMaterial.Stone;
            }

            return upper.Side;
        }

        private void EmitSkirts(Grid grid, Func<int, int, MeshBuilder> chunkFor)
        {
            var last = grid.Cells - 1;
            for (var k = 0; k < grid.Cells; k++)
            {
                EmitSkirt(grid, chunkFor(0, k), grid.Sides[grid.CellIndex(0, k)].Left, grid.Vertex(0, k), grid.Vertex(0, k + 1), -Vector3.UnitX);
                EmitSkirt(grid, chunkFor(last, k), grid.Sides[grid.CellIndex(last, k)].Right, grid.Vertex(grid.Cells, k), grid.Vertex(grid.Cells, k + 1), Vector3.UnitX);
                EmitSkirt(grid, chunkFor(k, 0), grid.Sides[grid.CellIndex(k, 0)].Bottom, grid.Vertex(k, 0), grid.Vertex(k + 1, 0), -Vector3.UnitZ);
                EmitSkirt(grid, chunkFor(k, last), grid.Sides[grid.CellIndex(k, last)].Top, grid.Vertex(k, grid.Cells), grid.Vertex(k + 1, grid.Cells), Vector3.UnitZ);
            }
        }

        private static void EmitSkirt(Grid grid, MeshBuilder builder, int triangleIndex, int p, int q, Vector3 outward)
        {
            var triangle = grid.Triangles[triangleIndex];
            if (triangle.Region.Kind == RegionKind.Hole)
            {
                return;
            }

            var pFlat = grid.Positions[p];
            var qFlat = grid.Positions[q];
            builder.AddQuadFacing(
                GeoMath.At(pFlat, triangle.HeightOf(p)),
                GeoMath.At(qFlat, triangle.HeightOf(q)),
                GeoMath.At(qFlat, SkirtBottom),
                GeoMath.At(pFlat, SkirtBottom),
                outward,
                triangle.Top);
        }

        private static Vector3 Lift(Grid grid, int vertex, float height) => GeoMath.At(grid.Positions[vertex], height);

        private readonly struct Triangle
        {
            public Triangle(int a, int b, int c, ISurfaceRegion region, SurfaceMaterial top, float heightA, float heightB, float heightC)
            {
                A = a;
                B = b;
                C = c;
                Region = region;
                Top = top;
                HeightA = heightA;
                HeightB = heightB;
                HeightC = heightC;
            }

            public int A { get; }

            public int B { get; }

            public int C { get; }

            public ISurfaceRegion Region { get; }

            public SurfaceMaterial Top { get; }

            public float HeightA { get; }

            public float HeightB { get; }

            public float HeightC { get; }

            public float HeightOf(int vertex)
            {
                if (vertex == A)
                {
                    return HeightA;
                }

                if (vertex == B)
                {
                    return HeightB;
                }

                if (vertex == C)
                {
                    return HeightC;
                }

                throw new ArgumentException($"Vertex {vertex} is not part of this triangle.", nameof(vertex));
            }
        }

        /// <summary>Which of a cell's two triangles touches each side, plus the diagonal's endpoints.</summary>
        private readonly struct CellSides
        {
            public CellSides(int left, int right, int bottom, int top, int diagonalA, int diagonalB)
            {
                Left = left;
                Right = right;
                Bottom = bottom;
                Top = top;
                DiagonalA = diagonalA;
                DiagonalB = diagonalB;
            }

            public int Left { get; }

            public int Right { get; }

            public int Bottom { get; }

            public int Top { get; }

            public int DiagonalA { get; }

            public int DiagonalB { get; }
        }

        private sealed class Grid
        {
            public Grid(int cells, Vector2 origin, float cellSize)
            {
                Cells = cells;
                Origin = origin;
                CellSize = cellSize;
                var vertexCount = (cells + 1) * (cells + 1);
                Positions = new Vector2[vertexCount];
                BaseHeights = new float[vertexCount];
                Contour = new int[vertexCount];
                Triangles = new Triangle[cells * cells * 2];
                Sides = new CellSides[cells * cells];
            }

            public int Cells { get; }

            public Vector2 Origin { get; }

            public float CellSize { get; }

            public Vector2[] Positions { get; }

            public float[] BaseHeights { get; }

            public int[] Contour { get; }

            public Triangle[] Triangles { get; }

            public CellSides[] Sides { get; }

            public int Vertex(int i, int j) => (j * (Cells + 1)) + i;

            public int CellIndex(int i, int j) => (j * Cells) + i;

            public Vector2 Centroid(Triangle triangle) =>
                (Positions[triangle.A] + Positions[triangle.B] + Positions[triangle.C]) / 3f;
        }
    }
}
