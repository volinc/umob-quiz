using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using UmobQuiz.Api.Configuration;
using UmobQuiz.Api.Infrastructure.Persistence;

namespace UmobQuiz.Api.Infrastructure.Gbfs;

public sealed class GbfsIngestionService(
    AppDbContext dbContext,
    GbfsClient gbfsClient,
    IConfiguration configuration,
    ILogger<GbfsIngestionService> logger)
{
    private static readonly GeometryFactory GeometryFactory =
        NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    public async Task IngestAllProvidersAsync(CancellationToken cancellationToken)
    {
        var providers = configuration.GetSection(GbfsProviderOptions.SectionName).Get<GbfsProviderOptions[]>() ?? [];
        foreach (var provider in providers)
        {
            try
            {
                await IngestProviderAsync(provider, cancellationToken);
            }
            catch (Exception ex)
            {
                // One provider failing must not block ingestion for the others.
                logger.LogWarning(ex, "Skipped GBFS ingestion for provider {Provider}", provider.Key);
            }
        }
    }

    private async Task IngestProviderAsync(GbfsProviderOptions provider, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var bikes = await gbfsClient.GetFreeBikesAsync(provider.GbfsUrl, cancellationToken);
        var stations = await gbfsClient.GetStationsAsync(provider.GbfsUrl, cancellationToken);

        await UpsertBikesAsync(provider.Key, bikes, now, cancellationToken);
        await UpsertStationsAsync(provider.Key, stations, now, cancellationToken);

        logger.LogInformation(
            "Ingested {BikeCount} bikes and {StationCount} stations for {Provider}",
            bikes.Count,
            stations.Count,
            provider.Key);
    }

    private async Task UpsertBikesAsync(
        string providerKey,
        IReadOnlyList<FreeBike> bikes,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var incomingIds = bikes.Select(b => b.BikeId!).ToHashSet();
        var existing = await dbContext.Bikes
            .Where(b => b.Provider == providerKey)
            .ToListAsync(cancellationToken);

        foreach (var bike in bikes)
        {
            var point = GeometryFactory.CreatePoint(new Coordinate(bike.Lon!.Value, bike.Lat!.Value));
            var entity = existing.FirstOrDefault(b => b.ExternalId == bike.BikeId);
            if (entity is null)
            {
                dbContext.Bikes.Add(new Domain.Entities.Bike
                {
                    Id = Guid.NewGuid(),
                    Provider = providerKey,
                    ExternalId = bike.BikeId!,
                    Location = point,
                    IsActive = true,
                    UpdatedAt = now
                });
            }
            else
            {
                entity.Location = point;
                entity.IsActive = true;
                entity.UpdatedAt = now;
            }
        }

        foreach (var stale in existing.Where(b => !incomingIds.Contains(b.ExternalId)))
        {
            stale.IsActive = false;
            stale.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertStationsAsync(
        string providerKey,
        IReadOnlyList<StationSnapshot> stations,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var incomingIds = stations.Select(s => s.StationId).ToHashSet();
        var existing = await dbContext.Stations
            .Where(s => s.Provider == providerKey)
            .ToListAsync(cancellationToken);

        foreach (var station in stations)
        {
            var point = GeometryFactory.CreatePoint(new Coordinate(station.Lon, station.Lat));
            var entity = existing.FirstOrDefault(s => s.ExternalId == station.StationId);
            if (entity is null)
            {
                dbContext.Stations.Add(new Domain.Entities.Station
                {
                    Id = Guid.NewGuid(),
                    Provider = providerKey,
                    ExternalId = station.StationId,
                    Location = point,
                    Capacity = station.Capacity,
                    NumBikesAvailable = station.NumBikesAvailable,
                    IsActive = true,
                    UpdatedAt = now
                });
            }
            else
            {
                entity.Location = point;
                entity.Capacity = station.Capacity;
                entity.NumBikesAvailable = station.NumBikesAvailable;
                entity.IsActive = true;
                entity.UpdatedAt = now;
            }
        }

        foreach (var stale in existing.Where(s => !incomingIds.Contains(s.ExternalId)))
        {
            stale.IsActive = false;
            stale.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
