using Newtonsoft.Json;
using ArquivosDoDisco.Entities;
using ArquivosDoDisco.UseCase;
using System.Reflection;
using System.Xml.Linq;

class Program
{
    static async Task Main(string[] args)
    {
        string driveToScan = GetDrive();

        MyFolderEntity structure = await FileManager.ListFoldersAndFilesAsync(driveToScan);

        SaveStructureAsJson(structure);

        //string folderToFindPath = @"Microsoft Visual Studio\2022\Community\Common7\IDE\1033";
        //string folderToFindPath = @"Teste";

        //MyFolderEntity foundFolder = FindFolder(structure, folderToFindPath);

        //SaveStructureAsJson(foundFolder);
        //ShowFoundFolder(foundFolder);

        //Console.ReadKey();
    }

    private static string GetDrive()
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

    private static void SaveStructureAsJson(MyFolderEntity structure)
    {
        const string jsonFilePath = "C:\\Users\\Albert\\OneDrive\\Git\\AlbertKellner\\GeminiClone\\structure.json";

        var json = JsonConvert.SerializeObject(structure, Formatting.Indented);

        File.WriteAllText(jsonFilePath, json);
    }

    private static MyFolderEntity FindFolder(MyFolderEntity structure, string folderToFindPath)
    {
        string[] pathSegments = folderToFindPath.Split(Path.DirectorySeparatorChar);
        MyFolderEntity foundFolder = structure.FindFolder(pathSegments);

        return foundFolder;
    }
}
