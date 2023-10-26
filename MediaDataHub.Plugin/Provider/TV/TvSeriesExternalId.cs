using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace MediaDataHub.Plugin.Provider.TV;

public class TvSeriesExternalId : IExternalId
{
  /// <inheritdoc />
  public string ProviderName => Plugin.ProviderName;

  /// <inheritdoc />
  public string Key => Plugin.ProviderId;

  /// <inheritdoc />
  public ExternalIdMediaType? Type => ExternalIdMediaType.Series;

  /// <inheritdoc />
  public string? UrlFormatString => null;

  /// <inheritdoc />
  public bool Supports(IHasProviderIds item) => item is Series;
}
