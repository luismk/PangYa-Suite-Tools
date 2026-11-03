namespace PangyaAPI.UpdateList.Models;

public sealed record UpdateFileVerificationResult(
    UpdateEntry Entry,
    string? FullPath,
    UpdateFileVerificationStatus Status,
    int? ActualCrc,
    string? ErrorMessage);
