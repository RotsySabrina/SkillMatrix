namespace SkillMatrix.Core.DTOs
{
    public class ConsultantImportDto
    {
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public string Titre { get; set; }
        public int ExperienceTotale { get; set; }
        public string Statut { get; set; }
        
        // Cette colonne contiendra "C#|3;Azure|2"
        public string CompetencesString { get; set; } 
    }
}