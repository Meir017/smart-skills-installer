namespace SmartSkills.Core;

/// <summary>
/// Structured error with error code, description, and suggested action.
/// </summary>
public class SmartSkillsException : Exception
{
    public string ErrorCode { get; }
    public string Suggestion { get; }

    public SmartSkillsException(string errorCode, string message, string suggestion, Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        Suggestion = suggestion;
    }

    public override string ToString() => $"[{ErrorCode}] {Message}\nSuggestion: {Suggestion}";
}

public static class ErrorCodes
{
    public const string AuthFailed = "SMSK001";
    public const string RegistryNotFound = "SMSK002";
    public const string NetworkError = "SMSK003";
    public const string InvalidManifest = "SMSK004";
    public const string ChecksumMismatch = "SMSK005";
    public const string InvalidConfig = "SMSK006";
    public const string ProjectNotFound = "SMSK007";
    public const string SkillNotFound = "SMSK008";
}
