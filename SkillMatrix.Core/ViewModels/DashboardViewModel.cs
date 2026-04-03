using SkillMatrix.Core.Models;

namespace SkillMatrix.Core.ViewModels{
    public class DashboardViewModel
    {
        public int TotalConsultants { get; set; }
        public int EnMissionCount { get; set; }
        public int DisponibleCount { get; set; }
        public int IntercontratCount { get; set; }
        public int MissionsActivesAujourdHui {get; set;}
        public int ConsultantsSansCompetence {get; set;}
        public double TauxDisponibilite {get; set;}
        public double TauxOccupation {get; set;}
        public List<SkillStat> TopSkills { get; set; } = new();        
        public List<Consultant> RecentConsultants { get; set; } = new();
    }

    public class SkillStat
    {
        public string? SkillNom { get; set; }
        public int Count { get; set; }
    }
}