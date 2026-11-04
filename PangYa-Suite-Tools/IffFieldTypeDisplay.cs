using PangYa_Suite_Tools.Localization;
using PangyaAPI.IFF;

namespace PangYa_Suite_Tools;

internal static class IffFieldTypeDisplay
{
    public static object[] SelectableTypes(IffFieldType? currentType = null) =>
        Enum.GetValues<IffFieldType>()
            .Where(type => type != (currentType == IffFieldType.Icon ? IffFieldType.ImageResource : IffFieldType.Icon))
            .Cast<object>().ToArray();

    public static string GetName(IffFieldType type) => type is IffFieldType.Icon or IffFieldType.ImageResource
        ? Strings.IFFManager_FieldTypeImageResource
        : type.ToString();

    public static void FormatComboBoxItem(object? sender, ListControlConvertEventArgs e)
    {
        if (e.ListItem is IffFieldType type) e.Value = GetName(type);
    }
}
