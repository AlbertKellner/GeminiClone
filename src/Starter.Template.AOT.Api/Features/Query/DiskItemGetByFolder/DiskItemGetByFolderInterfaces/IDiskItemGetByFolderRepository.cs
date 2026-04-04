namespace Starter.Template.AOT.Api.Features.Query.DiskItemGetByFolder;

public interface IDiskItemGetByFolderRepository
{
    Task<DiskItemGetByFolderEntity?> ScanFolderAsync(string driveId, string folderPath);
}
