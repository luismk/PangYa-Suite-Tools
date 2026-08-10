namespace PangyaAPI.WFT;

public readonly record struct WftTrueTypeExportProgress(int ProcessedGlyphRecords,
    int TotalGlyphRecords);
