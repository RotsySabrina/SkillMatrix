namespace SkillMatrix.Core.Models
{
    public class Consultant
    {
        public int Id { get; set; }
        public string Nom { get; set; } = "";
        public string Prenom { get; set; } = "";
        public string Titre { get; set; } = "";
        public int ExperienceTotale { get; set; }
        public string Statut { get; set; } = "";

        public ICollection<ConsultantSkill>? ConsultantSkills { get; set; }
        public List<Mission> Missions { get; set; } = new();
    }
}
