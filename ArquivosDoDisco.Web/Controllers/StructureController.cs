﻿using ArquivosDoDisco.Entities;
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

            int totalDescendants = CountDescendants(structure.Children);

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
            double percentage = 80.0 * depth / totalDescendants;

            // Atribuir a cor base a este item
            item.Color = InterpolateToGrey(color, percentage);  // Aumente a porcentagem de cinza com base no número total de descendentes

            if (item.Children == null)
            {
                return;
            }

            foreach (var child in item.Children)
            {
                // Atribuir a cor base a cada criança e repetir o processo recursivamente
                AddColorToChildren(child, item.Color, totalDescendants, depth + 1);  
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
