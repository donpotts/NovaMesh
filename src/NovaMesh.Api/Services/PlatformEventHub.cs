using Microsoft.AspNetCore.SignalR;

namespace NovaMesh.Api.Services;

public class PlatformEventHub : Hub
{
    public async Task BroadcastEvent(string title, string message, string severity = "Info")
    {
        await Clients.All.SendAsync("ReceiveEvent", new
        {
            title,
            message,
            severity,
            timestamp = DateTime.UtcNow
        });
    }
}
