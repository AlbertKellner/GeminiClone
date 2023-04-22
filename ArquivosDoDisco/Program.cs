using Newtonsoft.Json;
using ArquivosDoDisco.Entities;
using ArquivosDoDisco.UseCase;
using System.Reflection;
using System.Xml.Linq;

class Program
{
    static async Task Main(string[] args)
    {
        string driveToScan = DriverFind.GetDrive();

        MyFolderEntity structure = await FileManager.ListFoldersAndFilesAsync(driveToScan);

        DriverFind.SaveStructureAsJson(structure);

        //string folderToFindPath = @"Microsoft Visual Studio\2022\Community\Common7\IDE\1033";
        //string folderToFindPath = @"Teste";

        //MyFolderEntity foundFolder = FindFolder(structure, folderToFindPath);

        //SaveStructureAsJson(foundFolder);
        //ShowFoundFolder(foundFolder);

        //Console.ReadKey();
    }
}
