using ArquivosDoDisco.Entities;

namespace ArquivosDoDisco.UseCase
{
    public static class FileManager
    {
        public static MyFolderEntity ListFoldersAndFiles(string path)
        {
            var dirInfo = new DirectoryInfo(path);
            return CreateStructure(dirInfo);
        }

        private static MyFolderEntity CreateStructure(DirectoryInfo dirInfo)
        {
            var folder = new MyFolderEntity
            {
                Name = dirInfo.Name,
                FullPath = dirInfo.FullName,
                Files = new List<MyFileEntity>(),
                Folders = new List<MyFolderEntity>()
            };

            try
            {
                AddFilesToFolder(dirInfo, folder);
                AddSubFoldersToFolder(dirInfo, folder);
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine($"Acesso negado à pasta: {dirInfo.FullName}");
            }

            return folder;
        }


        private static void AddFilesToFolder(DirectoryInfo dirInfo, MyFolderEntity folder)
        {
            foreach (var fileInfo in dirInfo.GetFiles())
            {
                var file = new MyFileEntity
                {
                    Name = fileInfo.Name,
                    Size = fileInfo.Length,
                    Extension = fileInfo.Extension,
                    FullPath = fileInfo.FullName
                };

                folder.Files.Add(file);
                folder.TotalSize += file.Size;
            }
        }

        public static void AddSubFoldersToFolder(DirectoryInfo dirInfo, MyFolderEntity folder)
        {
            foreach (var subFolderInfo in dirInfo.GetDirectories())
            {
                var subFolder = CreateStructure(subFolderInfo);
                folder.Folders.Add(subFolder);
                folder.TotalSize += subFolder.TotalSize;
            }
        }
    }
}
