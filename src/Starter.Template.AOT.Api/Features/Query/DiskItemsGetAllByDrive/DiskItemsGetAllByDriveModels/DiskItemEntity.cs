namespace Starter.Template.AOT.Api.Features.Query.DiskItemsGetAllByDrive;

public class DiskItemEntity
{
    public string Name { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public bool IsFolder { get; set; }
    public string Extension { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public List<DiskItemEntity> Children { get; set; } = [];
}
