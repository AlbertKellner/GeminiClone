namespace ArquivosDoDisco.Dto
{
    public class Node
    {
        public string name { get; set; }
        public double value { get; set; }
        public string color { get; set; }
        public List<Node> children { get; set; }
    }
}
