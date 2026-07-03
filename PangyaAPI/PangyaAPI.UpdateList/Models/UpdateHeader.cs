
namespace PangyaAPI.UpdateList.Models;

public class UpdateHeader
{
    public string ClientPatchVersion { get; set; } = string.Empty;
    public string ClientPatchNum { get; set; } = string.Empty;

    public string UpdateVersion { get; set; } = string.Empty;
}
