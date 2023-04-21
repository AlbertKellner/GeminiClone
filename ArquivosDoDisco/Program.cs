using Newtonsoft.Json;
using ArquivosDoDisco.Entities;
using ArquivosDoDisco.UseCase;
using System.Reflection;

class Program
{
    static void Main(string[] args)
    {
        string driveC = TakeAllDrives();

        MyFolderEntity structure = ReadStructure(driveC);
        
        SaveStructureAsJson(structure);

        //ShowStructure(structure);

        ShowTotalSizePerExtension(structure);

        string folderToFindPath = @"ArquivosDoDisco";

        MyFolderEntity foundFolder = FindFolder(structure, folderToFindPath);
        ShowFoundFolder(foundFolder);

        Console.ReadKey();
    }

    private static string TakeAllDrives()
    {
        var drives = DriveInfo.GetDrives();

        var physicalDrives = drives.Where(d => d.DriveType == DriveType.Fixed);

        foreach (var drive in physicalDrives)
        {
            Console.WriteLine($"Drive: {drive.Name}, Type: {drive.DriveType}, Size: {drive.TotalSize}");
        }

        return physicalDrives.First().Name;
    }

    private static void SaveStructureAsJson(MyFolderEntity structure)
    {
        //var solutionFolderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        //var jsonFilePath = Path.Combine(solutionFolderPath, "structure.json");
        const string jsonFilePath = "C:\\Users\\Albert\\OneDrive\\Git\\AlbertKellner\\GeminiClone\\structure.json";

        var json = JsonConvert.SerializeObject(structure, Formatting.Indented);
        File.WriteAllText(jsonFilePath, json);
    }


    private static void ShowFoundFolder(MyFolderEntity foundFolder)
    {
        if (foundFolder != null)
        {
            Console.WriteLine($"\n\nEstrutura em JSON:\n{JsonConvert.SerializeObject(foundFolder, Formatting.Indented)}");
        }
        else
        {
            Console.WriteLine("Folder not found");
        }
    }

    private static MyFolderEntity FindFolder(MyFolderEntity structure, string folderToFindPath)
    {
        string[] pathSegments = folderToFindPath.Split(Path.DirectorySeparatorChar);
        MyFolderEntity foundFolder = structure.FindFolder(pathSegments);

        return foundFolder;
    }

    private static void ShowStructure(MyFolderEntity structure)
    {
        Console.WriteLine($"\n\nEstrutura em JSON:\n{JsonConvert.SerializeObject(structure, Formatting.Indented)}");
    }

    private static MyFolderEntity ReadStructure(string path)
    {
        MyFolderEntity myFolderEntity = FileManager.ListFoldersAndFiles(path);
        
        //myFolderEntity.SortFilesBySize();
        //myFolderEntity.SortFoldersBySize();
        //myFolderEntity.SortFilesByExtension();

        return myFolderEntity;

    }

    private static void ShowTotalSizePerExtension(MyFolderEntity structure)
    {
        List<ExtensionSummaryEntity> totalSizePerExtension = structure.GetTotalSizePerExtension();
        Console.WriteLine($"\n\nGetTotalSizePerExtension:\n{JsonConvert.SerializeObject(totalSizePerExtension, Formatting.Indented)}");
    }
}
