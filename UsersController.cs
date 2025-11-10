using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using MinhaApiCrud;
using MinhaApiCrud.Hubs;

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

        // GET: api/users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers(
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                _logger.LogInformation("Buscando usuarios - Search: {Search}, Page: {Page}", search, page);

                var query = _context.Users.AsQueryable();

                // Filtro de busca
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(u =>
                        (u.Name != null && u.Name.Contains(search)) ||
                        (u.Email != null && u.Email.Contains(search))
                    );
                }

                // Paginação
                var totalCount = await query.CountAsync();
                var users = await query
                    .OrderBy(u => u.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var response = new
                {
                    Success = true,
                    Data = users,
                    Pagination = new
                    {
                        Page = page,
                        PageSize = pageSize,
                        TotalCount = totalCount,
                        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                        HasPrevious = page > 1,
                        HasNext = page < (int)Math.Ceiling(totalCount / (double)pageSize)
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar usuarios");
                return StatusCode(500, new { Success = false, Message = "Erro interno do servidor", Error = ex.Message });
            }
        }

        // GET: api/users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            try
            {
                _logger.LogInformation("Buscando usuario com ID: {UserId}", id);

                var user = await _context.Users.FindAsync(id);

                if (user == null)
                {
                    _logger.LogWarning("Usuario com ID {UserId} nao encontrado", id);
                    return NotFound(new { Success = false, Message = $"Usuario com ID {id} nao encontrado" });
                }

                return Ok(new { Success = true, Data = user });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar usuario com ID: {UserId}", id);
                return StatusCode(500, new { Success = false, Message = "Erro interno do servidor", Error = ex.Message });
            }
        }

        // POST: api/users
        [HttpPost]
        public async Task<ActionResult<User>> CreateUser(User user)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Tentativa de criar usuario com dados invalidos");
                    return BadRequest(new { Success = false, Message = "Dados invalidos", Errors = ModelState.Values.SelectMany(v => v.Errors) });
                }

                // Validação de email único
                if (await _context.Users.AnyAsync(u => u.Email == user.Email))
                {
                    _logger.LogWarning("Tentativa de criar usuario com email duplicado: {Email}", user.Email);
                    return Conflict(new { Success = false, Message = "Já existe um usuario com este email" });
                }

                user.CreatedAt = DateTime.UtcNow;
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Usuario criado com ID: {UserId}", user.Id);

                // 🚀 Envia notificação em tempo real
                await _hubContext.Clients.All.SendAsync("UserAdded", user);

                return CreatedAtAction(nameof(GetUser), new { id = user.Id },
                    new { Success = true, Message = "Usuario criado com sucesso", Data = user });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar usuario");
                return StatusCode(500, new { Success = false, Message = "Erro ao criar usuario", Error = ex.Message });
            }
        }

        // PUT: api/users/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, User user)
        {
            try
            {
                if (id != user.Id)
                {
                    _logger.LogWarning("Tentativa de atualizar usuario com ID inconsistente: {UrlId} vs {BodyId}", id, user.Id);
                    return BadRequest(new { Success = false, Message = "ID do usuario nao corresponde" });
                }

                var existingUser = await _context.Users.FindAsync(id);
                if (existingUser == null)
                {
                    return NotFound(new { Success = false, Message = $"Usuario com ID {id} nao encontrado" });
                }

                existingUser.Name = user.Name;
                existingUser.Email = user.Email;
                existingUser.Age = user.Age;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Usuario atualizado com ID: {UserId}", id);

                // 🚀 Notifica atualização
                await _hubContext.Clients.All.SendAsync("UserUpdated", existingUser);

                return Ok(new { Success = true, Message = "Usuario atualizado com sucesso", Data = existingUser });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar usuario com ID: {UserId}", id);
                return StatusCode(500, new { Success = false, Message = "Erro ao atualizar usuario", Error = ex.Message });
            }
        }

        // DELETE: api/users/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("Tentativa de deletar usuario nao encontrado: {UserId}", id);
                    return NotFound(new { Success = false, Message = $"Usuario com ID {id} nao encontrado" });
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Usuario deletado com ID: {UserId}", id);

                // 🚀 Notifica exclusão
                await _hubContext.Clients.All.SendAsync("UserDeleted", id);

                return Ok(new { Success = true, Message = "Usuario deletado com sucesso" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao deletar usuario com ID: {UserId}", id);
                return StatusCode(500, new { Success = false, Message = "Erro ao deletar usuario", Error = ex.Message });
            }
        }

        // POST: api/users/reset-database
        [HttpPost("reset-database")]
        public async Task<ActionResult> ResetDatabase()
        {
            try
            {
                await _context.Database.EnsureDeletedAsync();
                await _context.Database.EnsureCreatedAsync();

                _context.Users.AddRange(
                    new User { Name = "João Silva", Email = "joao@email.com", Age = 30 },
                    new User { Name = "Maria Santos", Email = "maria@email.com", Age = 25 },
                    new User { Name = "Pedro Oliveira", Email = "pedro@email.com", Age = 35 }
                );
                await _context.SaveChangesAsync();

                // 🚀 Atualiza todos os clientes com os novos dados
                await _hubContext.Clients.All.SendAsync("DatabaseReset");

                return Ok(new { Success = true, Message = "Banco resetado e recriado com sucesso!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Success = false, Message = $"Erro: {ex.Message}" });
            }
        }

        // GET: api/users/search
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<User>>> SearchUsers(
            [FromQuery] string q,
            [FromQuery] int limit = 20)
        {
            try
            {
                if (string.IsNullOrEmpty(q) || q.Length < 2)
                {
                    return BadRequest(new { Success = false, Message = "Termo de busca deve ter pelo menos 2 caracteres" });
                }

                var users = await _context.Users
                    .Where(u =>
                        (u.Name != null && u.Name.Contains(q)) ||
                        (u.Email != null && u.Email.Contains(q)))
                    .Take(limit)
                    .ToListAsync();

                _logger.LogInformation("Busca por '{SearchTerm}' retornou {ResultCount} resultados", q, users.Count);

                return Ok(new { Success = true, Data = users, SearchTerm = q, Count = users.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro na busca por: {SearchTerm}", q);
                return StatusCode(500, new { Success = false, Message = "Erro na busca", Error = ex.Message });
            }
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
    }
}
