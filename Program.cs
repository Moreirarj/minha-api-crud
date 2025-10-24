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

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
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
        Console.WriteLine(created ? "✅ Database created successfully!" : "✅ Database already exists!");
        
        // Test connection
        bool canConnect = dbContext.Database.CanConnect();
        Console.WriteLine(canConnect ? "✅ Can connect to database!" : "❌ Cannot connect to database!");
        
        // List all tables (for debugging)
        var usersCount = dbContext.Users.Count();
        Console.WriteLine($"📊 Total users in database: {usersCount}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Database error: {ex.Message}");
    }
}

app.Run();