using System.Globalization;
using System.Resources;

namespace PangyaAPI.UpdateList.Localization;

internal static class UpdateListStrings
{
    private static readonly ResourceManager ResourceManager = new(
        "PangyaAPI.UpdateList.Localization.Strings",
        typeof(UpdateListStrings).Assembly);

    internal static string Crc32InputStreamMustNotBeNull =>
        GetString(nameof(Crc32InputStreamMustNotBeNull));

    internal static string Crc32DataBufferMustNotBeNull =>
        GetString(nameof(Crc32DataBufferMustNotBeNull));

    internal static string UpdateMakerTargetDirectoryNotFound =>
        GetString(nameof(UpdateMakerTargetDirectoryNotFound));

    internal static string UpdateMakerOutputPathMustIncludeDirectory =>
        GetString(nameof(UpdateMakerOutputPathMustIncludeDirectory));

    internal static string UpdateMakerInvalidZipPackageName =>
        GetString(nameof(UpdateMakerInvalidZipPackageName));

    internal static string UpdateMakerZipPackageCollision =>
        GetString(nameof(UpdateMakerZipPackageCollision));

    internal static string UpdateReaderFileNotFound =>
        GetString(nameof(UpdateReaderFileNotFound));

    internal static string UpdateReaderEncryptedListEmptyOrTruncated =>
        GetString(nameof(UpdateReaderEncryptedListEmptyOrTruncated));

    internal static string UpdateReaderDecryptedListMissingCompleteXml =>
        GetString(nameof(UpdateReaderDecryptedListMissingCompleteXml));

    internal static string UpdateWriterNoChangesToSave =>
        GetString(nameof(UpdateWriterNoChangesToSave));

    internal static string UpdateWriterGeneratedSuccessfully =>
        GetString(nameof(UpdateWriterGeneratedSuccessfully));

    internal static string UpdateKeyDetectorKeyDetected =>
        GetString(nameof(UpdateKeyDetectorKeyDetected));

    internal static string UpdateKeyDetectorNoKnownKey =>
        GetString(nameof(UpdateKeyDetectorNoKnownKey));

    internal static string UpdateKeyDetectorPlainTextReadyToEncrypt =>
        GetString(nameof(UpdateKeyDetectorPlainTextReadyToEncrypt));

    internal static string UpdateListVerifierDirectoryIsRooted =>
        GetString(nameof(UpdateListVerifierDirectoryIsRooted));

    internal static string UpdateListVerifierEntryLeafFilenameUnsafe =>
        GetString(nameof(UpdateListVerifierEntryLeafFilenameUnsafe));

    internal static string UpdateListVerifierPathEscapesRoot =>
        GetString(nameof(UpdateListVerifierPathEscapesRoot));

    internal static string UpdateListVerifierReparsePoint =>
        GetString(nameof(UpdateListVerifierReparsePoint));

    internal static string Format(string format, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, format, args);

    private static string GetString(string name) =>
        ResourceManager.GetString(name, CultureInfo.CurrentUICulture)
        ?? throw new MissingManifestResourceException();
}
