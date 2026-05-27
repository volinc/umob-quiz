using System.Text.Json;

namespace UmobQuiz.Api.Infrastructure.Gbfs;

// GBFS discovery supports both v2.x and v3.x manifest layouts.

public sealed class GbfsClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<IReadOnlyList<FreeBike>> GetFreeBikesAsync(string gbfsRootUrl, CancellationToken cancellationToken)
    {
        foreach (var feedName in new[] { "free_bike_status", "vehicle_status" })
        {
            var feedUrl = await ResolveFeedUrlAsync(gbfsRootUrl, feedName, cancellationToken);
            if (feedUrl is null)
            {
                continue;
            }

            if (feedName == "vehicle_status")
            {
                var vehicleResponse = await httpClient.GetFromJsonAsync<VehicleStatusResponse>(feedUrl, JsonOptions, cancellationToken);
                return vehicleResponse?.Data?.Vehicles?
                    .Where(v => v.VehicleId is not null && v.Lat is not null && v.Lon is not null)
                    .Select(v => new FreeBike { BikeId = v.VehicleId, Lat = v.Lat, Lon = v.Lon })
                    .ToList() ?? [];
            }

            var response = await httpClient.GetFromJsonAsync<FreeBikeStatusResponse>(feedUrl, JsonOptions, cancellationToken);
            return response?.Data?.Bikes?
                .Where(b => b.BikeId is not null && b.Lat is not null && b.Lon is not null)
                .ToList() ?? [];
        }

        return [];
    }

    public async Task<IReadOnlyList<StationSnapshot>> GetStationsAsync(string gbfsRootUrl, CancellationToken cancellationToken)
    {
        var statusUrl = await ResolveFeedUrlAsync(gbfsRootUrl, "station_status", cancellationToken);
        var infoUrl = await ResolveFeedUrlAsync(gbfsRootUrl, "station_information", cancellationToken);
        if (infoUrl is null)
        {
            return [];
        }

        var infoResponse = await httpClient.GetFromJsonAsync<StationInformationResponse>(infoUrl, JsonOptions, cancellationToken);
        var stations = infoResponse?.Data?.Stations?
            .Where(s => s.StationId is not null && s.Lat is not null && s.Lon is not null)
            .ToDictionary(s => s.StationId!) ?? new Dictionary<string, StationInformation>();

        Dictionary<string, StationStatus>? statusById = null;
        if (statusUrl is not null)
        {
            var statusResponse = await httpClient.GetFromJsonAsync<StationStatusResponse>(statusUrl, JsonOptions, cancellationToken);
            statusById = statusResponse?.Data?.Stations?
                .Where(s => s.StationId is not null)
                .ToDictionary(s => s.StationId!) ?? new Dictionary<string, StationStatus>();
        }

        return stations.Values
            .Select(info =>
            {
                int? bikesAvailable = null;
                if (statusById is not null &&
                    statusById.TryGetValue(info.StationId!, out var status))
                {
                    bikesAvailable = status.NumBikesAvailable;
                }

                return new StationSnapshot(
                    info.StationId!,
                    info.Lat!.Value,
                    info.Lon!.Value,
                    info.Capacity,
                    bikesAvailable);
            })
            .ToList();
    }

    private async Task<string?> ResolveFeedUrlAsync(string gbfsRootUrl, string feedName, CancellationToken cancellationToken)
    {
        var root = await httpClient.GetFromJsonAsync<GbfsRoot>(gbfsRootUrl, JsonOptions, cancellationToken);
        var feeds = root?.Data?.En?.Feeds ?? root?.Data?.Feeds;
        if (feeds is null || feeds.Count == 0)
        {
            using var doc = await JsonDocument.ParseAsync(
                await httpClient.GetStreamAsync(gbfsRootUrl, cancellationToken),
                cancellationToken: cancellationToken);
            feeds = ExtractFeedsFromJson(doc.RootElement);
        }

        return feeds?.FirstOrDefault(f =>
            string.Equals(f.Name, feedName, StringComparison.OrdinalIgnoreCase))?.Url;
    }

    private static List<GbfsFeed> ExtractFeedsFromJson(JsonElement root)
    {
        if (!root.TryGetProperty("data", out var data))
        {
            return [];
        }

        if (data.TryGetProperty("en", out var en) && en.TryGetProperty("feeds", out var enFeeds))
        {
            return ParseFeedArray(enFeeds);
        }

        if (data.TryGetProperty("feeds", out var directFeeds))
        {
            return ParseFeedArray(directFeeds);
        }

        foreach (var language in data.EnumerateObject())
        {
            if (language.Value.TryGetProperty("feeds", out var langFeeds))
            {
                return ParseFeedArray(langFeeds);
            }
        }

        return [];
    }

    private static List<GbfsFeed> ParseFeedArray(JsonElement feedsElement)
    {
        var feeds = new List<GbfsFeed>();
        foreach (var feed in feedsElement.EnumerateArray())
        {
            feeds.Add(new GbfsFeed
            {
                Name = feed.TryGetProperty("name", out var name) ? name.GetString() : null,
                Url = feed.TryGetProperty("url", out var url) ? url.GetString() : null
            });
        }

        return feeds;
    }
}

public sealed record StationSnapshot(
    string StationId,
    double Lat,
    double Lon,
    int? Capacity,
    int? NumBikesAvailable);
