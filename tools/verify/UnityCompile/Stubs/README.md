Signature-only stand-ins for package types (URP, Input System) that are not available as NuGet
reference assemblies. They exist only so `Townscape.UnityCompile` can type-check our scripts; Unity
never sees them. Keep them minimal and mirror the real package API exactly.
