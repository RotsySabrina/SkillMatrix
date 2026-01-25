using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SkillMatrix.Core.DTOs;
using SkillMatrix.Core.Models;
using SkillMatrix.Core.ViewModels;
using SkillMatrix.Data.EF;
using SkillMatrix.Data.Services;

namespace SkillMatrix.Web.Areas_Admin_Pages_Consultants
{
    public class EditModel : PageModel
    {
        private readonly SkillMatrix.Data.EF.ApplicationDbContext _context;
        private readonly ElasticSearchService _elasticService;
        public EditModel(SkillMatrix.Data.EF.ApplicationDbContext context, ElasticSearchService elasticService)
        {
            _context = context;
            _elasticService = elasticService;
        }

        [BindProperty]
        public Consultant Consultant { get; set; } = default!;
        public ConsultantEditViewModel ViewModel {get; set;} = new();
        public SelectList ClientOptions{get; set;} = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var consultant =  await _context.Consultants
                .Include(c => c.Missions)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (consultant == null) return NotFound();

            ViewModel.Consultant = consultant;
            ViewModel.ClientsList = await _context.Clients.OrderBy(c =>c.Nom).ToListAsync();

            Consultant = consultant;
            ClientOptions = new SelectList(ViewModel.ClientsList, "Id", "Nom");

            return Page();
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }
            
            UpdateConsultantStatus(Consultant);
            _context.Attach(Consultant).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                var updatedConsultant = await _context.Consultants
                    .Include(c => c.ConsultantSkills)
                        .ThenInclude(cs => cs.Skill)
                    .FirstOrDefaultAsync(c => c.Id == Consultant.Id);

                if (updatedConsultant != null)
                {
                    var searchDto = new SearchConsultantDto
                    {
                        Id = updatedConsultant.Id,
                        NomComplet = $"{updatedConsultant.Prenom} {updatedConsultant.Nom}",
                        Titre = updatedConsultant.Titre,
                        Statut = updatedConsultant.Statut,
                        Competences = updatedConsultant.ConsultantSkills
                                        .Select(cs => cs.Skill.Nom).ToList()
                    };

                    await _elasticService.IndexConsultantAsync(searchDto);
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ConsultantExists(Consultant.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }

        private bool ConsultantExists(int id)
        {
            return _context.Consultants.Any(e => e.Id == id);
        }

        private void UpdateConsultantStatus(Consultant consultant)
        {
            var today = DateTime.Today;
            
            bool isOnMission = consultant.Missions.Any(m => 
                m.DateDebut <= today &&             
                (m.DateFin == null || m.DateFin >= today)
            );

            if (isOnMission)
            {
                consultant.Statut = "En Mission";
            }
            else
            {
                bool aDejaTravaille = consultant.Missions.Any(m => m.DateFin < today);
                consultant.Statut = aDejaTravaille ? "Intercontrat" : "Disponible";
            }
        }
    }
}
