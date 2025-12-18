using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkillMatrix.Data.Services;

namespace SkillMatrix.Web.Areas_Admin_Pages_Consultants
{
    public class ImportModel : PageModel
    {
        private readonly CsvImportService _importService;

        public ImportModel(CsvImportService importService)
        {
            _importService = importService;
        }

        [BindProperty]
        public IFormFile Upload { get; set; }

        public string Message { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (Upload == null || Upload.Length == 0)
            {
                Message = "Veuillez sélectionner un fichier valide.";
                return Page();
            }

            try
            {
                using (var stream = Upload.OpenReadStream())
                {
                    int count = await _importService.ImportConsultantsAsync(stream);
                    Message = $"{count} consultants ont été importés avec succès !";
                }
            }
            catch (Exception ex)
            {
                Message = $"Erreur lors de l'import : {ex.Message}";
            }

            return Page();
        }
    }
}