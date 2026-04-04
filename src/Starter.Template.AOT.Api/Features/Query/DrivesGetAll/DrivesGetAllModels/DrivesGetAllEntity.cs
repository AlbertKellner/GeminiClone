namespace Starter.Template.AOT.Api.Features.Query.DrivesGetAll;

public class DrivesGetAllEntity
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string DriveType { get; init; } = string.Empty;
    public long TotalSizeBytes { get; init; }
    public long AvailableSizeBytes { get; init; }
}
