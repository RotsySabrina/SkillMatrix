using Microsoft.AspNetCore.Mvc;
using SkillMatrix.Data.Services;
using SkillMatrix.Core.DTOs;
using System.Threading.Tasks;
using SkillMatrix.Core.ViewModels;
using Microsoft.AspNetCore.Authorization;

[Authorize]
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

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View();
    }

    [Authorize]
    public async Task<IActionResult> Index()
    {
        var model = await _adoNetService.GetDashboardStatsAsync();
        return View(model);
    }

    public async Task<IActionResult> ConsultantsList(string searchQuery, int page = 1, int pageSize = 3)
    {
        var viewModel = new ConsultantListViewModel { CurrentPage = page, PageSize = pageSize };

        // 1. Mise à jour forcée des statuts périmés en SQL (Source de vérité)
        await _adoNetService.UpdateExpiredStatusesAsync();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            // 2. Recherche textuelle dans Elastic
            var elasticResults = await _elasticService.SearchConsultantsAsync(searchQuery);
            var ids = elasticResults.Select(e => e.Id).ToList();

            // 3. Récupération des statuts RÉELS depuis SQL pour ces IDs
            var realStatuses = await _adoNetService.GetRealtimeStatusesAsync(ids);

            viewModel.Consultants = elasticResults.Select(e => new ConsultantListingDto {
                Id = e.Id,
                NomComplet = e.NomComplet,
                Titre = e.Titre,
                // On affiche le statut SQL si dispo, sinon celui d'Elastic
                Statut = realStatuses.ContainsKey(e.Id) ? realStatuses[e.Id] : e.Statut,
                Competences = e.Competences 
            }).ToList();

            viewModel.TotalCount = elasticResults.Count;
            viewModel.TotalPages = 1;
            ViewData["CurrentSearch"] = searchQuery;

            // 4. Synchronisation asynchrone pour mettre à jour Elastic pour la prochaine fois
            _ = _elasticService.SyncAllConsultantsAsync(viewModel.Consultants.Select(c => new SearchConsultantDto {
                Id = c.Id, NomComplet = c.NomComplet, Titre = c.Titre, Statut = c.Statut,Competences = c.Competences
            }));
        }
        else
        {
            // Listing classique via SQL
            var result = await _adoNetService.GetConsultantsAsync(page, pageSize);
            viewModel.Consultants = result.Consultants;
            viewModel.TotalCount = result.TotalCount;
            viewModel.TotalPages = (int)Math.Ceiling((double)result.TotalCount / pageSize);
        }

        return View(viewModel);
    }

    public async Task<IActionResult> DownloadCv(int id)
    {
        var consultant = await _adoNetService.GetConsultantDetailsAsync(id);

        if (consultant == null)
        {
            return NotFound("Consultant introuvable.");
        }

        var pdfBytes = _pdfService.GenerateAnonymousCv(consultant);

        return File(pdfBytes, "application/pdf", $"CV_Ref_{consultant.Id}.pdf");
    }

    public async Task<IActionResult> Details(int id)
    {
        var consultant = await _adoNetService.GetConsultantDetailsAsync(id);

        if (consultant == null)
        {
            return NotFound();
        }

        return View(consultant);
    }

}