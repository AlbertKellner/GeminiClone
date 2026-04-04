namespace Starter.Template.AOT.Api.Features.Query.DiskItemsGetAllByDrive;

public class DiskItemsGetAllByDriveOutput
{
    public string DriveId { get; init; } = string.Empty;
    public DiskItemsGetAllByDriveItemOutput? Root { get; init; }
}

public class DiskItemsGetAllByDriveItemOutput
{
    public string Name { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public string FormattedSize { get; init; } = string.Empty;
    public bool IsFolder { get; init; }
    public string Extension { get; init; } = string.Empty;
    public List<DiskItemsGetAllByDriveItemOutput> Children { get; init; } = [];
}
