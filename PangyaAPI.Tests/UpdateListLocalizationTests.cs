using System.Collections;
using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;
using PangyaAPI.UpdateList.Models;

namespace PangyaAPI.Tests;

[CollectionDefinition("UpdateList localization", DisableParallelization = true)]
public sealed class UpdateListLocalizationCollection
{
    public const string Name = "UpdateList localization";
}

[Collection(UpdateListLocalizationCollection.Name)]
public sealed class UpdateListLocalizationTests
{
    private static readonly string[] ExpectedKeys =
    [
        "Crc32DataBufferMustNotBeNull",
        "Crc32InputStreamMustNotBeNull",
        "UpdateKeyDetectorKeyDetected",
        "UpdateKeyDetectorNoKnownKey",
        "UpdateKeyDetectorPlainTextReadyToEncrypt",
        "UpdateListVerifierDirectoryIsRooted",
        "UpdateListVerifierEntryLeafFilenameUnsafe",
        "UpdateListVerifierPathEscapesRoot",
        "UpdateListVerifierReparsePoint",
        "UpdateMakerInvalidZipPackageName",
        "UpdateMakerOutputPathMustIncludeDirectory",
        "UpdateMakerTargetDirectoryNotFound",
        "UpdateMakerZipPackageCollision",
        "UpdateReaderDecryptedListMissingCompleteXml",
        "UpdateReaderEncryptedListEmptyOrTruncated",
        "UpdateReaderFileNotFound",
        "UpdateWriterGeneratedSuccessfully",
        "UpdateWriterNoChangesToSave"
    ];

    private static readonly ResourceManager Resources = new(
        "PangyaAPI.UpdateList.Localization.Strings",
        typeof(UpdateMaker).Assembly);

    [Fact]
    public void ResourceSets_HaveMatchingNonEmptyKeysAndPlaceholders()
    {
        ResourceSet neutralSet = ExactResourceSet(CultureInfo.InvariantCulture);
        Assert.Equal(ExpectedKeys, KeysFor(neutralSet));

        foreach (string cultureName in new[] { "pt-BR", "sv", "ja", "fr" })
        {
            ResourceSet localizedSet = ExactResourceSet(CultureInfo.GetCultureInfo(cultureName));
            Assert.Equal(ExpectedKeys, KeysFor(localizedSet));

            foreach (string key in ExpectedKeys)
            {
                string neutral = Assert.IsType<string>(neutralSet.GetObject(key));
                string localized = Assert.IsType<string>(localizedSet.GetObject(key));
                Assert.False(string.IsNullOrWhiteSpace(localized));
                Assert.Equal(Placeholders(neutral), Placeholders(localized));
            }
        }
    }

    [Theory]
    [InlineData("en", "Target directory does not exist: ")]
    [InlineData("pt-BR", "Diretório de destino não existe: ")]
    [InlineData("sv", "Målkatalogen finns inte: ")]
    [InlineData("ja", "対象ディレクトリが存在しません: ")]
    [InlineData("fr", "Le répertoire cible n’existe pas : ")]
    public void UpdateMaker_ExceptionUsesCurrentUiCultureAndPreservesPath(
        string cultureName,
        string expectedPrefix)
    {
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
        string missingPath = Path.Combine(
            Path.GetTempPath(),
            $"missing-update-list-{Guid.NewGuid():N}");

        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);

            DirectoryNotFoundException exception = Assert.Throws<DirectoryNotFoundException>(
                () => new UpdateMaker().GenerateFromDirectory(
                    missingPath,
                    Path.Combine(Path.GetTempPath(), "updatelist.dat"),
                    [1, 2, 3, 4],
                    "1"));

            Assert.Equal(expectedPrefix + missingPath, exception.Message);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Fact]
    public void KeyDetector_MessageUsesCurrentUiCultureAndPreservesHeaderCharacters()
    {
        string path = Path.GetTempFileName();
        TextWriter originalOutput = Console.Out;
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
        var output = new StringWriter(CultureInfo.InvariantCulture);

        try
        {
            var data = new byte[77];
            data[0] = (byte)'<';
            data[1] = (byte)'?';
            data[75] = (byte)'K';
            data[76] = (byte)'R';
            File.WriteAllBytes(path, data);

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("sv");
            Console.SetOut(output);

            UpdateKeyDetector.IsFileCrypt(path);

            Assert.Contains(
                "[Info] Filen öppnades som klartext. Klar att kryptera (KR)",
                output.ToString(),
                StringComparison.Ordinal);
        }
        finally
        {
            Console.SetOut(originalOutput);
            CultureInfo.CurrentUICulture = originalUiCulture;
            File.Delete(path);
            output.Dispose();
        }
    }

    private static ResourceSet ExactResourceSet(CultureInfo culture) =>
        Resources.GetResourceSet(culture, createIfNotExists: true, tryParents: false)
        ?? throw new MissingManifestResourceException(
            $"No exact UpdateList resource set exists for '{culture.Name}'.");

    private static string[] KeysFor(ResourceSet resourceSet) =>
        resourceSet.Cast<DictionaryEntry>()
            .Select(entry => (string)entry.Key)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();

    private static string[] Placeholders(string value) =>
        Regex.Matches(value, @"\{\d+(?::[^}]*)?\}")
            .Select(match => match.Value)
            .OrderBy(placeholder => placeholder, StringComparer.Ordinal)
            .ToArray();
}
