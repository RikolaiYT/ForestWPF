namespace ForestWPF.Models
{
    public class Tree
    {
        public string Species { get; set; } = string.Empty;
        public double Height { get; set; }
        public double Diameter { get; set; }
        public string WoodType { get; set; } = string.Empty;
        public int Age { get; set; }
    }
}
