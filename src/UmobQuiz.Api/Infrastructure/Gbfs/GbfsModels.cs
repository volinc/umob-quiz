using System.Text.Json.Serialization;

namespace UmobQuiz.Api.Infrastructure.Gbfs;

public sealed class GbfsRoot
{
    [JsonPropertyName("data")]
    public GbfsData? Data { get; set; }
}

public sealed class GbfsData
{
    [JsonPropertyName("en")]
    public GbfsLanguageFeeds? En { get; set; }

    [JsonPropertyName("feeds")]
    public List<GbfsFeed>? Feeds { get; set; }
}

public sealed class GbfsLanguageFeeds
{
    [JsonPropertyName("feeds")]
    public List<GbfsFeed>? Feeds { get; set; }
}

public sealed class GbfsFeed
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

public sealed class FreeBikeStatusResponse
{
    [JsonPropertyName("data")]
    public FreeBikeStatusData? Data { get; set; }
}

public sealed class FreeBikeStatusData
{
    [JsonPropertyName("bikes")]
    public List<FreeBike>? Bikes { get; set; }
}

public sealed class FreeBike
{
    [JsonPropertyName("bike_id")]
    public string? BikeId { get; set; }

    [JsonPropertyName("lat")]
    public double? Lat { get; set; }

    [JsonPropertyName("lon")]
    public double? Lon { get; set; }
}

public sealed class VehicleStatusResponse
{
    [JsonPropertyName("data")]
    public VehicleStatusData? Data { get; set; }
}

public sealed class VehicleStatusData
{
    [JsonPropertyName("vehicles")]
    public List<VehicleStatus>? Vehicles { get; set; }
}

public sealed class VehicleStatus
{
    [JsonPropertyName("vehicle_id")]
    public string? VehicleId { get; set; }

    [JsonPropertyName("lat")]
    public double? Lat { get; set; }

    [JsonPropertyName("lon")]
    public double? Lon { get; set; }
}

public sealed class StationStatusResponse
{
    [JsonPropertyName("data")]
    public StationStatusData? Data { get; set; }
}

public sealed class StationStatusData
{
    [JsonPropertyName("stations")]
    public List<StationStatus>? Stations { get; set; }
}

public sealed class StationStatus
{
    [JsonPropertyName("station_id")]
    public string? StationId { get; set; }

    [JsonPropertyName("num_bikes_available")]
    public int? NumBikesAvailable { get; set; }

    [JsonPropertyName("num_docks_available")]
    public int? NumDocksAvailable { get; set; }
}

public sealed class StationInformationResponse
{
    [JsonPropertyName("data")]
    public StationInformationData? Data { get; set; }
}

public sealed class StationInformationData
{
    [JsonPropertyName("stations")]
    public List<StationInformation>? Stations { get; set; }
}

public sealed class StationInformation
{
    [JsonPropertyName("station_id")]
    public string? StationId { get; set; }

    [JsonPropertyName("lat")]
    public double? Lat { get; set; }

    [JsonPropertyName("lon")]
    public double? Lon { get; set; }

    [JsonPropertyName("capacity")]
    public int? Capacity { get; set; }
}
