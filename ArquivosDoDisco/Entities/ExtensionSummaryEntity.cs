namespace ArquivosDoDisco.Entities
{
    public class ExtensionSummaryEntity
    {
        public string Extension { get; set; }
        public long TotalSize { get; set; }
        public int ItemCount { get; set; }
        public double AverageSize { get; set; }
        public double SizeVariationPercentage { get; set; }
    }
}
