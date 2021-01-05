using Chat.Shared;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace Chat.Server.Hubs
{
    public class ChatHub : Hub
    {

        //todo mozna dodac jakas zabawe z datami
        //todo nie powinnismy w sumie uzywac all tylko Other, ale co jak jest zalgoowany na dwoch kompach? ALl zostaje
        public async Task SendMessage(Message message)
            => await Clients.All.SendAsync("ReceiveMessage", message).ConfigureAwait(false);

        //todo usuniecie tez z bazy jakbysmy jakas mieli
        //todo 2 walidacja usera ofc jakby jakas byla
        public async Task DeleteMessage(Guid messageId)
            => await Clients.All.SendAsync("DeleteMessage", messageId).ConfigureAwait(false);

        public async Task UserWriting(string userName)
            => await Clients.Others.SendAsync("UserIsWriting", userName).ConfigureAwait(false);

        public override Task OnConnectedAsync() => base.OnConnectedAsync();

        public override Task OnDisconnectedAsync(Exception exception) => base.OnDisconnectedAsync(exception);
    }
}