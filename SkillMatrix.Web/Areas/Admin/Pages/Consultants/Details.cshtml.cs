using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SkillMatrix.Core.Models;
using SkillMatrix.Data.EF;

namespace SkillMatrix.Web.Areas_Admin_Pages_Consultants
{
    public class DetailsModel : PageModel
    {
        private readonly SkillMatrix.Data.EF.ApplicationDbContext _context;

        public DetailsModel(SkillMatrix.Data.EF.ApplicationDbContext context)
        {
            _context = context;
        }

        public Consultant Consultant { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var consultant = await _context.Consultants
                .Include(c => c.Missions)
                    .ThenInclude(m => m.Client)
                .Include(c => c.ConsultantSkills)
                    .ThenInclude(cs => cs.Skill)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (consultant == null) return NotFound();
            
            // Trie les missions de la plus récente à la plus ancienne
            consultant.Missions = consultant.Missions.OrderByDescending(m => m.DateDebut).ToList();
            Consultant = consultant;

            return Page();
        }
    }
}
