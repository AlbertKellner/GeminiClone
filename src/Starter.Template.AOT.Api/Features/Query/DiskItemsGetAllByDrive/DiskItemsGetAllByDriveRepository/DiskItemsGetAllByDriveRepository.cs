namespace Starter.Template.AOT.Api.Features.Query.DiskItemsGetAllByDrive;

public class DiskItemsGetAllByDriveRepository(ILogger<DiskItemsGetAllByDriveRepository> logger) : IDiskItemsGetAllByDriveRepository
{
    public async Task<DiskItemEntity?> ScanDriveAsync(string driveId)
    {
        logger.LogInformation("[DiskItemsGetAllByDriveRepository][ScanDriveAsync] Iniciar varredura do drive. DriveId={DriveId}", driveId);

        var rootPath = ResolveRootPath(driveId);

        if (rootPath is null || !Directory.Exists(rootPath))
        {
            logger.LogInformation("[DiskItemsGetAllByDriveRepository][ScanDriveAsync] Drive não encontrado. DriveId={DriveId}", driveId);

            return null;
        }

        var root = new DiskItemEntity
        {
            Name = driveId,
            FullPath = rootPath,
            IsFolder = true,
            Children = []
        };

        await ScanDirectoryAsync(root);

        UpdateFolderSize(root);
        SortChildrenBySize(root);

        logger.LogInformation("[DiskItemsGetAllByDriveRepository][ScanDriveAsync] Retornar árvore de itens do drive. DriveId={DriveId}, TotalSizeBytes={Size}", driveId, root.SizeBytes);

        return root;
    }

    private static string? ResolveRootPath(string driveId)
    {
        if (driveId.Equals("root", StringComparison.OrdinalIgnoreCase))
            return "/";

        var candidate = $"{driveId.ToUpperInvariant()}:\\";

        return Directory.Exists(candidate) ? candidate : null;
    }

    private async Task ScanDirectoryAsync(DiskItemEntity folder)
    {
        var tasks = new List<Task>();

        try
        {
            var subDirectories = Directory.GetDirectories(folder.FullPath);

            logger.LogInformation("[DiskItemsGetAllByDriveRepository][ScanDirectoryAsync] Varrer {Count} subdiretórios em {Path}", subDirectories.Length, folder.FullPath);

            foreach (var dir in subDirectories)
            {
                var dirInfo = new DirectoryInfo(dir);
                var subFolder = new DiskItemEntity
                {
                    Name = dirInfo.Name,
                    FullPath = dir,
                    IsFolder = true,
                    Children = []
                };

                folder.Children.Add(subFolder);

                tasks.Add(Task.Run(async () => await ScanDirectoryAsync(subFolder)));
            }

            logger.LogInformation("[DiskItemsGetAllByDriveRepository][ScanDirectoryAsync] Subdiretórios iterados. {Count} filhos adicionados em {Path}", subDirectories.Length, folder.FullPath);

            var files = Directory.GetFiles(folder.FullPath);

            logger.LogInformation("[DiskItemsGetAllByDriveRepository][ScanDirectoryAsync] Varrer {Count} arquivos em {Path}", files.Length, folder.FullPath);

            foreach (var file in files)
            {
                try
                {
                    var fileInfo = new FileInfo(file);
                    folder.Children.Add(new DiskItemEntity
                    {
                        Name = fileInfo.Name,
                        FullPath = file,
                        IsFolder = false,
                        Extension = fileInfo.Extension,
                        SizeBytes = fileInfo.Length
                    });
                }
                catch (IOException)
                {
                    // Skip inaccessible files
                }
            }

            logger.LogInformation("[DiskItemsGetAllByDriveRepository][ScanDirectoryAsync] Arquivos iterados. {Count} arquivos processados em {Path}", files.Length, folder.FullPath);

            await Task.WhenAll(tasks);
        }
        catch (UnauthorizedAccessException)
        {
            // Skip inaccessible directories
        }
        catch (IOException)
        {
            // Skip directories with I/O errors
        }
    }

    private void UpdateFolderSize(DiskItemEntity folder)
    {
        logger.LogInformation("[DiskItemsGetAllByDriveRepository][UpdateFolderSize] Calcular tamanho acumulado da pasta. Path={Path}", folder.FullPath);

        if (!folder.IsFolder)
            return;

        logger.LogInformation("[DiskItemsGetAllByDriveRepository][UpdateFolderSize] Iterar {Count} filhos de {Path}", folder.Children.Count, folder.FullPath);

        foreach (var child in folder.Children)
        {
            UpdateFolderSize(child);
        }

        logger.LogInformation("[DiskItemsGetAllByDriveRepository][UpdateFolderSize] Iteração concluída. Calcular soma dos filhos de {Path}", folder.FullPath);

        folder.SizeBytes = folder.Children.Sum(c => c.SizeBytes);

        logger.LogInformation("[DiskItemsGetAllByDriveRepository][UpdateFolderSize] Tamanho calculado. Path={Path}, SizeBytes={Size}", folder.FullPath, folder.SizeBytes);
    }

    private void SortChildrenBySize(DiskItemEntity folder)
    {
        logger.LogInformation("[DiskItemsGetAllByDriveRepository][SortChildrenBySize] Ordenar filhos por tamanho. Path={Path}, Count={Count}", folder.FullPath, folder.Children.Count);

        folder.Children.Sort((a, b) => b.SizeBytes.CompareTo(a.SizeBytes));

        logger.LogInformation("[DiskItemsGetAllByDriveRepository][SortChildrenBySize] Iterar {Count} filhos para ordenação recursiva. Path={Path}", folder.Children.Count, folder.FullPath);

        foreach (var child in folder.Children)
        {
            SortChildrenBySize(child);
        }

        logger.LogInformation("[DiskItemsGetAllByDriveRepository][SortChildrenBySize] Ordenação recursiva concluída. Path={Path}", folder.FullPath);
    }
}
