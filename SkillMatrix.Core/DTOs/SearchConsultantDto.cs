using System.Text.Json.Serialization;

namespace SkillMatrix.Core.DTOs
{
    public class SearchConsultantDto
    {
        public int Id { get; set; }
        public string NomComplet { get; set; }
        public string Titre { get; set; }
        public string Statut { get; set; }

        public string DescriptionProfil { get; set; }

        [JsonPropertyName("competences")] 
        public List<string> Competences { get; set; } = new List<string>();
    }
}