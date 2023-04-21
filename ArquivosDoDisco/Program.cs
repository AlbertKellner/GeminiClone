using Newtonsoft.Json;
using ArquivosDoDisco.Entities;
using ArquivosDoDisco.UseCase;

class Program
{
    static void Main(string[] args)
    {
        string path = @"C:\Arquivos Pessoais\Git\Albert\ArquivosDoDisco";
        //string path = @"C:\";

        MyFolderEntity structure = ReadStructure(path);
        ShowStructure(structure);
        ShowTotalSizePerExtension(structure);

        string folderToFindPath = @"ArquivosDoDisco\ArquivosDoDisco";

        MyFolderEntity foundFolder = FindFolder(structure, folderToFindPath);
        ShowFoundFolder(foundFolder);

        Console.ReadKey();
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
