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
            Entities.MyDiskItemEntity structure = await FileManager.ListFoldersAndFilesAsync($"{selectedDrive}:/");



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
