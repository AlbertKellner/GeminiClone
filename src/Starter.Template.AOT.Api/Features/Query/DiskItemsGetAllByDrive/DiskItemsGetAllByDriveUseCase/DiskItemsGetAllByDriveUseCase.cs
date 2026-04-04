using Starter.Template.AOT.Api.Shared.Formatting;

namespace Starter.Template.AOT.Api.Features.Query.DiskItemsGetAllByDrive;

public class DiskItemsGetAllByDriveUseCase(IDiskItemsGetAllByDriveRepository repository, ILogger<DiskItemsGetAllByDriveUseCase> logger)
{
    public async Task<DiskItemsGetAllByDriveOutput?> ExecuteAsync(string driveId)
    {
        logger.LogInformation("[DiskItemsGetAllByDriveUseCase][ExecuteAsync] Iniciar varredura completa do drive. DriveId={DriveId}", driveId);

        var root = await repository.ScanDriveAsync(driveId);

        if (root is null)
        {
            logger.LogInformation("[DiskItemsGetAllByDriveUseCase][ExecuteAsync] Drive não encontrado. DriveId={DriveId}", driveId);

            return null;
        }

        var output = new DiskItemsGetAllByDriveOutput
        {
            DriveId = driveId,
            Root = MapToOutput(root)
        };

        logger.LogInformation("[DiskItemsGetAllByDriveUseCase][ExecuteAsync] Retornar estrutura do drive. DriveId={DriveId}, TotalSizeBytes={Size}", driveId, root.SizeBytes);

        return output;
    }

    private static DiskItemsGetAllByDriveItemOutput MapToOutput(DiskItemEntity entity)
    {
        return new DiskItemsGetAllByDriveItemOutput
        {
            Name = entity.Name,
            SizeBytes = entity.SizeBytes,
            FormattedSize = DiskSizeFormatter.FormatBytes(entity.SizeBytes),
            IsFolder = entity.IsFolder,
            Extension = entity.Extension,
            Children = entity.Children.Select(MapToOutput).ToList()
        };
    }
}
