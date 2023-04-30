using System.Runtime.InteropServices;
using System.Text.Json;
using ArquivosDoDisco.Entities;
using Microsoft.Extensions.ObjectPool;
using System.Text.Json;
using System.Text.Json.Serialization;

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

        public static void SaveStructureAsJson(MyDiskItemEntity structure)
        {
            const string jsonFilePath = "C:\\Users\\Albert\\OneDrive\\Git\\AlbertKellner\\GeminiClone\\structure.json";
            string json = ConvertToJson(structure);

            //var json = JsonConvert.SerializeObject(structure, Formatting.Indented);

            File.WriteAllText(jsonFilePath, json);
        }

        public static string ConvertToJson(MyDiskItemEntity structure)
        {
            var options = new JsonSerializerOptions
            {
                IncludeFields = true,
                PropertyNameCaseInsensitive = true,
                //MaxDepth = 2,
            };
            var json = JsonSerializer.Serialize(structure, options);

            return json;
        }

        public static MyDiskItemEntity FindFolder(MyDiskItemEntity structure, string folderToFindPath)
        {
            string[] pathSegments = folderToFindPath.Split(Path.DirectorySeparatorChar);
            MyDiskItemEntity foundFolder = structure.FindFolder(pathSegments);

            return foundFolder;
        }
    }
}
