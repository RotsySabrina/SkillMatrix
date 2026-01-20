namespace SkillMatrix.Core.Models
{
    public class Mission
    {
        public int Id { get; set; }
        public string TitreProjet { get; set; } = "";
        public string? RoleOccupe { get; set; } 
        public string? Description { get; set; }
        public DateTime DateDebut { get; set; }
        public DateTime? DateFin { get; set; } 

        public int ConsultantId { get; set; }
        public Consultant? Consultant { get; set; }

        public int ClientId { get; set; }
        public Client? Client { get; set; }

        public List<MissionSkill> MissionSkills { get; set; } = new();
    }
}