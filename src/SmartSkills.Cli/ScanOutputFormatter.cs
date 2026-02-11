using System.Text.Json;
using SmartSkills.Core.Models;

namespace SmartSkills.Cli;

public static class ScanOutputFormatter
{
    public static void WriteTable(IReadOnlyList<DetectedPackage> packages, TextWriter writer)
    {
        if (packages.Count == 0)
        {
            writer.WriteLine("No packages detected.");
            return;
        }

        var nameWidth = Math.Max("Package Name".Length, packages.Max(p => p.Name.Length));
        var versionWidth = Math.Max("Version".Length, packages.Max(p => (p.Version ?? "N/A").Length));
        const int typeWidth = 10; // "Transitive".Length

        var header = $"{"Package Name".PadRight(nameWidth)}  {"Version".PadRight(versionWidth)}  {"Type".PadRight(typeWidth)}";
        writer.WriteLine(header);
        writer.WriteLine(new string('-', header.Length));

        foreach (var pkg in packages.OrderBy(p => p.IsTransitive).ThenBy(p => p.Name))
        {
            var type = pkg.IsTransitive ? "Transitive" : "Direct";
            writer.WriteLine($"{pkg.Name.PadRight(nameWidth)}  {(pkg.Version ?? "N/A").PadRight(versionWidth)}  {type.PadRight(typeWidth)}");
        }

        var directCount = packages.Count(p => !p.IsTransitive);
        var transitiveCount = packages.Count(p => p.IsTransitive);
        writer.WriteLine();
        writer.WriteLine($"Total: {packages.Count} package(s) ({directCount} direct, {transitiveCount} transitive)");
    }

    public static void WriteJson(IReadOnlyList<DetectedPackage> packages, TextWriter writer)
    {
        var output = packages.Select(p => new
        {
            name = p.Name,
            version = p.Version,
            type = p.IsTransitive ? "transitive" : "direct"
        });

        var json = JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true });
        writer.WriteLine(json);
    }
}
