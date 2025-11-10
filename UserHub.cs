using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace MinhaApiCrud.Hubs
{
    public class UserHub : Hub
    {
        // 🔹 Notifica todos os clientes quando um novo usuário for adicionado
        public async Task SendUserAdded(object user)
        {
            await Clients.All.SendAsync("UserAdded", user);
        }

        // 🔹 Notifica sobre atualizações
        public async Task SendUserUpdated(object user)
        {
            await Clients.All.SendAsync("UserUpdated", user);
        }

        // 🔹 Notifica sobre exclusões
        public async Task SendUserDeleted(int userId)
        {
            await Clients.All.SendAsync("UserDeleted", userId);
        }

        // 🔹 (Opcional) método de log de conexões — útil para debug
        public override async Task OnConnectedAsync()
        {
            await Clients.Caller.SendAsync("Connected", "Conectado ao SignalR UserHub!");
            await base.OnConnectedAsync();
        }
    }
}
