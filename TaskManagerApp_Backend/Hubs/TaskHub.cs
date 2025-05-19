using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace TaskManagerApp_Backend.Hubs
{
    // SignalR Hub để quản lý thông báo gán công việc
    [Authorize]
    public class TaskHub : Hub
    {
        // Gửi thông báo đến người dùng cụ thể khi công việc được gán
        public async Task SendTaskAssignedNotification(string userId, string taskDetails)
        {
            await Clients.User(userId).SendAsync("ReceiveMessage", taskDetails);
        }
    }
}