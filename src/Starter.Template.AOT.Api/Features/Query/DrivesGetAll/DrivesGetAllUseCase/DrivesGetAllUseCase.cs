using Starter.Template.AOT.Api.Shared.Formatting;

namespace Starter.Template.AOT.Api.Features.Query.DrivesGetAll;

public class DrivesGetAllUseCase(IDrivesGetAllRepository repository, ILogger<DrivesGetAllUseCase> logger)
{
    public DrivesGetAllOutput Execute()
    {
        logger.LogInformation("[DrivesGetAllUseCase][Execute] Obter todos os drives disponíveis");

        var entities = repository.GetAllDrives();

        var drives = entities.Select(e => new DrivesGetAllDriveOutput
        {
            Id = e.Id,
            Name = e.Name,
            DriveType = e.DriveType,
            TotalSizeBytes = e.TotalSizeBytes,
            AvailableSizeBytes = e.AvailableSizeBytes,
            FormattedTotalSize = DiskSizeFormatter.FormatBytes(e.TotalSizeBytes),
            FormattedAvailableSize = DiskSizeFormatter.FormatBytes(e.AvailableSizeBytes)
        }).ToList();

        var output = new DrivesGetAllOutput { Drives = drives };

        logger.LogInformation("[DrivesGetAllUseCase][Execute] Retornar {Count} drives formatados", output.Drives.Count);

        return output;
    }
}
