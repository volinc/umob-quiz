using NetTopologySuite.Geometries;

namespace UmobQuiz.Api.Domain.Entities;

public sealed class Station
{
    public Guid Id { get; set; }
    public required string Provider { get; set; }
    public required string ExternalId { get; set; }
    public required Point Location { get; set; }
    public int? Capacity { get; set; }
    public int? NumBikesAvailable { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; }
}
