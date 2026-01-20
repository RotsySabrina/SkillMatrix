namespace SkillMatrix.Core.Models
{
    public class MissionSkill
    {
        public int MissionId { get; set; }
        public Mission? Mission { get; set; }

        public int SkillId { get; set; }
        public Skill? Skill { get; set; }
    }
}