using Microsoft.EntityFrameworkCore;
using TheRealBank.Repositories;
using TheRealBank.Services;
using TheRealBank.Contexts;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

TheRealBank.Repositories.ExtensionMethods.AddDesignerRepositories(builder.Services, builder.Configuration);

builder.Services.AddApplicationServices();

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizePage("/Experiencia/Layout");

    options.Conventions.AuthorizeFolder("/Customers", "AdminOnly");

    options.Conventions.AllowAnonymousToPage("/Customers/AddCliente");
});

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Autentifica/Auth";
        o.AccessDeniedPath = "/Autentifica/AuthAdm";
        o.Cookie.Name = "TheRealBank.Auth";
    });

builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MainContext>();
    db.Database.Migrate();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.Run();
