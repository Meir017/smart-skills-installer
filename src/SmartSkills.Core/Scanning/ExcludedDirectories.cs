namespace SmartSkills.Core.Scanning;

/// <summary>
/// Well-known directory names that should be skipped during recursive traversal.
/// </summary>
public static class ExcludedDirectories
{
    /// <summary>Version control and IDE directories.</summary>
    public static IReadOnlySet<string> Universal { get; } = CreateSet(
        ".git", ".hg", ".svn", ".vs", ".vscode", ".idea");

    /// <summary>.NET build output directories.</summary>
    public static IReadOnlySet<string> DotNet { get; } = CreateSet(
        "bin", "obj");

    /// <summary>Node.js ecosystem directories.</summary>
    public static IReadOnlySet<string> NodeJs { get; } = CreateSet(
        "node_modules", ".next", ".nuxt", "bower_components");

    /// <summary>Python ecosystem directories.</summary>
    public static IReadOnlySet<string> Python { get; } = CreateSet(
        "venv", ".venv", "__pycache__", ".tox", ".mypy_cache", ".pytest_cache", "site-packages");

    /// <summary>Java ecosystem directories.</summary>
    public static IReadOnlySet<string> Java { get; } = CreateSet(
        "target", ".gradle", "build");

    /// <summary>Generic build output directories.</summary>
    public static IReadOnlySet<string> BuildOutput { get; } = CreateSet(
        "dist", "vendor", "coverage", ".cache");

    /// <summary>Combined set of all excluded directory names (case-insensitive).</summary>
    public static IReadOnlySet<string> All { get; } = CreateCombinedSet();

    private static HashSet<string> CreateSet(params string[] names) =>
        new(names, StringComparer.OrdinalIgnoreCase);

    private static HashSet<string> CreateCombinedSet()
    {
        var all = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        all.UnionWith(Universal);
        all.UnionWith(DotNet);
        all.UnionWith(NodeJs);
        all.UnionWith(Python);
        all.UnionWith(Java);
        all.UnionWith(BuildOutput);
        return all;
    }
}
