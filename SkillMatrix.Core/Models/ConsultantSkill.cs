using System.ComponentModel.DataAnnotations;

namespace SkillMatrix.Core.Models
{
    public class ConsultantSkill
    {
        public int ConsultantId { get; set; }
        public Consultant? Consultant { get; set; }

        public int SkillId { get; set; }
        public Skill? Skill { get; set; }

        [Range(1,5)]
        public int Niveau { get; set; }

        public DateTime? DerniereUtilisation { get; set; }
    }
}
