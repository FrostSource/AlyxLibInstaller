namespace FileDeployment;

/// <summary>
/// Represents the result of a <see cref="ValidationRule"/> evaluation.
/// </summary>
public class ValidationResult
{
    public bool Success { get; private set; }
    public string? Message { get; private set; }

    public ValidationResult(bool success, string? message = null)
    {
        Success = success;
        Message = message;
    }

    public static ValidationResult Pass() => new(true);
    public static ValidationResult Fail(string? message = null) => new(false, message);

    public static implicit operator ValidationResult(bool success) => new(success);
    public static implicit operator ValidationResult((bool success, string? message) result)
        => new(result.success, result.message);
    public static implicit operator ValidationResult(string message) => new(false, message);
}
