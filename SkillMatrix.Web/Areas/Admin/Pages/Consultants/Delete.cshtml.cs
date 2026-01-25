using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SkillMatrix.Core.Models;
using SkillMatrix.Data.EF;
using SkillMatrix.Data.Services;

namespace SkillMatrix.Web.Areas_Admin_Pages_Consultants
{
    public class DeleteModel : PageModel
    {
        private readonly SkillMatrix.Data.EF.ApplicationDbContext _context;
        private readonly ElasticSearchService _elasticService;

        public DeleteModel(SkillMatrix.Data.EF.ApplicationDbContext context, ElasticSearchService elasticService)
        {
            _context = context;
            _elasticService = elasticService;
        }

        [BindProperty]
        public Consultant Consultant { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var consultant = await _context.Consultants.FirstOrDefaultAsync(m => m.Id == id);

            if (consultant is not null)
            {
                Consultant = consultant;

                return Page();
            }

            return NotFound();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var consultant = await _context.Consultants.FindAsync(id);
            if (consultant != null)
            {
                Consultant = consultant;
                _context.Consultants.Remove(Consultant);
                await _context.SaveChangesAsync();
                try 
                {
                    await _elasticService.DeleteConsultantAsync(id.Value);
                }
                catch (Exception ex)
                {
                    // Optionnel : logger l'erreur si Elastic est indisponible
                    // mais ne pas bloquer la redirection car SQL est déjà supprimé
                }
            }

            return RedirectToPage("./Index");
        }
    }
}
