using Microsoft.AspNetCore.Mvc;
using SkillMatrix.Data.Services;
using SkillMatrix.Core.DTOs;
using System.Threading.Tasks;
using SkillMatrix.Core.ViewModels;
public class HomeController : Controller
{
    private readonly AdoNetService _adoNetService;
    private readonly ElasticSearchService _elasticService; // 🛑 1. Ajouter le champ
    private readonly CvPdfService _pdfService;
    public HomeController(AdoNetService adoNetService, ElasticSearchService elasticService, CvPdfService pdfService)
    {
        _adoNetService = adoNetService;
        _elasticService = elasticService;
        _pdfService = pdfService;
    }

    // 🛑 3. Modifier la méthode Index pour accepter 'searchQuery'
    public async Task<IActionResult> Index(string searchQuery, int page = 1, int pageSize = 3)
    {
        var viewModel = new ConsultantListViewModel
        {
            CurrentPage = page,
            PageSize = pageSize
        };

        // SCÉNARIO A : RECHERCHE ACTIVE (Via Elasticsearch)
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var elasticResults = await _elasticService.SearchConsultantsAsync(searchQuery);

            // Mapping manuel : Convertir SearchConsultantDto (Elastic) vers ConsultantListingDto (Affichage)
            viewModel.Consultants = elasticResults.Select(e => new ConsultantListingDto
            {
                Id = e.Id,
                NomComplet = e.NomComplet,
                Titre = e.Titre,
                Statut = e.Statut,
                //DescriptionCourte = e.DescriptionProfil.Length > 50 ? e.DescriptionProfil.Substring(0, 50) + "..." : e.DescriptionProfil,
                // On peut ajouter les compétences dans la description pour l'affichage recherche
                //DescriptionCourte = $"Skills: {string.Join(", ", e.Competences)}" 
            }).ToList();

            viewModel.TotalCount = elasticResults.Count;
            viewModel.TotalPages = 1; // Pas de pagination complexe pour la recherche pour l'instant
            
            // On renvoie le terme de recherche à la vue pour l'afficher dans la barre
            ViewData["CurrentSearch"] = searchQuery; 
        }
        // SCÉNARIO B : LISTING CLASSIQUE (Via ADO.NET)
        else
        {
            var result = await _adoNetService.GetConsultantsAsync(page, pageSize);
            viewModel.Consultants = result.Consultants;
            viewModel.TotalCount = result.TotalCount;
            viewModel.TotalPages = (int)Math.Ceiling((double)result.TotalCount / pageSize);
        }

        return View(viewModel);
    }

    public async Task<IActionResult> DownloadCv(int id)
    {
        // 1. Récupération via ADO.NET au lieu de EF Core
        var consultant = await _adoNetService.GetConsultantDetailsAsync(id);

        if (consultant == null)
        {
            return NotFound("Consultant introuvable.");
        }

        // 2. Génération du PDF (Le service PDF reste identique)
        var pdfBytes = _pdfService.GenerateAnonymousCv(consultant);

        // 3. Retour du fichier anonymisé
        return File(pdfBytes, "application/pdf", $"CV_Ref_{consultant.Id}.pdf");
    }
    //Avec EF Core
   /* public async Task<IActionResult> DownloadCv(int id)
    {
        // 1. Récupération complète (Eager Loading)
        var consultant = await _context.Consultants
            .Include(c => c.ConsultantSkills)
            .ThenInclude(cs => cs.Skill)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (consultant == null)
        {
            return NotFound();
        }

        // 2. Génération du PDF
        var pdfBytes = _pdfService.GenerateAnonymousCv(consultant);

        // 3. Retour du fichier
        // Le nom du fichier est aussi anonymisé : "CV_Ref_12.pdf"
        return File(pdfBytes, "application/pdf", $"CV_Ref_{consultant.Id}.pdf");
    }*/

}