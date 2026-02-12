namespace SmartSkills.Core.Installation;

/// <summary>
/// Orchestrates the full skill installation pipeline.
/// </summary>
public interface ISkillInstaller
{
    Task<InstallResult> InstallAsync(InstallOptions options, CancellationToken cancellationToken = default);
    Task<RestoreResult> RestoreAsync(string projectPath, CancellationToken cancellationToken = default);
    Task UninstallAsync(string skillName, CancellationToken cancellationToken = default);
}
