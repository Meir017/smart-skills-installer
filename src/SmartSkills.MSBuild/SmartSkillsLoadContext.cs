using System.Reflection;
using System.Runtime.Loader;

namespace SmartSkills.MSBuild;

/// <summary>
/// Custom AssemblyLoadContext that isolates SmartSkills MSBuild task dependencies
/// from the host MSBuild process, preventing version conflicts.
/// </summary>
internal sealed class SmartSkillsLoadContext : AssemblyLoadContext
{
    private readonly string _dependencyDir;
    private readonly AssemblyDependencyResolver _resolver;

    public SmartSkillsLoadContext(string taskAssemblyPath) : base("SmartSkills", isCollectible: true)
    {
        _dependencyDir = Path.GetDirectoryName(taskAssemblyPath) ?? string.Empty;
        _resolver = new AssemblyDependencyResolver(taskAssemblyPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Let MSBuild-provided assemblies load from the default context
        if (IsMSBuildAssembly(assemblyName.Name))
            return null;

        // Try the dependency resolver first (uses .deps.json)
        var resolvedPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (resolvedPath != null)
            return LoadFromAssemblyPath(resolvedPath);

        // Fallback: look in the task's directory
        var candidate = Path.Combine(_dependencyDir, $"{assemblyName.Name}.dll");
        if (File.Exists(candidate))
            return LoadFromAssemblyPath(candidate);

        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var resolvedPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return resolvedPath != null
            ? LoadUnmanagedDllFromPath(resolvedPath)
            : IntPtr.Zero;
    }

    private static bool IsMSBuildAssembly(string? name)
    {
        if (name is null) return false;
        return name.StartsWith("Microsoft.Build", StringComparison.OrdinalIgnoreCase)
            || name.Equals("System.Runtime", StringComparison.OrdinalIgnoreCase);
    }
}
