using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices; // Para MethodImpl AggressiveInlining
using System.Threading.Tasks;
using System.Buffers; // Para ArrayPool
using ArquivosDoDisco.Entities;

namespace ArquivosDoDisco.UseCase
{
    public static class FileManager
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr FindFirstFile(string lpFileName, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool FindNextFile(IntPtr hFindFile, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FindClose(IntPtr hFindFile);

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

        public static Task<MyDiskItemEntity> ListFoldersAndFilesAsync(string path)
        {
            return Task.Run(() =>
            {
                // Garantir que não termine com barra
                if (path.EndsWith("\\"))
                    path = path.TrimEnd('\\');

                var rootFolder = new MyDiskItemEntity
                {
                    Name = "root",
                    FullPath = path,
                    Children = new List<MyDiskItemEntity>(),
                    Size = 0
                };

                ListFolderContents(rootFolder);

                // Ordenar no final se necessário
                rootFolder.SortChildrenBySize(rootFolder);
                return rootFolder;
            });
        }

        // Controlar grau de paralelismo
        private static readonly ParallelOptions parallelOptions = new ParallelOptions
        {
            // Reduzir ao mínimo para diminuir uso de CPU (p.ex.: 1 ou 2 threads somente)
            MaxDegreeOfParallelism = 2
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsDirectory(uint attributes) => (attributes & 0x10) == 0x10;

        private static void ListFolderContents(MyDiskItemEntity folder)
        {
            // Para evitar Path.Combine, apenas adicionamos "*" manualmente
            string searchPath = folder.FullPath + "\\*";

            WIN32_FIND_DATA findData;
            IntPtr findHandle = FindFirstFile(searchPath, out findData);

            if (findHandle == IntPtr.Zero)
                return;

            // Usar um ArrayPool para reutilizar arrays e reduzir GC
            // Estimando que 128 é um tamanho razoável. Se for insuficiente para muitos diretórios, pode-se redimensionar depois.
            MyDiskItemEntity[] dirBuffer = ArrayPool<MyDiskItemEntity>.Shared.Rent(128);
            int dirCount = 0;

            do
            {
                var name = findData.cFileName;
                if (name == "." || name == "..") continue;

                if (IsDirectory(findData.dwFileAttributes))
                {
                    // Expandir buffer se necessário (menos ideal, mas caso encontre mais de 128 dirs)
                    if (dirCount == dirBuffer.Length)
                    {
                        MyDiskItemEntity[] newArr = ArrayPool<MyDiskItemEntity>.Shared.Rent(dirBuffer.Length * 2);
                        Array.Copy(dirBuffer, newArr, dirCount);
                        ArrayPool<MyDiskItemEntity>.Shared.Return(dirBuffer, true);
                        dirBuffer = newArr;
                    }

                    var subFolder = new MyDiskItemEntity
                    {
                        Name = name,
                        // Concatenação manual do path
                        FullPath = folder.FullPath + "\\" + name,
                        Children = new List<MyDiskItemEntity>(),
                        Size = 0
                    };
                    folder.Children.Add(subFolder);
                    dirBuffer[dirCount++] = subFolder;
                }
                else
                {
                    long fileSize = ((long)findData.nFileSizeHigh << 32) + findData.nFileSizeLow;

                    // Evitar Path.GetExtension e extrair extensão manualmente
                    string extension = null;
                    int dotIndex = name.LastIndexOf('.');
                    if (dotIndex >= 0 && dotIndex < name.Length - 1)
                    {
                        extension = name.Substring(dotIndex);
                    }

                    var file = new MyDiskItemEntity
                    {
                        Name = name,
                        Size = fileSize,
                        Extension = extension,
                        FullPath = folder.FullPath + "\\" + name
                    };

                    folder.Children.Add(file);
                    folder.Size += fileSize; // Incremental
                }
            }
            while (FindNextFile(findHandle, out findData));

            FindClose(findHandle);

            if (dirCount > 0)
            {
                // Convertendo para índice já que usamos Parallel.For
                Parallel.For(0, dirCount, parallelOptions, i =>
                {
                    ListFolderContents(dirBuffer[i]);
                });

                // Somar tamanhos das subpastas
                for (int i = 0; i < dirCount; i++)
                {
                    folder.Size += dirBuffer[i].Size;
                }
            }

            ArrayPool<MyDiskItemEntity>.Shared.Return(dirBuffer, true);
        }

    }
}
