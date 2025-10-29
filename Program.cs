using Microsoft.EntityFrameworkCore;
using MinhaApiCrud;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Entity Framework with SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=minhaapi.db"));

// NOVAS ADIÇÕES (CORRIGIDAS)
builder.Services.AddHealthChecks(); // Health Check básico sem Entity Framework
builder.Services.AddCors(options => 
    options.AddPolicy("AllowAll", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// NOVOS MIDDLEWARES
app.UseCors("AllowAll");
app.MapHealthChecks("/health"); // Health Check básico

app.UseAuthorization();
app.MapControllers();

// Simple database initialization - NO MIGRATIONS NEEDED
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    try
    {
        // This creates the database and tables without migrations
        bool created = dbContext.Database.EnsureCreated();
        Console.WriteLine(created ? "Database created successfully!" : "Database already exists!");
        
        // Test connection
        bool canConnect = dbContext.Database.CanConnect();
        Console.WriteLine(canConnect ? "Can connect to database!" : "Cannot connect to database!");
        
        // List all tables (for debugging)
        var usersCount = dbContext.Users.Count();
        Console.WriteLine($"Total users in database: {usersCount}");
        
        // Seed de dados iniciais (OPCIONAL)
        if (created || usersCount == 0)
        {
            Console.WriteLine("Seeding initial data...");
            
            // Adiciona alguns usuarios de exemplo se a tabela estiver vazia
            if (!dbContext.Users.Any())
            {
                dbContext.Users.AddRange(
                    new User { Name = "João Silva", Email = "joao@email.com", Age = 30 },
                    new User { Name = "Maria Santos", Email = "maria@email.com", Age = 25 },
                    new User { Name = "Pedro Oliveira", Email = "pedro@email.com", Age = 35 }
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

// NOVO ENDPOINT RAIZ (OPCIONAL)
app.MapGet("/", () => {
    var startupTime = DateTime.UtcNow;
    return new {
        Message = "Minha API CRUD está rodando!",
        Version = "v1.0",
        Environment = app.Environment.EnvironmentName,
        StartupTime = startupTime,
        Uptime = DateTime.UtcNow - startupTime,
        Endpoints = new[] {
            "GET /swagger - Documentação interativa",
            "GET /health - Status da API", 
            "GET /api/users - Listar usuarios",
            "GET /api/users/{id} - Buscar usuario",
            "POST /api/users - Criar usuario",
            "PUT /api/users/{id} - Atualizar usuario",
            "DELETE /api/users/{id} - Deletar usuario"
        }
    };
});

app.Run();