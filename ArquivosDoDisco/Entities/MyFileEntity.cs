namespace ArquivosDoDisco.Entities
{
    public class MyFileEntity
    {
        public string Name { get; set; }
        public long Size { get; set; }
        public string Extension { get; set; }
        public string FullPath { get; set; }

        public override string ToString()
        {
            return FullPath;
        }
    }
}
