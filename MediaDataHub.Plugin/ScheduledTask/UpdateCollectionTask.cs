using Microsoft.Extensions.Logging;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Tasks;
using MediaDataHub.Plugin.Api;
using MediaDataHub.Plugin.Api.Manager;
using Model = MediaDataHub.Plugin.Api.Model;
using MediaDataHub.Plugin.Configuration;

namespace MediaDataHub.Plugin.ScheduledTask;

public class UpdateCollectionTask : IScheduledTask
{
  private readonly ILogger<UpdateCollectionTask> _logger;
  private readonly ILibraryManager _libraryManager;
  private readonly ICollectionManager _collectionManager;
  private readonly MediaDataHubApiManager _apiManager;
  public string Name => "Update Collections";
  public string Key => "MediaDataHub.UpdateCollectionTask";
  public string Description => "Add Tv series and movies to collections";
  public string Category => "Media Data Hub";

  public UpdateCollectionTask(
    ILibraryManager libraryManager,
    ICollectionManager collectionManager,
    MediaDataHubApiManager apiManager,
    ILogger<UpdateCollectionTask> logger)
  {
    _libraryManager = libraryManager;
    _collectionManager = collectionManager;
    _logger = logger;
    _apiManager = apiManager;
  }
  public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Start Scheduled Task ({name})", Name);
    var config = MediaDataHubUtils.GetConfiguration();
    if (!config.AutoAddToCollection)
    {
      _logger.LogInformation("End update movie collections (Disabled)");
      return;
    }
    var query = new InternalItemsQuery
    {
      IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Series },
      IsVirtualItem = false,
      Recursive = true
    };
    var items = _libraryManager.GetItemList(query).Select((item, index) => (item, index));
    foreach (var (item, index) in items)
    {
      progress?.Report(100.0 * index / items.Count());
      if (!item.TryGetProviderId(Plugin.ProviderId, out var id))
      {
        continue;
      }
      IEnumerable<Model.Collection> collections;
      try
      {
        switch (item)
        {
          case Movie movie:
            var movieDetail = await _apiManager.GetMovieDetailById(id, cancellationToken).ConfigureAwait(false);
            collections = movieDetail.Expand.Collections;
            break;
          case Series series:
            var tvSeriesDetail = await _apiManager.GetTvSeriesDetailById(id, cancellationToken).ConfigureAwait(false);
            collections = tvSeriesDetail.Expand.Collections;
            break;
          default:
            continue;
        }
      }
      catch (Model.ApiException e)
      {
        _logger.LogWarning(e, "Failed to load ({id})", id);
        continue;
      }
      foreach (var collection in collections)
      {
        var boxSet = await GetOrCreateBoxSet(collection, config);
        if (boxSet != null)
        {
          if (!boxSet.ContainsLinkedChildByItemId(item.Id))
          {
            await _collectionManager.AddToCollectionAsync(boxSet.Id, new List<Guid> { item.Id }).ConfigureAwait(false);
          }
        }
        else
        {
          _logger.LogWarning("Failed to get or create boxSet ({id}, {name})", collection.Id, collection.Name);
        }
      }
    }
    progress?.Report(100);
    _logger.LogInformation("End update movie collections");
  }

  private async Task<BoxSet?> GetOrCreateBoxSet(ICollectionCreation collection, PluginConfiguration config)
  {
    var query = new InternalItemsQuery
    {
      IncludeItemTypes = new[] { BaseItemKind.BoxSet },
      CollapseBoxSetItems = false,
      Recursive = true,
      HasAnyProviderId = new Dictionary<string, string> { { Plugin.ProviderId, collection.Id } }
    };
    var boxSet = _libraryManager.GetItemList(query).FirstOrDefault() as BoxSet;
    if (boxSet != null)
    {
      return boxSet;
    }
    if (config.AutoConnectCollection)
    {
      query = new InternalItemsQuery
      {
        IncludeItemTypes = new[] { BaseItemKind.BoxSet },
        CollapseBoxSetItems = false,
        Recursive = true,
        Name = collection.Name
      };
      boxSet = _libraryManager.GetItemList(query).FirstOrDefault() as BoxSet;
    }
    if (boxSet != null)
    {
      return boxSet;
    }
    return config.AutoCreateCollection ? await _collectionManager.CreateCollectionAsync(collection.ToCollectionCreationOptions()).ConfigureAwait(false) : null;
  }

  public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
  {
    // Run this task every 24 hours
    return new[] { new TaskTriggerInfo { Type = TaskTriggerInfo.TriggerInterval, IntervalTicks = TimeSpan.FromHours(24).Ticks } };
  }
}
