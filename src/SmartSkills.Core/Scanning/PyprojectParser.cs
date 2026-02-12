using Tomlyn;
using Tomlyn.Model;

namespace SmartSkills.Core.Scanning;

/// <summary>
/// Parses pyproject.toml to extract direct dependency names.
/// Supports PEP 621 [project.dependencies] and Poetry [tool.poetry.dependencies].
/// </summary>
public static class PyprojectParser
{
    /// <summary>
    /// Returns normalized direct dependency names from pyproject.toml content.
    /// </summary>
    public static HashSet<string> ParseDirectDependencies(string tomlContent)
    {
        ArgumentNullException.ThrowIfNull(tomlContent);

        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var model = Toml.ToModel(tomlContent);

        // PEP 621: [project.dependencies]
        if (model.TryGetValue("project", out var projectObj) && projectObj is TomlTable projectTable)
        {
            if (projectTable.TryGetValue("dependencies", out var depsObj) && depsObj is TomlArray depsArray)
            {
                foreach (var dep in depsArray)
                {
                    if (dep is string depStr)
                    {
                        var name = ExtractPackageName(depStr);
                        if (!string.IsNullOrWhiteSpace(name))
                            result.Add(PypiNameNormalizer.Normalize(name));
                    }
                }
            }

            // [project.optional-dependencies]
            if (projectTable.TryGetValue("optional-dependencies", out var optObj) && optObj is TomlTable optTable)
            {
                foreach (var group in optTable.Values)
                {
                    if (group is TomlArray groupArray)
                    {
                        foreach (var dep in groupArray)
                        {
                            if (dep is string depStr)
                            {
                                var name = ExtractPackageName(depStr);
                                if (!string.IsNullOrWhiteSpace(name))
                                    result.Add(PypiNameNormalizer.Normalize(name));
                            }
                        }
                    }
                }
            }
        }

        // Poetry: [tool.poetry.dependencies]
        if (model.TryGetValue("tool", out var toolObj) && toolObj is TomlTable toolTable)
        {
            if (toolTable.TryGetValue("poetry", out var poetryObj) && poetryObj is TomlTable poetryTable)
            {
                ExtractPoetryDeps(poetryTable, "dependencies", result);
                ExtractPoetryDeps(poetryTable, "dev-dependencies", result);

                // Poetry groups: [tool.poetry.group.*.dependencies]
                if (poetryTable.TryGetValue("group", out var groupObj) && groupObj is TomlTable groupTable)
                {
                    foreach (var group in groupTable.Values)
                    {
                        if (group is TomlTable groupDepsTable)
                            ExtractPoetryDeps(groupDepsTable, "dependencies", result);
                    }
                }
            }
        }

        return result;
    }

    private static void ExtractPoetryDeps(TomlTable parent, string key, HashSet<string> result)
    {
        if (!parent.TryGetValue(key, out var depsObj) || depsObj is not TomlTable depsTable)
            return;

        foreach (var name in depsTable.Keys)
        {
            if (string.Equals(name, "python", StringComparison.OrdinalIgnoreCase))
                continue;

            result.Add(PypiNameNormalizer.Normalize(name));
        }
    }

    /// <summary>
    /// Extracts the package name from a PEP 508 dependency specifier.
    /// E.g. "azure-identity>=1.0,&lt;2.0" → "azure-identity"
    ///      "requests[security]>=2.0" → "requests"
    /// </summary>
    internal static string ExtractPackageName(string pep508Spec)
    {
        var span = pep508Spec.AsSpan().Trim();
        var end = 0;
        while (end < span.Length && span[end] != '>' && span[end] != '<' && span[end] != '='
               && span[end] != '!' && span[end] != '~' && span[end] != '[' && span[end] != ';'
               && span[end] != ' ')
        {
            end++;
        }
        return span[..end].ToString().Trim();
    }
}
