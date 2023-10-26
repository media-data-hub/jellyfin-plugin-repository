using System.Text.Json.Serialization;

namespace MediaDataHub.Plugin.Api.Model;

public class Tag : Record
{
  [JsonPropertyName("name")]
  public string Name { get; set; } = "";

  [JsonPropertyName("sortName")]
  public string SortName { get; set; } = "";
}
