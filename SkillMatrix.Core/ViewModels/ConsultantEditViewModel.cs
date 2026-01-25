using SkillMatrix.Core.Models;


namespace SkillMatrix.Core.ViewModels
{
    public class ConsultantEditViewModel
    {
        public Consultant Consultant { get; set; } = new();
        
        public List<Client> ClientsList { get; set; } = new();
        
        public List<Skill> AvailableSkills { get; set; } =new();
    }
}