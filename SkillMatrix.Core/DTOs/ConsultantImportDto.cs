namespace SkillMatrix.Core.DTOs
{
    public class ConsultantImportDto
    {
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public string Titre { get; set; }
        public int ExperienceTotale { get; set; }
        public string Statut { get; set; }
        public string CompetencesString { get; set; } 
    }
}