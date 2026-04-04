using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SkillMatrix.Core.DTOs;
using SkillMatrix.Core.Models;
using SkillMatrix.Data.EF;
using SkillMatrix.Data.Services;

namespace SkillMatrix.Web.Areas_Admin_Pages_Consultants
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ElasticSearchService _elasticService;

        public EditModel(ApplicationDbContext context, ElasticSearchService elasticService)
        {
            _context = context;
            _elasticService = elasticService;
        }

        [BindProperty]
        public Consultant Consultant { get; set; } = default!;

        public List<Mission> ExistingMissions { get; set; } = new();
        public List<AssignedSkillData> AssignedSkills { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var consultant = await _context.Consultants
                .Include(c => c.Missions)
                    .ThenInclude(m => m.Client)
                .Include(c => c.ConsultantSkills)
                    .ThenInclude(cs => cs.Skill)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (consultant == null)
            {
                return NotFound();
            }

            ExistingMissions = consultant.Missions
                .OrderByDescending(m => m.DateDebut)
                .ToList();

            PopulateAssignedSkillData(consultant);

            Consultant = new Consultant
            {
                Id = consultant.Id,
                Nom = consultant.Nom,
                Prenom = consultant.Prenom,
                Titre = consultant.Titre,
                ExperienceTotale = consultant.ExperienceTotale,
                Statut = consultant.Statut
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string[] selectedSkills, Dictionary<string, string> SkillLevels)
        {
            if (!ModelState.IsValid)
            {
                await ReloadPageDataAsync(Consultant.Id);
                return Page();
            }

            var existingConsultant = await _context.Consultants
                .Include(c => c.Missions)
                .Include(c => c.ConsultantSkills)
                    .ThenInclude(cs => cs.Skill)
                .FirstOrDefaultAsync(c => c.Id == Consultant.Id);

            if (existingConsultant == null)
            {
                return NotFound();
            }

            existingConsultant.Nom = (Consultant.Nom ?? string.Empty).Trim();
            existingConsultant.Prenom = (Consultant.Prenom ?? string.Empty).Trim();
            existingConsultant.Titre = (Consultant.Titre ?? string.Empty).Trim();
            existingConsultant.ExperienceTotale = Consultant.ExperienceTotale;

            if (existingConsultant.ConsultantSkills != null && existingConsultant.ConsultantSkills.Any())
            {
                _context.ConsultantSkills.RemoveRange(existingConsultant.ConsultantSkills);
            }

            existingConsultant.ConsultantSkills = new List<ConsultantSkill>();

            if (selectedSkills != null)
            {
                foreach (var skillIdString in selectedSkills)
                {
                    if (int.TryParse(skillIdString, out int skillId))
                    {
                        int niveau = 1;

                        if (SkillLevels.TryGetValue(skillIdString, out string? niveauString) &&
                            int.TryParse(niveauString, out int parsedNiveau))
                        {
                            niveau = parsedNiveau;
                        }

                        existingConsultant.ConsultantSkills.Add(new ConsultantSkill
                        {
                            ConsultantId = existingConsultant.Id,
                            SkillId = skillId,
                            Niveau = niveau,
                            DerniereUtilisation = DateTime.Now
                        });
                    }
                }
            }

            UpdateConsultantStatus(existingConsultant);

            try
            {
                await _context.SaveChangesAsync();

                var updatedConsultant = await _context.Consultants
                    .Include(c => c.ConsultantSkills)
                        .ThenInclude(cs => cs.Skill)
                    .FirstOrDefaultAsync(c => c.Id == existingConsultant.Id);

                if (updatedConsultant != null)
                {
                    var searchDto = new SearchConsultantDto
                    {
                        Id = updatedConsultant.Id,
                        NomComplet = $"{updatedConsultant.Prenom} {updatedConsultant.Nom}",
                        Titre = updatedConsultant.Titre,
                        Statut = updatedConsultant.Statut,
                        Competences = updatedConsultant.ConsultantSkills
                            .Where(cs => cs.Skill != null)
                            .Select(cs => cs.Skill!.Nom)
                            .ToList()
                    };

                    await _elasticService.IndexConsultantAsync(searchDto);
                }

                TempData["SuccessMessage"] = "Consultant mis à jour avec succès.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ConsultantExists(Consultant.Id))
                {
                    return NotFound();
                }

                throw;
            }

            return RedirectToPage("./Index");
        }

        private async Task ReloadPageDataAsync(int consultantId)
        {
            var consultant = await _context.Consultants
                .Include(c => c.Missions)
                    .ThenInclude(m => m.Client)
                .Include(c => c.ConsultantSkills)
                    .ThenInclude(cs => cs.Skill)
                .FirstOrDefaultAsync(c => c.Id == consultantId);

            if (consultant != null)
            {
                ExistingMissions = consultant.Missions
                    .OrderByDescending(m => m.DateDebut)
                    .ToList();

                PopulateAssignedSkillData(consultant);
            }
        }

        private void PopulateAssignedSkillData(Consultant consultant)
        {
            var allSkills = _context.Skills.ToList();

            var consultantSkills = consultant.ConsultantSkills?
                .ToDictionary(cs => cs.SkillId, cs => cs.Niveau)
                ?? new Dictionary<int, int>();

            AssignedSkills = allSkills.Select(skill => new AssignedSkillData
            {
                SkillId = skill.Id,
                Name = skill.Nom,
                Assigned = consultantSkills.ContainsKey(skill.Id),
                Level = consultantSkills.ContainsKey(skill.Id) ? consultantSkills[skill.Id] : 3
            }).ToList();
        }

        private bool ConsultantExists(int id)
        {
            return _context.Consultants.Any(e => e.Id == id);
        }

        private void UpdateConsultantStatus(Consultant consultant)
        {
            var today = DateTime.Today;
            var missions = consultant.Missions ?? new List<Mission>();

            bool isOnMission = missions.Any(m =>
                m.DateDebut <= today &&
                (m.DateFin == null || m.DateFin >= today));

            if (isOnMission)
            {
                consultant.Statut = "En Mission";
            }
            else
            {
                bool hasWorkedBefore = missions.Any(m => m.DateFin != null && m.DateFin < today);
                consultant.Statut = hasWorkedBefore ? "Intercontrat" : "Disponible";
            }
        }
    }
}