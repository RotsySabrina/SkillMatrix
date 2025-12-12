namespace SkillMatrix.Core.Models
{
    public class Skill
    {
        public int Id { get; set; }
        public string Nom { get; set; } = "";
        public ICollection<ConsultantSkill>? ConsultantSkills { get; set; }
    }
}
