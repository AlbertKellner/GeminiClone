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
        public long Color { get; set; }
        [JsonIgnore]
        public string FullPath { get; set; }
        [JsonIgnore]
        public string Extension { get; set; }
        [JsonIgnore]
        public bool IsFolder => string.IsNullOrEmpty(Extension);

        [JsonPropertyName("children")]
        [JsonInclude]
        public List<MyDiskItemEntity> Children { get; set; }

        public void UpdateFolderSize()
        {
            if (IsFolder)
            {
                Size = Children.Sum(child => child.Size);
            }
        }


        public void SortChildrenBySize()
        {
            if (Children != null)
            {
                Children.Sort((child1, child2) => child2.Size.CompareTo(child1.Size));
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
