namespace PangyaAPI.UpdateList.Models;

public enum UpdateFileVerificationStatus
{
    Match,
    ChecksumMismatch,
    Missing,
    InvalidPath,
    ReadError
}
