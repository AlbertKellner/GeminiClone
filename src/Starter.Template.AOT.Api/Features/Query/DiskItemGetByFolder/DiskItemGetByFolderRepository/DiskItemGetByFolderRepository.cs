namespace Starter.Template.AOT.Api.Features.Query.DiskItemGetByFolder;

public class DiskItemGetByFolderRepository(ILogger<DiskItemGetByFolderRepository> logger) : IDiskItemGetByFolderRepository
{
    public async Task<DiskItemGetByFolderEntity?> ScanFolderAsync(string driveId, string folderPath)
    {
        logger.LogInformation("[DiskItemGetByFolderRepository][ScanFolderAsync] Iniciar varredura da pasta. DriveId={DriveId}, FolderPath={FolderPath}", driveId, folderPath);

        var absolutePath = ResolveAbsolutePath(driveId, folderPath);

        if (absolutePath is null || !Directory.Exists(absolutePath))
        {
            logger.LogInformation("[DiskItemGetByFolderRepository][ScanFolderAsync] Pasta não encontrada. DriveId={DriveId}, FolderPath={FolderPath}", driveId, folderPath);

            return null;
        }

        var dirInfo = new DirectoryInfo(absolutePath);
        var root = new DiskItemGetByFolderEntity
        {
            Name = dirInfo.Name,
            FullPath = absolutePath,
            IsFolder = true,
            Children = []
        };

        await ScanDirectoryAsync(root);

        UpdateFolderSize(root);
        SortChildrenBySize(root);

        logger.LogInformation("[DiskItemGetByFolderRepository][ScanFolderAsync] Retornar árvore da pasta. DriveId={DriveId}, FolderPath={FolderPath}, TotalSizeBytes={Size}", driveId, folderPath, root.SizeBytes);

        return root;
    }

    private static string? ResolveAbsolutePath(string driveId, string folderPath)
    {
        string rootPath;

        if (driveId.Equals("root", StringComparison.OrdinalIgnoreCase))
        {
            rootPath = "/";
        }
        else
        {
            rootPath = $"{driveId.ToUpperInvariant()}:\\";

            if (!Directory.Exists(rootPath))
                return null;
        }

        var normalizedFolder = folderPath
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar)
            .TrimStart(Path.DirectorySeparatorChar);

        return string.IsNullOrWhiteSpace(normalizedFolder)
            ? rootPath
            : Path.Combine(rootPath, normalizedFolder);
    }

    private async Task ScanDirectoryAsync(DiskItemGetByFolderEntity folder)
    {
        var tasks = new List<Task>();

        try
        {
            var subDirectories = Directory.GetDirectories(folder.FullPath);

            logger.LogInformation("[DiskItemGetByFolderRepository][ScanDirectoryAsync] Varrer {Count} subdiretórios em {Path}", subDirectories.Length, folder.FullPath);

            foreach (var dir in subDirectories)
            {
                var dirInfo = new DirectoryInfo(dir);
                var subFolder = new DiskItemGetByFolderEntity
                {
                    Name = dirInfo.Name,
                    FullPath = dir,
                    IsFolder = true,
                    Children = []
                };

                folder.Children.Add(subFolder);

                tasks.Add(Task.Run(async () => await ScanDirectoryAsync(subFolder)));
            }

            logger.LogInformation("[DiskItemGetByFolderRepository][ScanDirectoryAsync] Subdiretórios iterados. {Count} filhos adicionados em {Path}", subDirectories.Length, folder.FullPath);

            var files = Directory.GetFiles(folder.FullPath);

            logger.LogInformation("[DiskItemGetByFolderRepository][ScanDirectoryAsync] Varrer {Count} arquivos em {Path}", files.Length, folder.FullPath);

            foreach (var file in files)
            {
                try
                {
                    var fileInfo = new FileInfo(file);
                    folder.Children.Add(new DiskItemGetByFolderEntity
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

            logger.LogInformation("[DiskItemGetByFolderRepository][ScanDirectoryAsync] Arquivos iterados. {Count} arquivos processados em {Path}", files.Length, folder.FullPath);

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

    private void UpdateFolderSize(DiskItemGetByFolderEntity folder)
    {
        logger.LogInformation("[DiskItemGetByFolderRepository][UpdateFolderSize] Calcular tamanho acumulado da pasta. Path={Path}", folder.FullPath);

        if (!folder.IsFolder)
            return;

        logger.LogInformation("[DiskItemGetByFolderRepository][UpdateFolderSize] Iterar {Count} filhos de {Path}", folder.Children.Count, folder.FullPath);

        foreach (var child in folder.Children)
        {
            UpdateFolderSize(child);
        }

        logger.LogInformation("[DiskItemGetByFolderRepository][UpdateFolderSize] Iteração concluída. Calcular soma dos filhos de {Path}", folder.FullPath);

        folder.SizeBytes = folder.Children.Sum(c => c.SizeBytes);

        logger.LogInformation("[DiskItemGetByFolderRepository][UpdateFolderSize] Tamanho calculado. Path={Path}, SizeBytes={Size}", folder.FullPath, folder.SizeBytes);
    }

    private void SortChildrenBySize(DiskItemGetByFolderEntity folder)
    {
        logger.LogInformation("[DiskItemGetByFolderRepository][SortChildrenBySize] Ordenar filhos por tamanho. Path={Path}, Count={Count}", folder.FullPath, folder.Children.Count);

        folder.Children.Sort((a, b) => b.SizeBytes.CompareTo(a.SizeBytes));

        logger.LogInformation("[DiskItemGetByFolderRepository][SortChildrenBySize] Iterar {Count} filhos para ordenação recursiva. Path={Path}", folder.Children.Count, folder.FullPath);

        foreach (var child in folder.Children)
        {
            SortChildrenBySize(child);
        }

        logger.LogInformation("[DiskItemGetByFolderRepository][SortChildrenBySize] Ordenação recursiva concluída. Path={Path}", folder.FullPath);
    }
}
