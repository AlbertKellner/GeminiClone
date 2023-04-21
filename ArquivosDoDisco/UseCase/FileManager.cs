using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using ArquivosDoDisco.Entities;
using ArquivosDoDisco.UseCase;

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

        public static MyFolderEntity ListFoldersAndFiles(string path)
        {
            var rootFolder = new MyFolderEntity
            {
                Name = "root",
                FullPath = path,
                Files = new List<MyFileEntity>(),
                Folders = new List<MyFolderEntity>()
            };

            ListFolderContents(rootFolder);

            return rootFolder;
        }

        private static void ListFolderContents(MyFolderEntity folder)
        {
            WIN32_FIND_DATA findData;
            IntPtr findHandle = FindFirstFile(Path.Combine(folder.FullPath, "*"), out findData);

            if (findHandle != IntPtr.Zero)
            {
                do
                {
                    if (findData.cFileName == "." || findData.cFileName == "..") continue;

                    if ((findData.dwFileAttributes & 0x10) == 0x10) // Diretório
                    {
                        var subFolder = new MyFolderEntity
                        {
                            Name = findData.cFileName,
                            FullPath = Path.Combine(folder.FullPath, findData.cFileName),
                            Files = new List<MyFileEntity>(),
                            Folders = new List<MyFolderEntity>()
                        };

                        folder.Folders.Add(subFolder);
                        ListFolderContents(subFolder);
                    }
                    else // Arquivo
                    {
                        long fileSize = ((long)findData.nFileSizeHigh << 32) + findData.nFileSizeLow;

                        var file = new MyFileEntity
                        {
                            Name = findData.cFileName,
                            Size = fileSize,
                            Extension = Path.GetExtension(findData.cFileName),
                            FullPath = Path.Combine(folder.FullPath, findData.cFileName)
                        };
                        folder.Files.Add(file);
                    }
                }
                while (FindNextFile(findHandle, out findData));

                FindClose(findHandle);
            }
        }
    }
}


//O código acima usa as funções `FindFirstFile`, `FindNextFile` e `FindClose` da WinAPI para listar os conteúdos das pastas.
//A função `ListFolderContents` é chamada recursivamente para listar as pastas e arquivos de todas as subpastas.

//Agora, você pode usar o método `ListFoldersAndFiles` desta classe para listar todas as pastas e arquivos do disco C: da seguinte maneira:

//```csharp
//string path = @"C:\";
//var rootFolder = FileManager.ListFoldersAndFiles(path);
