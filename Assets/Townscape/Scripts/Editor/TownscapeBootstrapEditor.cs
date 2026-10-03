using Townscape.Runtime;
using UnityEditor;
using UnityEngine;

namespace Townscape.Editor
{
    [CustomEditor(typeof(TownscapeBootstrap))]
    internal sealed class TownscapeBootstrapEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            var bootstrap = (TownscapeBootstrap)target;
            var town = bootstrap.Town;
            if (town != null)
            {
                var stats = town.GroundStats;
                EditorGUILayout.HelpBox(
                    $"{town.Meshes.Count} meshes. Ground: {stats.TopTriangles} triangles, {stats.StepFaces} kerb and wall faces.",
                    MessageType.Info);
            }

            if (GUILayout.Button("Rebuild Town"))
            {
                bootstrap.Rebuild();
                SceneView.RepaintAll();
            }
        }
    }
}
