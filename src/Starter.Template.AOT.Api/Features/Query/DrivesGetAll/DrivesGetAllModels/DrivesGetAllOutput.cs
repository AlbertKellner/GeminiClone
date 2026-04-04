namespace Starter.Template.AOT.Api.Features.Query.DrivesGetAll;

public class DrivesGetAllOutput
{
    public List<DrivesGetAllDriveOutput> Drives { get; init; } = [];
}

public class DrivesGetAllDriveOutput
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string DriveType { get; init; } = string.Empty;
    public long TotalSizeBytes { get; init; }
    public long AvailableSizeBytes { get; init; }
    public string FormattedTotalSize { get; init; } = string.Empty;
    public string FormattedAvailableSize { get; init; } = string.Empty;
}
