using Microsoft.AspNetCore.Mvc;
using SkillMatrix.Data.Services;
using SkillMatrix.Core.DTOs;
using SkillMatrix.Core.ViewModels;
using Microsoft.AspNetCore.Authorization;

[Authorize]
public class HomeController : Controller
{
    private readonly AdoNetService _adoNetService;
    private readonly ElasticSearchService _elasticService;
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
    public IActionResult Index()
    {
        if (User.IsInRole("Admin"))
        {
            return RedirectToPage("/Dashboard/Index", new { area = "Admin" });
        }

        return RedirectToAction("UserHome");
    }

    [Authorize]
    public IActionResult UserHome()
    {
        if (User.IsInRole("Admin"))
        {
            return RedirectToAction("Index");
        }

        ViewBag.UserName = User.Identity?.Name ?? "Utilisateur";
        return View();
    }

    public async Task<IActionResult> ConsultantsList(string searchQuery, int page = 1, int pageSize = 3)
    {
        var viewModel = new ConsultantListViewModel
        {
            CurrentPage = page,
            PageSize = pageSize
        };

        await _adoNetService.UpdateExpiredStatusesAsync();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var elasticResults = await _elasticService.SearchConsultantsAsync(searchQuery);
            var ids = elasticResults.Select(e => e.Id).ToList();

            var realStatuses = await _adoNetService.GetRealtimeStatusesAsync(ids);

            viewModel.Consultants = elasticResults.Select(e => new ConsultantListingDto
            {
                Id = e.Id,
                NomComplet = e.NomComplet,
                Titre = e.Titre,
                Statut = realStatuses.ContainsKey(e.Id) ? realStatuses[e.Id] : e.Statut,
                Competences = e.Competences
            }).ToList();

            viewModel.TotalCount = elasticResults.Count;
            viewModel.TotalPages = 1;
            ViewData["CurrentSearch"] = searchQuery;

            _ = _elasticService.SyncAllConsultantsAsync(
                viewModel.Consultants.Select(c => new SearchConsultantDto
                {
                    Id = c.Id,
                    NomComplet = c.NomComplet,
                    Titre = c.Titre,
                    Statut = c.Statut,
                    Competences = c.Competences
                })
            );
        }
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

    public async Task<IActionResult> Timeline()
    {
        var model = await _adoNetService.GetTimelineDataAsync(6);
        return View(model);
    }
}