using DesafioHyperativa.API.Extensions;
using DesafioHyperativa.API.Middleware;
using DesafioHyperativa.Domain.Entities;
using DesafioHyperativa.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddApplicationServices();

var app = builder.Build();

// Aplicar migrations automaticamente ao iniciar
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    // Seed: Criar usuário admin padrão se não existir
    var userRepo = scope.ServiceProvider.GetRequiredService<DesafioHyperativa.Domain.Interfaces.IUserRepository>();
    var adminUser = await userRepo.GetByUsernameAsync("admin");
    if (adminUser is null)
    {
        var admin = new User(
            username: "admin",
            email: "admin@hyperativa.com.br",
            passwordHash: BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            roles: ["Admin", "User"]
        );
        await userRepo.AddAsync(admin);
        Log.Information("Usuário admin criado com sucesso.");
    }
}

// Middleware pipeline
app.UseSerilogRequestLogging();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Docker"))
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Desafio Hyperativa v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.UseCors("DefaultPolicy");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

await app.RunAsync();

// Necessário para testes de integração
public partial class Program { }
