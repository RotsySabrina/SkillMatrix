using Microsoft.EntityFrameworkCore;
using SkillMatrix.Data.EF;
using SkillMatrix.Data.Services;
using SkillMatrix.Core.Models;
using QuestPDF.Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// 🛑 ACTIVATION DE LA LICENCE GRATUITE
QuestPDF.Settings.License = LicenseType.Community;

// configure connection string via configuration (we'll add appsettings later)
builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews(); // for MVC parts / API controllers

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<AuthService>();
//front MVC
builder.Services.AddScoped<AdoNetService>();
//--end
// 1. Ajoutez votre CsvImportService ici
builder.Services.AddScoped<CsvImportService>();

builder.Services.AddScoped<CvPdfService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; // Où aller si on n'est pas connecté
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8); // Durée de la session
    });

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

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var auth = scope.ServiceProvider.GetRequiredService<AuthService>();

    if (!context.Users.Any())
    {
        // 1. ADMIN
        var admin = new User { Email="admin@skillmatrix.com", NomComplet="Jean Admin", Role="Admin" };
        admin.PasswordHash = auth.HashPassword(admin, "Admin123");
        
        // 2. RECRUTEUR (Simple utilisateur)
        var user = new User { Email="recrut@skillmatrix.com", NomComplet="Marie Recruteur", Role="User" };
        user.PasswordHash = auth.HashPassword(user, "User123");

        context.Users.AddRange(admin, user);
    }
    // 2. SEED DES CLIENTS 
    if (!context.Clients.Any())
    {
        context.Clients.AddRange(
            new Client { Nom = "BNP Paribas", SecteurActivite = "Banque" },
            new Client { Nom = "Orange", SecteurActivite = "Télécom" },
            new Client { Nom = "TotalEnergies", SecteurActivite = "Énergie" },
            new Client { Nom = "LVMH", SecteurActivite = "Luxe" }
        );
    }
    context.SaveChanges();
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); 
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}



app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();

//ROUTAGE MVC
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.MapRazorPages();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var elasticService = services.GetService<ElasticSearchService>();
    var context = services.GetRequiredService<ApplicationDbContext>();

    if (elasticService != null)
    {
        Console.WriteLine("Démarrage de la synchronisation initiale Elasticsearch...");

        // 1. Récupérer TOUS les consultants avec leurs compétences (Indispensable !)
        var consultants = await context.Consultants
            .AsNoTracking()
            .Include(c => c.ConsultantSkills)
                .ThenInclude(cs => cs.Skill)
            .ToListAsync();

        // 2. Transformer en DTOs
        var dtos = consultants.Select(c => new SkillMatrix.Core.DTOs.SearchConsultantDto
        {
            Id = c.Id,
            NomComplet = $"{c.Prenom} {c.Nom}",
            Titre = c.Titre,
            Statut = c.Statut,
            Competences = c.ConsultantSkills
                .Where(cs => cs.Skill != null)
                .Select(cs => cs.Skill.Nom)
                .ToList()
        }).ToList();

        // 3. Envoyer à Elasticsearch (Utilisez votre méthode ReindexAllAsync qui fait le Delete/Create/Bulk)
        await elasticService.ReindexAllAsync(dtos);

        Console.WriteLine($"Indexation initiale terminée : {dtos.Count} consultants synchronisés.");
    }
}

app.Run();
