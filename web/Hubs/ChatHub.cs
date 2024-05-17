using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace N8.Web.Hubs.Hubs;

[AllowAnonymous]
public class ChatHub : Hub
{
    private static readonly Dictionary<string, UserInfo> ConnectedUsers = new Dictionary<string, UserInfo>();

    public override async Task OnConnectedAsync()
    {
        var ctx = Context.GetHttpContext();

        var headers = ctx?.Request.Headers;

        if (headers != null)
        {
            var name = headers["user-name"].FirstOrDefault();

            if (!string.IsNullOrEmpty(name))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, name);

                ConnectedUsers[Context.ConnectionId] = new UserInfo(name);
            }
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _ = ConnectedUsers.Remove(Context.ConnectionId);

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(string user, string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", user, message, DateTime.UtcNow);
    }

    public List<UserInfo> GetConnectedUsers()
    {
        return ConnectedUsers.Values.ToList();
    }
}

public record UserInfo(string Name);
