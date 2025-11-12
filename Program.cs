using Microsoft.EntityFrameworkCore;
using MinhaApiCrud;
using MinhaApiCrud.Hubs;
using MinhaApiCrud.Models;

var builder = WebApplication.CreateBuilder(args);

// ======================
// SERVICES
// ======================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Banco de Dados SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=minhaapi.db"));

// Health Check e CORS
builder.Services.AddHealthChecks();
builder.Services.AddCors(options =>
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()));

// 👇 Adiciona o SignalR
builder.Services.AddSignalR();

// ======================
// APP
// ======================
var app = builder.Build();

// Swagger em ambiente de dev
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.MapHealthChecks("/health");
app.UseAuthorization();
app.MapControllers();

// 👇 Mapeia o Hub do SignalR
app.MapHub<UserHub>("/hubs/users");

// ======================
// BANCO DE DADOS
// ======================
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    try
    {
        bool created = dbContext.Database.EnsureCreated();
        Console.WriteLine(created ? "Database created successfully!" : "Database already exists!");

        bool canConnect = dbContext.Database.CanConnect();
        Console.WriteLine(canConnect ? "Can connect to database!" : "Cannot connect to database!");

        var usersCount = dbContext.Users.Count();
        Console.WriteLine($"Total users in database: {usersCount}");

        // Seed inicial
        if (created || usersCount == 0)
        {
            Console.WriteLine("Seeding initial data...");
            if (!dbContext.Users.Any())
            {
                dbContext.Users.AddRange(
                    new User { Name = "João Silva", Email = "joao.silva@email.com", Age = 38 },
                    new User { Name = "Maria Santos", Email = "maria.santos@email.com", Age = 28 },
                    new User { Name = "Pedro Oliveira", Email = "pedro.oliveira@email.com", Age = 32 }
                );
                await dbContext.SaveChangesAsync();
                Console.WriteLine($"Added {dbContext.Users.Count()} sample users!");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database error: {ex.Message}");
    }
}

// ======================
// ENDPOINT RAIZ
// ======================
app.MapGet("/", () =>
{
    var startupTime = DateTime.UtcNow;
    return new
    {
        Message = "Minha API CRUD está rodando!",
        Version = "v1.0",
        Environment = app.Environment.EnvironmentName,
        StartupTime = startupTime,
        Uptime = DateTime.UtcNow - startupTime,
        Endpoints = new[]
        {
            "GET /swagger - Documentação interativa",
            "GET /health - Status da API",
            "GET /api/users - Listar usuários",
            "GET /api/users/{id} - Buscar usuário",
            "POST /api/users - Criar usuário",
            "PUT /api/users/{id} - Atualizar usuário",
            "DELETE /api/users/{id} - Deletar usuário",
            "SignalR Hub: /hubs/users"
        }
    };
});

app.Run();