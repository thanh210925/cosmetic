using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace COSMETICC.Hubs
{
    public class NotificationHub : Hub
    {
        // Join user to their personal user group
        public async Task JoinUserGroup(string userId)
        {
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
            }
        }

        // Join admin to Admin group
        public async Task JoinAdminGroup()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "AdminGroup");
        }

        // Leave admin group
        public async Task LeaveAdminGroup()
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "AdminGroup");
        }
    }
}
