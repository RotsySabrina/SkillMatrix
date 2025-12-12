using Microsoft.EntityFrameworkCore;
using SkillMatrix.Data.EF;

var builder = WebApplication.CreateBuilder(args);

// configure connection string via configuration (we'll add appsettings later)
builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews(); // for MVC parts / API controllers

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// register any Data services (if you create interfaces)
//builder.Services.AddScoped<ISkillService, SkillMatrix.Data.Services.SkillService>(); // placeholder

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

app.Run();
