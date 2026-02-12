namespace SmartSkills.Core.Scanning;

/// <summary>
/// Configuration options for project detection behavior.
/// </summary>
public record ProjectDetectionOptions
{
    /// <summary>Whether to recursively search subdirectories. Default: false.</summary>
    public bool Recursive { get; init; }

    /// <summary>
    /// Maximum directory depth to traverse when Recursive is true.
    /// 0 = current directory only, 1 = immediate children, etc. Default: 5.
    /// </summary>
    public int MaxDepth { get; init; } = 5;
}
