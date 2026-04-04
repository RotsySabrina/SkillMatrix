using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SkillMatrix.Core.DTOs;
using SkillMatrix.Core.Models;
using SkillMatrix.Data.EF;
using SkillMatrix.Data.Services;

namespace SkillMatrix.Web.Areas_Admin_Pages_Consultants
{
    [Authorize(Roles = "Admin")]
    public class MissionsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ElasticSearchService _elasticService;

        public MissionsModel(ApplicationDbContext context, ElasticSearchService elasticService)
        {
            _context = context;
            _elasticService = elasticService;
        }

        public Consultant ConsultantProfile { get; set; } = default!;
        public List<Mission> ExistingMissions { get; set; } = new();
        public SelectList ClientOptions { get; set; } = default!;

        [BindProperty]
        public MissionFormModel MissionInput { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ok = await LoadPageDataAsync(id.Value);
            if (!ok)
            {
                return NotFound();
            }

            MissionInput = new MissionFormModel
            {
                DateDebut = DateTime.Today
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAddAsync(int consultantId)
        {
            if (!await LoadPageDataAsync(consultantId))
            {
                return NotFound();
            }

            if (!ValidateMissionInput())
            {
                return Page();
            }

            var mission = new Mission
            {
                TitreProjet = (MissionInput.TitreProjet ?? string.Empty).Trim(),
                ClientId = MissionInput.ClientId,
                DateDebut = MissionInput.DateDebut.Date,
                DateFin = MissionInput.DateFin?.Date,
                RoleOccupe = string.IsNullOrWhiteSpace(MissionInput.RoleOccupe) ? null : MissionInput.RoleOccupe.Trim(),
                Description = string.IsNullOrWhiteSpace(MissionInput.Description) ? null : MissionInput.Description.Trim(),
                ConsultantId = consultantId
            };

            _context.Missions.Add(mission);
            await _context.SaveChangesAsync();

            await RefreshConsultantStatusAndElasticAsync(consultantId);

            TempData["SuccessMessage"] = "Mission ajoutée avec succès.";
            return RedirectToPage(new { id = consultantId });
        }

        public async Task<IActionResult> OnPostUpdateAsync(
            int consultantId,
            int missionId,
            string titreProjet,
            int clientId,
            DateTime dateDebut,
            DateTime? dateFin,
            string? roleOccupe,
            string? description)
        {
            if (!await LoadPageDataAsync(consultantId))
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(titreProjet))
            {
                TempData["ErrorMessage"] = "Le titre du projet est obligatoire.";
                return RedirectToPage(new { id = consultantId });
            }

            if (clientId <= 0)
            {
                TempData["ErrorMessage"] = "Veuillez sélectionner un client.";
                return RedirectToPage(new { id = consultantId });
            }

            if (dateFin.HasValue && dateFin.Value.Date < dateDebut.Date)
            {
                TempData["ErrorMessage"] = "La date de fin ne peut pas être antérieure à la date de début.";
                return RedirectToPage(new { id = consultantId });
            }

            var mission = await _context.Missions
                .FirstOrDefaultAsync(m => m.Id == missionId && m.ConsultantId == consultantId);

            if (mission == null)
            {
                return NotFound();
            }

            mission.TitreProjet = titreProjet.Trim();
            mission.ClientId = clientId;
            mission.DateDebut = dateDebut.Date;
            mission.DateFin = dateFin?.Date;
            mission.RoleOccupe = string.IsNullOrWhiteSpace(roleOccupe) ? null : roleOccupe.Trim();
            mission.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

            await _context.SaveChangesAsync();

            await RefreshConsultantStatusAndElasticAsync(consultantId);

            TempData["SuccessMessage"] = "Mission mise à jour avec succès.";
            return RedirectToPage(new { id = consultantId });
        }

        public async Task<IActionResult> OnPostDeleteAsync(int consultantId, int missionId)
        {
            var mission = await _context.Missions
                .FirstOrDefaultAsync(m => m.Id == missionId && m.ConsultantId == consultantId);

            if (mission == null)
            {
                return NotFound();
            }

            _context.Missions.Remove(mission);
            await _context.SaveChangesAsync();

            await RefreshConsultantStatusAndElasticAsync(consultantId);

            TempData["SuccessMessage"] = "Mission supprimée avec succès.";
            return RedirectToPage(new { id = consultantId });
        }

        private async Task<bool> LoadPageDataAsync(int consultantId)
        {
            ConsultantProfile = await _context.Consultants
                .Include(c => c.Missions)
                    .ThenInclude(m => m.Client)
                .Include(c => c.ConsultantSkills)
                    .ThenInclude(cs => cs.Skill)
                .FirstOrDefaultAsync(c => c.Id == consultantId);

            if (ConsultantProfile == null)
            {
                return false;
            }

            ExistingMissions = ConsultantProfile.Missions
                .OrderByDescending(m => m.DateDebut)
                .ToList();

            var clients = await _context.Clients
                .OrderBy(c => c.Nom)
                .ToListAsync();

            ClientOptions = new SelectList(clients, "Id", "Nom");

            return true;
        }

        private bool ValidateMissionInput()
        {
            if (string.IsNullOrWhiteSpace(MissionInput.TitreProjet))
            {
                ModelState.AddModelError("MissionInput.TitreProjet", "Le titre du projet est obligatoire.");
            }

            if (MissionInput.ClientId <= 0)
            {
                ModelState.AddModelError("MissionInput.ClientId", "Veuillez sélectionner un client.");
            }

            if (MissionInput.DateFin.HasValue && MissionInput.DateFin.Value.Date < MissionInput.DateDebut.Date)
            {
                ModelState.AddModelError("MissionInput.DateFin", "La date de fin ne peut pas être antérieure à la date de début.");
            }

            return ModelState.IsValid;
        }

        private async Task RefreshConsultantStatusAndElasticAsync(int consultantId)
        {
            var consultant = await _context.Consultants
                .Include(c => c.Missions)
                .Include(c => c.ConsultantSkills)
                    .ThenInclude(cs => cs.Skill)
                .FirstOrDefaultAsync(c => c.Id == consultantId);

            if (consultant == null)
            {
                return;
            }

            var today = DateTime.Today;
            bool isOnMission = consultant.Missions.Any(m =>
                m.DateDebut <= today &&
                (m.DateFin == null || m.DateFin >= today));

            if (isOnMission)
            {
                consultant.Statut = "En Mission";
            }
            else
            {
                bool hasWorkedBefore = consultant.Missions.Any(m => m.DateFin != null && m.DateFin < today);
                consultant.Statut = hasWorkedBefore ? "Intercontrat" : "Disponible";
            }

            await _context.SaveChangesAsync();

            var searchDto = new SearchConsultantDto
            {
                Id = consultant.Id,
                NomComplet = $"{consultant.Prenom} {consultant.Nom}",
                Titre = consultant.Titre,
                Statut = consultant.Statut,
                Competences = consultant.ConsultantSkills
                    .Where(cs => cs.Skill != null)
                    .Select(cs => cs.Skill!.Nom)
                    .ToList()
            };

            await _elasticService.IndexConsultantAsync(searchDto);
        }
    }

    public class MissionFormModel
    {
        public string TitreProjet { get; set; } = "";
        public int ClientId { get; set; }
        public DateTime DateDebut { get; set; } = DateTime.Today;
        public DateTime? DateFin { get; set; }
        public string? RoleOccupe { get; set; }
        public string? Description { get; set; }
    }
}