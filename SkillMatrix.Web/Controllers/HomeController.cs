using Microsoft.AspNetCore.Mvc;
using SkillMatrix.Data.Services;
using SkillMatrix.Core.DTOs;
using System.Threading.Tasks;
using SkillMatrix.Core.ViewModels;
public class HomeController : Controller
{
    private readonly AdoNetService _adoNetService;
    private readonly ElasticSearchService _elasticService; // 🛑 1. Ajouter le champ

    public HomeController(AdoNetService adoNetService, ElasticSearchService elasticService)
    {
        _adoNetService = adoNetService;
        _elasticService = elasticService;
    }

    /*public async Task<IActionResult> Index(int page = 1, int pageSize = 3) // Prend les paramètres de l'URL
    {
        var result = await _adoNetService.GetConsultantsAsync(page, pageSize);
        
        // Créer un ViewModel simple pour passer les données et les métadonnées de pagination
        var viewModel = new ConsultantListViewModel
        {
            Consultants = result.Consultants,
            CurrentPage = page,
            PageSize = pageSize,
            TotalCount = result.TotalCount,
            TotalPages = (int)Math.Ceiling((double)result.TotalCount / pageSize)
        };
        
        return View(viewModel);
    }*/

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

}