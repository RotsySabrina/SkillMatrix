using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SkillMatrix.Core.ViewModels;
using SkillMatrix.Data.EF;

namespace SkillMatrix.Web.Areas_Admin_Pages_Dashboard
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DashboardViewModel Dashboard { get; set; } = new();

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task OnGetAsync()
        {
            await RefreshStatusesAsync();

            Dashboard.TotalConsultants = await _context.Consultants.CountAsync();
            Dashboard.EnMissionCount = await _context.Consultants.CountAsync(c => c.Statut == "En Mission");
            Dashboard.DisponibleCount = await _context.Consultants.CountAsync(c => c.Statut == "Disponible");
            Dashboard.IntercontratCount = await _context.Consultants.CountAsync(c => c.Statut == "Intercontrat");

            var today = DateTime.Today;

            Dashboard.MissionsActivesAujourdHui = await _context.Missions.CountAsync(m =>
                m.DateDebut <= today &&
                (m.DateFin == null || m.DateFin >= today)
            );

            Dashboard.TopSkills = await _context.ConsultantSkills
                .Include(cs => cs.Skill)
                .GroupBy(cs => cs.Skill!.Nom)
                .Select(g => new SkillStat
                {
                    SkillNom = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();

            Dashboard.RecentConsultants = await _context.Consultants
                .OrderByDescending(c => c.Id)
                .Take(5)
                .ToListAsync();
        }

        private async Task RefreshStatusesAsync()
        {
            var today = DateTime.Today;

            var consultants = await _context.Consultants
                .Include(c => c.Missions)
                .ToListAsync();

            bool hasChanges = false;

            foreach (var consultant in consultants)
            {
                bool isOnMission = consultant.Missions.Any(m =>
                    m.DateDebut <= today &&
                    (m.DateFin == null || m.DateFin >= today));

                string newStatus;

                if (isOnMission)
                {
                    newStatus = "En Mission";
                }
                else
                {
                    bool hasPastMission = consultant.Missions.Any(m => m.DateFin != null && m.DateFin < today);
                    newStatus = hasPastMission ? "Intercontrat" : "Disponible";
                }

                if (consultant.Statut != newStatus)
                {
                    consultant.Statut = newStatus;
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                await _context.SaveChangesAsync();
            }
        }
    }
}