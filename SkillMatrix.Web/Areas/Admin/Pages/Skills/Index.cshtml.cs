using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SkillMatrix.Core.Models;
using SkillMatrix.Data.EF;

namespace SkillMatrix.Web.Areas_Admin_Pages_Skills
{
    public class IndexModel : PageModel
    {
        private readonly SkillMatrix.Data.EF.ApplicationDbContext _context;

        public IndexModel(SkillMatrix.Data.EF.ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Skill> Skill { get;set; } = default!;

        public async Task OnGetAsync()
        {
            Skill = await _context.Skills.ToListAsync();
        }
    }
}
