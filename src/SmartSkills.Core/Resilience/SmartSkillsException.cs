namespace SmartSkills.Core.Resilience;

/// <summary>
/// Structured error with code and remediation suggestion.
/// </summary>
public sealed class SmartSkillsException : Exception
{
    public string ErrorCode { get; }
    public string? Remediation { get; }

    public SmartSkillsException() : base()
    {
        ErrorCode = string.Empty;
    }

    public SmartSkillsException(string message) : base(message)
    {
        ErrorCode = string.Empty;
    }

    public SmartSkillsException(string message, Exception innerException) : base(message, innerException)
    {
        ErrorCode = string.Empty;
    }

    public SmartSkillsException(string errorCode, string message, string? remediation = null, Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        Remediation = remediation;
    }

    internal static class Codes
    {
        public const string NetworkError = "SS001";
        public const string AuthenticationFailed = "SS002";
        public const string SdkNotFound = "SS003";
        public const string SkillValidationFailed = "SS004";
        public const string RegistryNotFound = "SS005";
        public const string SkillNotFound = "SS006";
        public const string InstallFailed = "SS007";
    }

    public static SmartSkillsException NetworkError(Exception inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        return new(Codes.NetworkError, $"Network error: {inner.Message}",
            "Check your internet connection and proxy settings.", inner);
    }

    public static SmartSkillsException AuthFailed(string provider) =>
        new(Codes.AuthenticationFailed, $"Authentication failed for {provider}.",
            provider == "ado"
                ? "Ensure you are logged in with 'az login' or have valid Azure credentials."
                : "Ensure the repository is public or check your access permissions.");

    public static SmartSkillsException SdkNotFound() =>
        new(Codes.SdkNotFound, "The .NET SDK was not found.",
            "Install the .NET SDK from https://dot.net/download");
}
