using Microsoft.AspNetCore.SignalR;

namespace SmartVillageAPI.Hubs
{
    public class SmartVillageHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"[SignalR] Client Connected: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            Console.WriteLine($"[SignalR] Client Disconnected: {Context.ConnectionId}");
            await base.OnDisconnectedAsync(exception);
        }
        public async Task JoinZoneGroup(string zoneId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Zone_{zoneId}");
            Console.WriteLine($"[SignalR] Client joined Zone_{zoneId}");
        }

        public async Task LeaveZoneGroup(string zoneId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Zone_{zoneId}");
        }

    }
}