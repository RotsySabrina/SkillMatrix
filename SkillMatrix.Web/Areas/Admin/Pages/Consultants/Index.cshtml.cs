using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SkillMatrix.Core.Models;
using SkillMatrix.Data.EF;

namespace SkillMatrix.Web.Areas_Admin_Pages_Consultants
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly SkillMatrix.Data.EF.ApplicationDbContext _context;

        public IndexModel(SkillMatrix.Data.EF.ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Consultant> Consultant { get;set; } = default!;

        public async Task OnGetAsync()
        {
            //Consultant = await _context.Consultants.ToListAsync();
            if (_context.Consultants != null)
            {
                Consultant = await _context.Consultants
                    .Include(c => c.ConsultantSkills) // Inclut la table de jointure
                    .ThenInclude(cs => cs.Skill)       // Inclut l'objet Skill réel
                    .ToListAsync();
            }
        }
    }
}
