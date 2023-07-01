using ArquivosDoDisco.Entities;
using ArquivosDoDisco.UseCase;
using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Net;
using System.Text.Json;

namespace ArquivosDoDisco.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public class StructureController : Controller
    {
        private readonly List<string> predefinedColors = new List<string>
{
    "#FFCCCC", // Light Red
    "#CCFFCC", // Light Green
    "#CCCCFF", // Light Blue
    "#FFFFCC", // Light Yellow
    "#FFCCFF", // Light Magenta
    "#FFDAB9", // Peach Puff
    "#E6E6FA", // Lavender
    "#FFF0F5", // Lavender Blush
    "#D8BFD8", // Thistle
    "#FFE4E1", // Misty Rose
    "#FFF5EE", // Seashell
    "#F0FFF0", // Honeydew
    "#F5FFFA", // Mint Cream
    "#F0F8FF", // Alice Blue
    "#F0E68C", // Khaki
    "#E6E6FA", // Lavender
    "#B0E0E6", // Powder Blue
    "#FAFAD2", // Light Goldenrod Yellow
    "#FFEFD5", // Papaya Whip
    "#FFE4B5", // Moccasin
};


        [HttpGet()]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public IActionResult GetDrivesToScanAsync()
        {
            var driveToScan = DriverFind.GetDriveNames();

            if (driveToScan != null)
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = false
                };
                var jsonString = JsonSerializer.Serialize(driveToScan, jsonOptions);
                return Content(jsonString, "application/json");
            }
            else
            {
                return NoContent();
            }
        }

        [HttpGet("{selectedDrive}")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task<IActionResult> GetAllByDiskAsync(
            [FromRoute] string selectedDrive)
        {
            MyDiskItemEntity structure = await FileManager.ListFoldersAndFilesAsync($"{selectedDrive}:/");

            if (structure?.Children != null)
            {
                for (int i = 0; i < structure.Children.Count; i++)
                {
                    AddColorToChildren(structure.Children[i], GenerateBaseColor(i));
                }
            }


            //RemoveNonFoldersRecursively(structure);
            //RemoveFoldersRecursively(structure, 0);

            if (structure != null)
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = false
                };
                var jsonString = JsonSerializer.Serialize(structure, jsonOptions);
                return Content(jsonString, "application/json");
            }
            else
            {
                return NoContent();
            }
        }

        private void AddColorToChildren(Entities.MyDiskItemEntity item, string color)
        {
            // Atribuir a cor base a este item
            item.Color = color;

            if (item.Children == null)
            {
                return;
            }

            foreach (var child in item.Children)
            {
                // Atribuir a cor base a cada criança e repetir o processo recursivamente
                AddColorToChildren(child, color);
            }
        }

        private string GenerateBaseColor(int index)
        {
            // Use o índice para selecionar uma cor da lista de cores predefinidas.
            // Use o operador % para garantir que o índice esteja dentro do intervalo da lista de cores.
            return predefinedColors[index % predefinedColors.Count];
        }



        [HttpGet("{selectedDrive}/folder/{selectedFolder}")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task<IActionResult> GetByFolderAsync(
            [FromRoute] string selectedDrive,
            [FromRoute] string selectedFolder)
        {
            selectedDrive = $"{selectedDrive}:/";

            var structure = await FileManager.ListFoldersAndFilesAsync(selectedDrive);

            var result = DriverFind.FindFolder(structure, selectedFolder);

            //RemoveNonFoldersRecursively(structure);

            if (result != null)
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = false
                };
                var jsonString = JsonSerializer.Serialize(result, jsonOptions);
                return Content(jsonString, "application/json");
            }
            else
            {
                return NoContent();
            }
        }

        public static string InterpolateToGrey(string hexColor, double percentage)
        {
            if (!double.TryParse(hexColor.Substring(1, 2), System.Globalization.NumberStyles.HexNumber, null, out double r) ||
                !double.TryParse(hexColor.Substring(3, 2), System.Globalization.NumberStyles.HexNumber, null, out double g) ||
                !double.TryParse(hexColor.Substring(5, 2), System.Globalization.NumberStyles.HexNumber, null, out double b))
            {
                throw new ArgumentException("Invalid color format");
            }

            double grey = (r + g + b) / 3;

            r = r + (grey - r) * percentage / 100;
            g = g + (grey - g) * percentage / 100;
            b = b + (grey - b) * percentage / 100;

            Color interpolatedColor = Color.FromArgb((int)r, (int)g, (int)b);

            return "#" + interpolatedColor.R.ToString("X2") + interpolatedColor.G.ToString("X2") + interpolatedColor.B.ToString("X2");
        }

        private void RemoveNonFoldersRecursively(Entities.MyDiskItemEntity item)
        {
            if (item.Children == null)
            {
                return;
            }

            item.Children.RemoveAll(child => !child.IsFolder);
            foreach (var child in item.Children)
            {
                RemoveNonFoldersRecursively(child);
            }
        }

        private void RemoveFoldersRecursively(MyDiskItemEntity folder, int depth = 0)
        {
            if (folder.Children == null)
            {
                return;
            }

            if (depth > 4)
            {
                folder.Children.Clear();
                return;
            }

            for (int i = folder.Children.Count - 1; i >= 0; i--)
            {
                MyDiskItemEntity child = folder.Children[i];
                if (child.IsFolder)
                {
                    RemoveFoldersRecursively(child, depth + 1);
                }
                else
                {
                    folder.Children.RemoveAt(i);
                }
            }
        }

    }
}
