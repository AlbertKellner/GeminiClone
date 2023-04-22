using System.Runtime.InteropServices;
using ArquivosDoDisco.Entities;
using Microsoft.Extensions.ObjectPool;
using Newtonsoft.Json;

namespace ArquivosDoDisco.UseCase
{
    public static class DriverFind
    {
        public static List<string> GetDrives()
        {
            var drives = DriveInfo.GetDrives();
            List<string> DriveList  = new List<string>();

            //var physicalDrives = drives.Where(d => d.DriveType == DriveType.Fixed);

            foreach (var drive in drives)
            {
                DriveList.Add(drive.Name);
            }

            return DriveList;
        }

        public static string GetDrive()
        {
            var drives = DriveInfo.GetDrives();

            //var physicalDrives = drives.Where(d => d.DriveType == DriveType.Fixed);

            foreach (var drive in drives)
            {
                Console.WriteLine($"Drive: {drive.Name}, Type: {drive.DriveType}, VolumeLabel: {drive.VolumeLabel}, Size: {drive.TotalSize}");
            }

            string driveName = drives.First(x => x.Name == "C:\\").Name;

            Console.Write($"Unidade selecionada: {driveName}");

            return driveName;
        }

        public static void SaveStructureAsJson(MyFolderEntity structure)
        {
            const string jsonFilePath = "C:\\Users\\Albert\\OneDrive\\Git\\AlbertKellner\\GeminiClone\\structure.json";

            var json = JsonConvert.SerializeObject(structure, Formatting.Indented);

            File.WriteAllText(jsonFilePath, json);
        }

        public static MyFolderEntity FindFolder(MyFolderEntity structure, string folderToFindPath)
        {
            string[] pathSegments = folderToFindPath.Split(Path.DirectorySeparatorChar);
            MyFolderEntity foundFolder = structure.FindFolder(pathSegments);

            return foundFolder;
        }
    }
}
