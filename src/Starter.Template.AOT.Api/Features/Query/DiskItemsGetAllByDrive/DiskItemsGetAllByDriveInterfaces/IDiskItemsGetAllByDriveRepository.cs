namespace Starter.Template.AOT.Api.Features.Query.DiskItemsGetAllByDrive;

public interface IDiskItemsGetAllByDriveRepository
{
    Task<DiskItemEntity?> ScanDriveAsync(string driveId);
}
