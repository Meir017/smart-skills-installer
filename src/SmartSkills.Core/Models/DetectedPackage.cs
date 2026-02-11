namespace SmartSkills.Core.Models;

public record DetectedPackage(string Name, string? Version, bool IsTransitive = false);
