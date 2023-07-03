using System.Text.Json.Serialization;

namespace ArquivosDoDisco.Entities
{
    public class MyDiskItemEntity
    {
        [JsonInclude]
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonInclude]
        [JsonPropertyName("value")]
        public long Size { get; set; }
        [JsonInclude]
        [JsonPropertyName("color")]
        public string Color { get; set; }
        [JsonIgnore]
        public string FullPath { get; set; }
        [JsonIgnore]
        public string Extension { get; set; }
        [JsonIgnore]
        public bool IsFolder => string.IsNullOrEmpty(Extension);

        [JsonPropertyName("children")]
        [JsonInclude]
        public List<MyDiskItemEntity> Children { get; set; }

        [JsonPropertyName("formattedSize")]
        public string FormattedSize
        {
            get
            {
                string[] sizes = { "B", "KB", "MB", "GB", "TB" };
                double len = Size;
                int order = 0;
                while (len >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    len = len / 1024;
                }

                // Adjust the format string to your preferences. For example "{0:0.##} {1}" would
                // show a maximum of two decimal places, and no decimal places if the number is a whole number.
                return String.Format("{0:0.##} {1}", len, sizes[order]);
            }
        }

        public override string ToString()
        {
            return FullPath;
        }

        public void UpdateFolderSize()
        {
            if (IsFolder)
            {
                Size = Children.Sum(child => child.Size);
            }
        }


        public void SortChildrenBySize(Entities.MyDiskItemEntity item)
        {
            if (item.Children == null)
            {
                return;
            }

            // Sort in descending order
            item.Children.Sort((a, b) => b.Size.CompareTo(a.Size));

            foreach (var child in item.Children)
            {
                SortChildrenBySize(child);
            }
        }


        public List<ExtensionSummaryEntity> GetTotalSizePerExtension()
        {
            return Children.GroupBy(file => file.Extension)
                        .Select(group =>
                        {
                            long minSize = group.Min(file => file.Size);
                            long maxSize = group.Max(file => file.Size);

                            double sizeVariationPercentage = minSize != maxSize ? (double)(maxSize - minSize) / minSize * 100 : 0;

                            return new ExtensionSummaryEntity
                            {
                                Extension = group.Key,
                                TotalSize = group.Sum(file => file.Size),
                                ItemCount = group.Count(),
                                AverageSize = group.Average(file => file.Size),
                                SizeVariationPercentage = sizeVariationPercentage
                            };
                        })
                        .ToList();
        }


        public MyDiskItemEntity FindFolder(IList<string> pathSegments)
        {
            if (pathSegments == null || pathSegments.Count == 0)
            {
                return this;
            }

            string targetFolderName = pathSegments[0];

            MyDiskItemEntity targetFolder = Children.FirstOrDefault(child => child.Name.Equals(targetFolderName, StringComparison.OrdinalIgnoreCase) && child.IsFolder);

            if (targetFolder == null)
            {
                return null;
            }

            return targetFolder.FindFolder(pathSegments.Skip(1).ToList());
        }


    }
}
