using System.Reflection;

namespace FileDeployment.Extensions
{
    internal static class AssemblyExtensions
    {
        /// <summary>
        /// Gets the types in an assembly, skipping any that can't be loaded instead of throwing.
        /// Some loaded assemblies, like native library wrappers, contain types that can't load in every process.
        /// </summary>
        public static IEnumerable<Type> GetLoadableTypes(this Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.OfType<Type>();
            }
        }
    }
}
