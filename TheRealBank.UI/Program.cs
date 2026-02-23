using System.Text.Json;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using OllamaSharp;
using TheRealBank.Contexts;
using TheRealBank.Repositories;
using TheRealBank.Services;
using TheRealBank.Services.Chat;

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

builder.Services.AddSingleton<IChatClient>(_ =>
    new OllamaApiClient("http://localhost:11434", "phi3:mini"));

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

app.MapPost("/api/chat", async (HttpContext context, OllamaChatService chatService) =>
{
    var request = await context.Request.ReadFromJsonAsync<ChatRequest>();
    if (request is null || string.IsNullOrWhiteSpace(request.Message))
    {
        context.Response.StatusCode = 400;
        return;
    }

    var history = new List<ChatMessage>();

    if (request.History is not null)
    {
        foreach (var msg in request.History)
        {
            var role = msg.Role?.ToLowerInvariant() switch
            {
                "user" => ChatRole.User,
                "assistant" => ChatRole.Assistant,
                _ => ChatRole.User
            };
            history.Add(new ChatMessage(role, msg.Content ?? string.Empty));
        }
    }

    history.Add(new ChatMessage(ChatRole.User, request.Message));

    context.Response.ContentType = "text/event-stream";
    context.Response.Headers.CacheControl = "no-cache";
    context.Response.Headers.Connection = "keep-alive";

    await foreach (var chunk in chatService.StreamResponseAsync(history, context.RequestAborted))
    {
        var data = JsonSerializer.Serialize(new { text = chunk });
        await context.Response.WriteAsync($"data: {data}\n\n", context.RequestAborted);
        await context.Response.Body.FlushAsync(context.RequestAborted);
    }

    await context.Response.WriteAsync("data: [DONE]\n\n", context.RequestAborted);
    await context.Response.Body.FlushAsync(context.RequestAborted);
});

app.Run();

public record ChatRequest(string Message, List<ChatMessageDto>? History);
public record ChatMessageDto(string? Role, string? Content);