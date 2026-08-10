namespace PangyaAPI.WFT;

public sealed record WftTrueTypeExportOptions
{
    public WftTrueTypeExportOptions(string familyName, WftFontStyle style = WftFontStyle.Regular,
        byte coverageThreshold = 128)
    {
        FamilyName = familyName;
        Style = style;
        CoverageThreshold = coverageThreshold;
    }

    public string FamilyName { get; init; }
    public WftFontStyle Style { get; init; }
    public byte CoverageThreshold { get; init; }
}
