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
        private static readonly List<string> predefinedColors = new List<string>
        {
            "#D32F2F", // Flat Red
            "#1976D2", // Flat Blue
            "#388E3C", // Flat Green            
            "#AB47BC", // Flat Magenta
            "#26C6DA", // Flat Cyan
            "#757575", // Flat Black
            "#BDBDBD", // Flat White
            "#5D4037", // Flat Maroon
            "#2E7D32", // Flat Dark Green
            "#1565C0", // Flat Navy
            "#AFB42B", // Flat Olive
            "#7B1FA2", // Flat Purple
            "#00897B", // Flat Teal
            "#BDBDBD", // Flat Silver
            "#F44336", // Flat Red 2
            "#E91E63", // Flat Pink
            "#9C27B0", // Flat Purple 2
            "#673AB7", // Flat Deep Purple
            "#3F51B5", // Flat Indigo
            "#2196F3", // Flat Blue 2
            "#03A9F4", // Flat Light Blue
            "#00BCD4", // Flat Cyan 2
            "#009688", // Flat Teal 2
            "#4CAF50", // Flat Green 2
            "#8BC34A", // Flat Light Green
            "#CDDC39", // Flat Lime
            "#FFEB3B", // Flat Yellow 2
            "#FFC107", // Flat Amber
            "#FF9800", // Flat Orange
            "#FF5722", // Flat Deep Orange
            "#795548", // Flat Brown
            "#9E9E9E", // Flat Grey
            "#607D8B", // Flat Blue Grey
            "#E53935", // Flat Red 3
            "#D81B60", // Flat Pink 2
            "#8E24AA", // Flat Purple 3
            "#5E35B1", // Flat Deep Purple 2
            "#3949AB", // Flat Indigo 2
            "#1E88E5", // Flat Blue 3
            "#039BE5", // Flat Light Blue 2
            "#00ACC1", // Flat Cyan 3
            "#00897B", // Flat Teal 3
            "#43A047", // Flat Green 3
            "#7CB342", // Flat Light Green 2
            "#C0CA33"  // Flat Lime 2
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

            if (structure.Children != null)
            {
                structure.SortChildrenBySize(structure);
                //structure.Children = structure.Children.OrderByDescending(item => item.Size).ToList();
            }

            int totalDescendants = structure.Children != null ? CountDescendants(structure.Children) : 0;

            if (structure?.Children != null)
            {
                for (int i = 0; i < structure.Children.Count; i++)
                {
                    AddColorToChildren(structure.Children[i], GenerateBaseColor(i), totalDescendants);
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

        private void AddColorToChildren(Entities.MyDiskItemEntity item, string color, int totalDescendants, int depth = 1)
        {
            double percentage = depth / (totalDescendants * 1.2) * 80;

            item.Color = InterpolateToGrey(color, percentage);

            if (item.Children == null || !item.Children.Any())
            {
                item.Color = Desaturate(item.Color, 80.0);
                return;
            }

            int childIndex = 0;
            foreach (var child in item.Children)
            {
                if (childIndex % 2 == 0)
                {
                    AddColorToChildren(child, Saturate(item.Color, 10), totalDescendants, depth + 1);
                }
                else
                {
                    AddColorToChildren(child, Saturate(item.Color, 30), totalDescendants, depth + 1);
                }
                childIndex++;
            }
        }

        private string GenerateBaseColor(int index)
        {
            double lighterPercentage = 0.4;

            string hex = predefinedColors[index % predefinedColors.Count];

            Color color = ColorTranslator.FromHtml(hex);

            Color lighterColor = Color.FromArgb(color.A, Math.Min(color.R + (int)(lighterPercentage * 255), 255), Math.Min(color.G + (int)(lighterPercentage * 255), 255), Math.Min(color.B + (int)(lighterPercentage * 255), 255));
            
            return $"#{lighterColor.R:X2}{lighterColor.G:X2}{lighterColor.B:X2}";
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
            Color originalColor = ColorTranslator.FromHtml(hexColor);

            double r = originalColor.R;
            double g = originalColor.G;
            double b = originalColor.B;

            double grey = (r + g + b) / 3;

            r = r + (grey - r) * percentage / 100;
            g = g + (grey - g) * percentage / 100;
            b = b + (grey - b) * percentage / 100;

            Color interpolatedColor = Color.FromArgb((int)r, (int)g, (int)b);

            return "#" + interpolatedColor.R.ToString("X2") + interpolatedColor.G.ToString("X2") + interpolatedColor.B.ToString("X2");
        }

        private int CountDescendants(List<Entities.MyDiskItemEntity> items, int currentDepth = 0)
        {
            if (items == null || !items.Any())
            {
                return currentDepth;
            }
            else
            {
                // Recursivamente obtenha a profundidade de cada item filho e retorne a profundidade máxima
                return items.Max(item => CountDescendants(item.Children, currentDepth + 1));
            }
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

        public static string Saturate(string hexColor, double percentage)
        {
            Color originalColor = ColorTranslator.FromHtml(hexColor);

            // Convert from RGB to HSL
            double h, s, l;
            RgbToHsl(originalColor.R, originalColor.G, originalColor.B, out h, out s, out l);

            // Increase the saturation
            s += (1 - s) * (percentage / 100.0);

            // Convert back to RGB
            double r, g, b;
            HslToRgb(h, s, l, out r, out g, out b);

            Color saturatedColor = Color.FromArgb((int)r, (int)g, (int)b);

            return "#" + saturatedColor.R.ToString("X2") + saturatedColor.G.ToString("X2") + saturatedColor.B.ToString("X2");
        }

        public static string Desaturate(string hexColor, double percentage)
        {
            Color originalColor = ColorTranslator.FromHtml(hexColor);

            // Converter de RGB para HSL
            double h, s, l;
            RgbToHsl(originalColor.R, originalColor.G, originalColor.B, out h, out s, out l);

            // Reduzir a saturação
            s -= s * (percentage / 100.0);

            // Converter de volta para RGB
            double r, g, b;
            HslToRgb(h, s, l, out r, out g, out b);

            Color desaturatedColor = Color.FromArgb((int)r, (int)g, (int)b);

            return "#" + desaturatedColor.R.ToString("X2") + desaturatedColor.G.ToString("X2") + desaturatedColor.B.ToString("X2");
        }

        private static void RgbToHsl(int r, int g, int b, out double h, out double s, out double l)
        {
            double R = r / 255.0;
            double G = g / 255.0;
            double B = b / 255.0;

            double max = Math.Max(R, Math.Max(G, B));
            double min = Math.Min(R, Math.Min(G, B));

            l = (max + min) / 2.0;

            if (max == min)
            {
                h = s = 0;
            }
            else
            {
                double d = max - min;
                s = l > 0.5 ? d / (2.0 - max - min) : d / (max + min);

                if (max == R)
                {
                    h = (G - B) / d + (G < B ? 6 : 0);
                }
                else if (max == G)
                {
                    h = (B - R) / d + 2;
                }
                else
                {
                    h = (R - G) / d + 4;
                }

                h /= 6;
            }
        }

        private static void HslToRgb(double h, double s, double l, out double r, out double g, out double b)
        {
            if (s == 0)
            {
                r = g = b = l;
            }
            else
            {
                Func<double, double, double, double> hue2rgb = (p, q, t) =>
                {
                    if (t < 0) t += 1;
                    if (t > 1) t -= 1;
                    if (t < 1 / 6.0) return p + (q - p) * 6 * t;
                    if (t < 1 / 2.0) return q;
                    if (t < 2 / 3.0) return p + (q - p) * (2 / 3.0 - t) * 6;
                    return p;
                };

                double q = l < 0.5 ? l * (1 + s) : l + s - l * s;
                double p = 2 * l - q;

                r = hue2rgb(p, q, h + 1 / 3.0);
                g = hue2rgb(p, q, h);
                b = hue2rgb(p, q, h - 1 / 3.0);
            }

            r = Math.Round(r * 255);
            g = Math.Round(g * 255);
            b = Math.Round(b * 255);
        }
    }
}
