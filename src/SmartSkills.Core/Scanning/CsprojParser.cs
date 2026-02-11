using System.Xml.Linq;
using SmartSkills.Core.Models;

namespace SmartSkills.Core.Scanning;

public class CsprojParser
{
    /// <summary>
    /// Parses a .csproj file and extracts PackageReference elements.
    /// Supports Central Package Management via Directory.Packages.props.
    /// </summary>
    public IReadOnlyList<DetectedPackage> Parse(string csprojPath)
    {
        var doc = XDocument.Load(csprojPath);
        var centralVersions = LoadCentralPackageVersions(Path.GetDirectoryName(csprojPath)!);

        var packages = new List<DetectedPackage>();

        foreach (var element in doc.Descendants("PackageReference"))
        {
            var name = element.Attribute("Include")?.Value;
            if (string.IsNullOrWhiteSpace(name))
                continue;

            // Version can be an attribute or a child element
            var version = element.Attribute("Version")?.Value
                          ?? element.Element("Version")?.Value;

            // Fall back to Central Package Management
            if (version is null && centralVersions.TryGetValue(name, out var centralVersion))
            {
                version = centralVersion;
            }

            packages.Add(new DetectedPackage(name, version));
        }

        return packages;
    }

    /// <summary>
    /// Parses a .sln file, discovers all referenced .csproj files, and parses each.
    /// </summary>
    public IReadOnlyList<DetectedPackage> ParseSolution(string slnPath)
    {
        var slnDir = Path.GetDirectoryName(slnPath)!;
        var csprojPaths = ParseSolutionForProjects(slnPath);

        var allPackages = new Dictionary<string, DetectedPackage>(StringComparer.OrdinalIgnoreCase);
        foreach (var relativePath in csprojPaths)
        {
            var fullPath = Path.GetFullPath(Path.Combine(slnDir, relativePath));
            if (!File.Exists(fullPath))
                continue;

            foreach (var pkg in Parse(fullPath))
            {
                // Keep latest version if duplicates found
                allPackages[pkg.Name] = pkg;
            }
        }

        return allPackages.Values.ToList();
    }

    private static IEnumerable<string> ParseSolutionForProjects(string slnPath)
    {
        foreach (var line in File.ReadLines(slnPath))
        {
            // Project lines: Project("{GUID}") = "Name", "path.csproj", "{GUID}"
            if (!line.StartsWith("Project(", StringComparison.Ordinal))
                continue;

            var parts = line.Split('"');
            // parts[5] is the relative path
            if (parts.Length >= 6 && parts[5].EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                yield return parts[5].Replace('\\', Path.DirectorySeparatorChar);
            }
        }
    }

    private static Dictionary<string, string> LoadCentralPackageVersions(string projectDir)
    {
        var versions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Walk up directories looking for Directory.Packages.props
        var dir = projectDir;
        while (dir is not null)
        {
            var propsPath = Path.Combine(dir, "Directory.Packages.props");
            if (File.Exists(propsPath))
            {
                var doc = XDocument.Load(propsPath);
                foreach (var pv in doc.Descendants("PackageVersion"))
                {
                    var name = pv.Attribute("Include")?.Value;
                    var version = pv.Attribute("Version")?.Value ?? pv.Element("Version")?.Value;
                    if (name is not null && version is not null && !versions.ContainsKey(name))
                    {
                        versions[name] = version;
                    }
                }
                break; // Only use the closest Directory.Packages.props
            }
            dir = Path.GetDirectoryName(dir);
        }

        return versions;
    }
}
