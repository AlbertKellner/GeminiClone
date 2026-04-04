using Starter.Template.AOT.Api.Shared.Formatting;

namespace Starter.Template.AOT.Api.Features.Query.DiskItemGetByFolder;

public class DiskItemGetByFolderUseCase(IDiskItemGetByFolderRepository repository, ILogger<DiskItemGetByFolderUseCase> logger)
{
    public async Task<DiskItemGetByFolderOutput?> ExecuteAsync(DiskItemGetByFolderInput input)
    {
        logger.LogInformation("[DiskItemGetByFolderUseCase][ExecuteAsync] Obter itens da pasta. DriveId={DriveId}, FolderPath={FolderPath}", input.DriveId, input.FolderPath);

        var entity = await repository.ScanFolderAsync(input.DriveId, input.FolderPath);

        if (entity is null)
        {
            logger.LogInformation("[DiskItemGetByFolderUseCase][ExecuteAsync] Pasta não encontrada. DriveId={DriveId}, FolderPath={FolderPath}", input.DriveId, input.FolderPath);

            return null;
        }

        var output = new DiskItemGetByFolderOutput
        {
            DriveId = input.DriveId,
            FolderPath = input.FolderPath,
            Folder = MapToOutput(entity)
        };

        logger.LogInformation("[DiskItemGetByFolderUseCase][ExecuteAsync] Retornar estrutura da pasta. DriveId={DriveId}, FolderPath={FolderPath}", input.DriveId, input.FolderPath);

        return output;
    }

    private static DiskItemGetByFolderItemOutput MapToOutput(DiskItemGetByFolderEntity entity)
    {
        return new DiskItemGetByFolderItemOutput
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
