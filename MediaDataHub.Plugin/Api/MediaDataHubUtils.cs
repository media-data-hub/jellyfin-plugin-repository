using Microsoft.AspNetCore.WebUtilities;
using MediaDataHub.Plugin.Configuration;

namespace MediaDataHub.Plugin.Api;

public static class MediaDataHubUtils
{
  public static string GetFileUrl(IRecord record, string fileName, string? thumb = null)
  {
    var config = GetConfiguration();
    var host = GetHost(config);
    var remoteUrl = $"{host}/api/files/{record.MetaCollectionName}/{record.Id}/{fileName}";
    return string.IsNullOrEmpty(thumb) ? remoteUrl : QueryHelpers.AddQueryString(remoteUrl, new Dictionary<string, string?> { { "thumb", thumb } });
  }

  public static PluginConfiguration GetConfiguration()
  {
    return (Plugin.Instance?.Configuration) ?? throw new Exception("Unexpected null return value.");
  }

  public static string GetHost(PluginConfiguration? config = null)
  {
    config ??= GetConfiguration();
    return string.IsNullOrEmpty(config.Host) ? throw new Exception("Host cannot be empty", null) : config.Host;
  }
}
