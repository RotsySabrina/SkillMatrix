using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SkillMatrix.Core.DTOs;
using SkillMatrix.Core.Models;
using SkillMatrix.Data.EF;
using SkillMatrix.Data.Services;

namespace SkillMatrix.Web.Areas_Admin_Pages_Consultants
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ElasticSearchService _elasticService;

        public IndexModel(ApplicationDbContext context, ElasticSearchService elasticService)
        {
            _context = context;
            _elasticService = elasticService;
        }

        public IList<Consultant> Consultant { get;set; } = default!;

        public async Task OnGetAsync()
        {
            var consultantsEnMission = await _context.Consultants
                .Include(c => c.Missions)
                .Where(c => c.Statut == "En Mission")
                .ToListAsync();

            bool hasChanges = false;
            var today = DateTime.Today;

            foreach (var c in consultantsEnMission)
            {
                // On vérifie s'il existe AU MOINS une mission encore active aujourd'hui
                bool aToujoursUneMissionActive = c.Missions.Any(m => 
                    m.DateDebut <= today && 
                    (m.DateFin == null || m.DateFin >= today)
                );

                if (!aToujoursUneMissionActive)
                {
                    c.Statut = "Intercontrat";
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                await _context.SaveChangesAsync();
            }

            // --- 2. CHARGEMENT NORMAL POUR L'AFFICHAGE ---
            if (_context.Consultants != null)
            {
                Consultant = await _context.Consultants
                    .Include(c => c.ConsultantSkills)
                    .ThenInclude(cs => cs.Skill)
                    // On inclut aussi les missions si vous voulez afficher le nom du client actuel
                    .Include(c => c.Missions)
                    .ThenInclude(m => m.Client)
                    .ToListAsync();
            }
        }

        // Ajoutez cette méthode à votre classe IndexModel
        public async Task<IActionResult> OnPostSyncAllToElasticAsync()
        {
            //Récupérer TOUS les consultants SQL avec leurs compétences
            var consultants = await _context.Consultants
                .Include(c => c.ConsultantSkills)
                    .ThenInclude(cs => cs.Skill)
                .ToListAsync();

            //Transformer les entités SQL en DTOs pour Elasticsearch
            var dtos = consultants.Select(c => new SearchConsultantDto
            {
                Id = c.Id,
                NomComplet = $"{c.Prenom} {c.Nom}",
                Titre = c.Titre,
                Statut = c.Statut,
                Competences = c.ConsultantSkills.Select(cs => cs.Skill.Nom).ToList()
            }).ToList();
            foreach (var dto in dtos)
            {
                Console.WriteLine($"Consultant: {dto.NomComplet}, Skills: {dto.Competences.Count}");
            }

            await _elasticService.ReindexAllAsync(dtos);

            TempData["SuccessMessage"] = $"{dtos.Count} consultants ont été synchronisés avec succès !";
            return RedirectToPage();
        }
    }
}
