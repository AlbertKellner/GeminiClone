using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArquivosDoDisco.Entities
{
    public class MyFolderEntity
    {
        public string Name { get; set; }
        public string FullPath { get; set; }
        public long TotalSize { get; set; }
        public List<MyFolderEntity> Folders { get; set; }
        public List<MyFileEntity> Files { get; set; }

        public void SortFilesBySize()
        {
            Files.Sort((file1, file2) => file1.Size.CompareTo(file2.Size));
        }

        public void SortFoldersBySize()
        {
            Folders.Sort((folder1, folder2) => folder1.TotalSize.CompareTo(folder2.TotalSize));
        }

        public void SortFilesByExtension()
        {
            Files.Sort((file1, file2) => string.Compare(file1.Extension, file2.Extension, StringComparison.OrdinalIgnoreCase));
        }

        public List<ExtensionSummaryEntity> GetTotalSizePerExtension()
        {
            return Files.GroupBy(file => file.Extension)
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


        public MyFolderEntity FindFolder(IList<string> pathSegments)
        {
            if (pathSegments == null || pathSegments.Count == 0)
            {
                return this;
            }

            string targetFolderName = pathSegments[0];

            MyFolderEntity targetFolder = Folders.FirstOrDefault(folder => folder.Name.Equals(targetFolderName, StringComparison.OrdinalIgnoreCase));

            if (targetFolder == null)
            {
                return null;
            }

            return targetFolder.FindFolder(pathSegments.Skip(1).ToList());
        }

    }
}
