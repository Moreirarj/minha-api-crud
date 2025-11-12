using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MinhaApiCrud.Hubs;
using MinhaApiCrud.Models;

namespace MinhaApiCrud.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UsersController> _logger;
        private readonly IHubContext<UserHub> _hubContext;

        public UsersController(AppDbContext context, ILogger<UsersController> logger, IHubContext<UserHub> hubContext)
        {
            _context = context;
            _logger = logger;
            _hubContext = hubContext;
        }

        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserResponse>>> GetUsers()
        {
            _logger.LogInformation("📋 Buscando todos os usuários");
            var users = await _context.Users.Where(u => u.IsActive).ToListAsync();
            return users.Select(UserResponse.FromUser).ToList();
        }

        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserResponse>> GetUser(int id)
        {
            _logger.LogInformation($"🔍 Buscando usuário com ID: {id}");
            
            var user = await _context.Users.FindAsync(id);

            if (user == null || !user.IsActive)
            {
                _logger.LogWarning($"❌ Usuário com ID {id} não encontrado");
                return NotFound();
            }

            _logger.LogInformation($"✅ Usuário encontrado: {user.Name}");
            return UserResponse.FromUser(user);
        }

        // POST: api/Users
        [HttpPost]
        public async Task<ActionResult<UserResponse>> CreateUser(CreateUserRequest createRequest)
        {
            _logger.LogInformation("🆕 Criando novo usuário");
            
            var user = new User
            {
                Name = createRequest.Name,
                Email = createRequest.Email,
                Age = createRequest.Age,
                Phone = createRequest.Phone,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            if (!user.IsValid())
            {
                _logger.LogWarning("❌ Dados do usuário inválidos");
                return BadRequest("Dados do usuário inválidos");
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Notificar via SignalR
            await _hubContext.Clients.All.SendAsync("UserCreated", UserResponse.FromUser(user));

            _logger.LogInformation($"✅ Usuário criado com ID: {user.Id}");
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, UserResponse.FromUser(user));
        }

        // PUT: api/Users/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, UpdateUserRequest updateRequest)
        {
            _logger.LogInformation($"✏️ Atualizando usuário com ID: {id}");

            var user = await _context.Users.FindAsync(id);
            if (user == null || !user.IsActive)
            {
                _logger.LogWarning($"❌ Usuário com ID {id} não encontrado para atualização");
                return NotFound();
            }

            // Atualiza apenas as propriedades que foram fornecidas
            if (!string.IsNullOrEmpty(updateRequest.Name))
                user.Name = updateRequest.Name;
            
            if (!string.IsNullOrEmpty(updateRequest.Email))
                user.Email = updateRequest.Email;
            
            if (updateRequest.Age.HasValue)
                user.Age = updateRequest.Age.Value;
            
            if (updateRequest.Phone != null)
                user.Phone = updateRequest.Phone;

            user.UpdateTimestamp();

            if (!user.IsValid())
            {
                _logger.LogWarning("❌ Dados atualizados do usuário são inválidos");
                return BadRequest("Dados do usuário inválidos");
            }

            _logger.LogInformation("🔄 Salvando alterações...");
            await _context.SaveChangesAsync();

            // Notificar via SignalR
            await _hubContext.Clients.All.SendAsync("UserUpdated", UserResponse.FromUser(user));

            _logger.LogInformation("✅ UPDATE CONCLUÍDO com sucesso!");
            return NoContent();
        }

        // DELETE: api/Users/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            _logger.LogInformation($"🗑️ Excluindo usuário com ID: {id}");
            
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                _logger.LogWarning($"❌ Usuário com ID {id} não encontrado para exclusão");
                return NotFound();
            }

            // Soft delete (apenas desativa)
            user.Deactivate();
            await _context.SaveChangesAsync();

            // Notificar via SignalR
            await _hubContext.Clients.All.SendAsync("UserDeleted", id);

            _logger.LogInformation($"✅ Usuário com ID {id} excluído com sucesso");
            return NoContent();
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id && e.IsActive);
        }
    }
}