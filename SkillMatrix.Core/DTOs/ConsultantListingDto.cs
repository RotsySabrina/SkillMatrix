namespace SkillMatrix.Core.DTOs
{
    public class ConsultantListingDto
    {
        public int Id { get; set; }
        public string NomComplet { get; set; }
        public string Titre { get; set; }
        public string Statut { get; set; } 
        public List<string> Competences { get; set; } = new List<string>();
    }
}