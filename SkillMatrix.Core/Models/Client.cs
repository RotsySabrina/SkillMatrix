namespace SkillMatrix.Core.Models
{
    public class Client
    {
        public int Id { get; set; }
        public string Nom { get; set; } = "";
        public string? SecteurActivite { get; set; }
        public string? Ville { get; set; }
        public List<Mission> Missions { get; set; } = new();
    }
}