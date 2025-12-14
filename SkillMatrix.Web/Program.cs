using Microsoft.EntityFrameworkCore;
using SkillMatrix.Data.EF;
using SkillMatrix.Data.Services;

var builder = WebApplication.CreateBuilder(args);

// configure connection string via configuration (we'll add appsettings later)
builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews(); // for MVC parts / API controllers

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// register any Data services (if you create interfaces)
//builder.Services.AddScoped<ISkillService, SkillMatrix.Data.Services.SkillService>(); // placeholder

var elasticUrl = builder.Configuration.GetSection("ElasticSearch:Url").Value;

if (!string.IsNullOrEmpty(elasticUrl))
{
    // 1. Enregistrement en tant que Singleton : une seule instance de ce service est créée
    builder.Services.AddSingleton(new ElasticSearchService(elasticUrl));
}
else
{
    // Gérer le cas où l'URL n'est pas définie
    Console.WriteLine("Attention : L'URL d'Elasticsearch n'est pas configurée.");
}

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}


app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var elasticService = scope.ServiceProvider.GetService<ElasticSearchService>();
    if (elasticService != null)
    {
        Console.WriteLine("Tentative de création de l'index Elasticsearch...");
        // Appel synchrone au démarrage (pour ne pas bloquer le thread principal trop longtemps)
        elasticService.CreateIndexAsync().Wait(); 
        Console.WriteLine("Indexation initiale terminée.");
    }
}

app.Run();
