#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    // Unity's .NET profile lacks this type, which the compiler needs for records and init-only setters.
    internal static class IsExternalInit
    {
    }
}
#endif
