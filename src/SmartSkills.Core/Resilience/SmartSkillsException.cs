namespace SmartSkills.Core.Resilience;

/// <summary>
/// Structured error with code and remediation suggestion.
/// </summary>
public sealed class SmartSkillsException : Exception
{
    public string ErrorCode { get; }
    public string? Remediation { get; }

    public SmartSkillsException(string errorCode, string message, string? remediation = null, Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        Remediation = remediation;
    }

    public static class Codes
    {
        public const string NetworkError = "SS001";
        public const string AuthenticationFailed = "SS002";
        public const string SdkNotFound = "SS003";
        public const string ConfigInvalid = "SS004";
        public const string SkillValidationFailed = "SS005";
        public const string RegistryNotFound = "SS006";
        public const string SkillNotFound = "SS007";
        public const string InstallFailed = "SS008";
    }

    public static SmartSkillsException NetworkError(Exception inner) =>
        new(Codes.NetworkError, $"Network error: {inner.Message}",
            "Check your internet connection and proxy settings.", inner);

    public static SmartSkillsException AuthFailed(string provider) =>
        new(Codes.AuthenticationFailed, $"Authentication failed for {provider}.",
            "Check your credentials. Set the appropriate environment variable (e.g., SMARTSKILLS_GITHUB_PAT).");

    public static SmartSkillsException SdkNotFound() =>
        new(Codes.SdkNotFound, "The .NET SDK was not found.",
            "Install the .NET SDK from https://dot.net/download");
}
