using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace SmartSkills.Core.Scanning;

/// <summary>
/// Resolves Java packages from Maven pom.xml files.
/// </summary>
public sealed class MavenPomPackageResolver(ILogger<MavenPomPackageResolver> logger) : IPackageResolver
{
    public async Task<ProjectPackages> ResolvePackagesAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(projectPath);

        var pomPath = projectPath.EndsWith("pom.xml", StringComparison.OrdinalIgnoreCase)
            ? projectPath
            : Path.Combine(projectPath, "pom.xml");

        if (!File.Exists(pomPath))
            throw new FileNotFoundException($"pom.xml not found: {pomPath}");

        var pomText = await File.ReadAllTextAsync(pomPath, cancellationToken).ConfigureAwait(false);
        var packages = ParsePom(pomText);

        logger.LogInformation("Resolved {Count} Maven packages from {Path}", packages.Count, pomPath);
        return new ProjectPackages(pomPath, packages);
    }

    /// <summary>
    /// Parse a pom.xml and extract dependency information.
    /// </summary>
    internal static List<ResolvedPackage> ParsePom(string pomXml)
    {
        var doc = XDocument.Parse(pomXml);
        var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;
        var properties = ParseProperties(doc.Root, ns);
        var packages = new List<ResolvedPackage>();

        // Get dependencies from <dependencies> section (not inside <dependencyManagement>)
        var dependenciesSection = doc.Root?
            .Elements(ns + "dependencies")
            .FirstOrDefault();

        if (dependenciesSection is null)
            return packages;

        foreach (var dep in dependenciesSection.Elements(ns + "dependency"))
        {
            var groupId = ResolveProperty(dep.Element(ns + "groupId")?.Value?.Trim(), properties);
            var artifactId = ResolveProperty(dep.Element(ns + "artifactId")?.Value?.Trim(), properties);
            var version = ResolveProperty(dep.Element(ns + "version")?.Value?.Trim(), properties) ?? "";
            var scope = dep.Element(ns + "scope")?.Value?.Trim()?.ToLowerInvariant();
            var type = dep.Element(ns + "type")?.Value?.Trim()?.ToLowerInvariant();

            if (string.IsNullOrEmpty(groupId) || string.IsNullOrEmpty(artifactId))
                continue;

            // Skip BOM imports (type=pom, scope=import)
            if (type == "pom" && scope == "import")
                continue;

            packages.Add(new ResolvedPackage
            {
                Name = $"{groupId}:{artifactId}",
                Version = version,
                IsTransitive = false,
                Ecosystem = Ecosystems.Java
            });
        }

        return packages;
    }

    /// <summary>
    /// Parse the &lt;properties&gt; section of a pom.xml into a dictionary.
    /// </summary>
    internal static Dictionary<string, string> ParseProperties(XElement? root, XNamespace ns)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var propsElement = root?.Element(ns + "properties");
        if (propsElement is null)
            return result;

        foreach (var prop in propsElement.Elements())
        {
            result[prop.Name.LocalName] = prop.Value.Trim();
        }

        return result;
    }

    /// <summary>
    /// Resolve ${property.name} references in a string value.
    /// </summary>
    internal static string? ResolveProperty(string? value, Dictionary<string, string> properties)
    {
        if (value is null)
            return null;

        // Match ${...} patterns
        while (value.Contains("${", StringComparison.Ordinal))
        {
            var start = value.IndexOf("${", StringComparison.Ordinal);
            var end = value.IndexOf('}', start);
            if (end < 0)
                break;

            var propName = value[(start + 2)..end];
            if (properties.TryGetValue(propName, out var propValue))
            {
                value = string.Concat(value.AsSpan(0, start), propValue, value.AsSpan(end + 1));
            }
            else
            {
                break; // Unresolvable property, stop
            }
        }

        return value;
    }
}
