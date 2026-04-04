namespace Starter.Template.AOT.Api.Features.Query.DrivesGetAll;

public class DrivesGetAllRepository(ILogger<DrivesGetAllRepository> logger) : IDrivesGetAllRepository
{
    public List<DrivesGetAllEntity> GetAllDrives()
    {
        logger.LogInformation("[DrivesGetAllRepository][GetAllDrives] Consultar drives disponíveis no sistema");

        var drives = DriveInfo.GetDrives();
        var result = new List<DrivesGetAllEntity>();

        logger.LogInformation("[DrivesGetAllRepository][GetAllDrives] Iterar {Count} drives detectados", drives.Length);

        foreach (var drive in drives)
        {
            var id = BuildDriveId(drive.Name);
            long total = 0;
            long available = 0;

            try
            {
                if (drive.IsReady)
                {
                    total = drive.TotalSize;
                    available = drive.AvailableFreeSpace;
                }
            }
            catch (IOException)
            {
                // Drive not ready — sizes remain zero
            }

            result.Add(new DrivesGetAllEntity
            {
                Id = id,
                Name = drive.Name,
                DriveType = drive.DriveType.ToString(),
                TotalSizeBytes = total,
                AvailableSizeBytes = available
            });
        }

        logger.LogInformation("[DrivesGetAllRepository][GetAllDrives] Iteração concluída. {Count} drives processados", result.Count);

        logger.LogInformation("[DrivesGetAllRepository][GetAllDrives] Retornar {Count} drives mapeados", result.Count);

        return result;
    }

    private static string BuildDriveId(string driveName)
    {
        if (driveName == "/")
            return "root";

        return driveName.Replace(":\\", string.Empty).Replace(":", string.Empty).ToLowerInvariant();
    }
}
