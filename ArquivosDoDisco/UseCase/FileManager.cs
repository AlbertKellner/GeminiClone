using System.Runtime.InteropServices;
using ArquivosDoDisco.Entities;
using Microsoft.Extensions.ObjectPool;

namespace ArquivosDoDisco.UseCase
{
    public static class FileManager
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr FindFirstFile(string lpFileName, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool FindNextFile(IntPtr hFindFile, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FindClose(IntPtr hFindFile);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WIN32_FIND_DATA
        {
            public uint dwFileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;
        }

        private static readonly ObjectPool<MyDiskItemEntity> FolderPool = new DefaultObjectPool<MyDiskItemEntity>(new DefaultPooledObjectPolicy<MyDiskItemEntity>());

        public static async Task<MyDiskItemEntity> ListFoldersAndFilesAsync(string path)
        {
            var rootFolder = new MyDiskItemEntity
            {
                Name = "root",
                FullPath = path,
                Children = new List<MyDiskItemEntity>()
            };

            await ListFolderContentsAsync(rootFolder);

            rootFolder.SortChildrenBySize();

            DriverFind.SaveStructureAsJson(rootFolder);

            return rootFolder;
        }


        private static async Task ListFolderContentsAsync(MyDiskItemEntity folder)
        {
            WIN32_FIND_DATA findData;
            IntPtr findHandle = FindFirstFile(Path.Combine(folder.FullPath, "*"), out findData);

            if (findHandle != IntPtr.Zero)
            {
                List<Task> tasks = new List<Task>();

                do
                {
                    if (findData.cFileName == "." || findData.cFileName == "..") continue;

                    if ((findData.dwFileAttributes & 0x10) == 0x10) // Diretório
                    {
                        TakeFolders(folder, findData, tasks);
                    }
                    else // Arquivo
                    {
                        TakeFiles(folder, findData);
                    }
                }
                while (FindNextFile(findHandle, out findData));

                FindClose(findHandle);

                await Task.WhenAll(tasks.ToArray());

                folder.UpdateFolderSize();
            }
        }


        private static void TakeFolders(MyDiskItemEntity folder, WIN32_FIND_DATA findData, List<Task> tasks)
        {
            var subFolder = new MyDiskItemEntity
            {
                Name = findData.cFileName,
                FullPath = Path.Combine(folder.FullPath, findData.cFileName),
                Children = new List<MyDiskItemEntity>()
            };

            folder.Children.Add(subFolder);

            tasks.Add(Task.Run(async () =>
            {
                await ListFolderContentsAsync(subFolder);
                FolderPool.Return(subFolder);
            }));
        }

        private static void TakeFiles(MyDiskItemEntity folder, WIN32_FIND_DATA findData)
        {
            long fileSize = ((long)findData.nFileSizeHigh << 32) + findData.nFileSizeLow;

            var file = new MyDiskItemEntity
            {
                Name = findData.cFileName,
                Size = fileSize,
                Extension = Path.GetExtension(findData.cFileName),
                FullPath = Path.Combine(folder.FullPath, findData.cFileName)
            };
            folder.Children.Add(file);
        }

    }
}
