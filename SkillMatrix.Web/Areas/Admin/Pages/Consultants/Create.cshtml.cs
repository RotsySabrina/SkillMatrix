using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SkillMatrix.Core.Models;
using SkillMatrix.Data.EF;
using SkillMatrix.Data.Services;
using Microsoft.EntityFrameworkCore;
using SkillMatrix.Core.DTOs;

namespace SkillMatrix.Web.Areas_Admin_Pages_Consultants
{
    public class CreateModel : PageModel
    {
        private readonly SkillMatrix.Data.EF.ApplicationDbContext _context;
        private readonly ElasticSearchService _elasticService;
        public CreateModel(SkillMatrix.Data.EF.ApplicationDbContext context,ElasticSearchService elasticService)
        {
            _context = context;
            _elasticService = elasticService;
        }

        // Propriété pour stocker les compétences disponibles
        public List<AssignedSkillData> AssignedSkills { get; set; } = new List<AssignedSkillData>();
        public void OnGet()
        {
            PopulateAssignedSkillData();
        }

        private void PopulateAssignedSkillData()
        {
            var allSkills = _context.Skills;

            // Créer un AssignedSkillData pour chaque compétence
            AssignedSkills = allSkills.Select(skill => new AssignedSkillData
            {
                SkillId = skill.Id,
                Name = skill.Nom,
                Assigned = false, // Par défaut, non assignée pour la création
                Level = 3 // Valeur par défaut pour l'exemple
            }).ToList();
        }

        [BindProperty]
        public Consultant Consultant { get; set; } = default!;

        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync(string[] selectedSkills,Dictionary<string, string> SkillLevels)
        {
            if (!ModelState.IsValid)
            {
                PopulateAssignedSkillData(); // Recharger les compétences en cas d'erreur
                return Page();
            }

            // 1. OnPostAsync reçoit le tableau d'IDs des compétences sélectionnées
            if (selectedSkills != null)
            {
                Consultant.ConsultantSkills = new List<ConsultantSkill>();

                foreach (var skillIdString in selectedSkills)
                {
                    if (int.TryParse(skillIdString, out int skillId))
                    {
                        int niveau = 1; 
                        // 🛑 Récupération de la vraie valeur du Niveau
                        if (SkillLevels.TryGetValue(skillIdString, out string niveauString) && int.TryParse(niveauString, out int parsedNiveau))
                        {
                            niveau = parsedNiveau;
                        }

                        // 3. Créer l'objet de jointure
                        Consultant.ConsultantSkills.Add(new ConsultantSkill
                        {
                            ConsultantId = Consultant.Id, // Sera assigné par EF Core
                            SkillId = skillId,
                            Niveau = niveau,
                            DerniereUtilisation = DateTime.Now
                        });
                    }
                }
            }

            _context.Consultants.Add(Consultant);
            await _context.SaveChangesAsync();

            // 🛑 NOUVEAU CODE : Préparer et envoyer les données à Elastic

            var newConsultant = await _context.Consultants
                .Include(c => c.ConsultantSkills)
                    .ThenInclude(cs => cs.Skill)
                .FirstOrDefaultAsync(c => c.Id == Consultant.Id);
                
            if (newConsultant != null)
            {
                var searchDto = new SearchConsultantDto
                {
                    Id = newConsultant.Id,
                    NomComplet = $"{newConsultant.Prenom} {newConsultant.Nom}",
                    Titre = newConsultant.Titre,
                    Statut = newConsultant.Statut,
                    // Mapping des noms de compétences pour l'indexation
                    Competences = newConsultant.ConsultantSkills.Select(cs => cs.Skill.Nom).ToList()
                };

                // 2. Appel au service d'indexation
                await _elasticService.IndexConsultantAsync(searchDto);
            }

            return RedirectToPage("./Index");
        }
        
    }
}
