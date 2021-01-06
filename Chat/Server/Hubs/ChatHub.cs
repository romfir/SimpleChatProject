using Chat.Shared;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Chat.Server.Hubs
{
    public class ChatHub : Hub
    {
        private const string _cacheListName = "list";

        private readonly IMemoryCache _cache;

        public ChatHub(IMemoryCache cache)
            => _cache = cache;

        public async Task SendMessage(Message message)
        {
            await Clients.All.SendAsync("ReceiveMessage", message).ConfigureAwait(false);

            if (_cache.TryGetValue(_cacheListName, out IEnumerable<Message> messages))
            {
                _cache.Set(_cacheListName, messages.Append(message));
            }
            else
            {
                _cache.Set(_cacheListName, new List<Message> { message });
            }
        }

        //todo 2 walidacja usera ofc jakby jakas byla
        public async Task DeleteMessage(Guid messageId)
        {
            if (_cache.TryGetValue(_cacheListName, out IEnumerable<Message> messages))
            {
                _cache.Set(_cacheListName, messages.Where(m => m.Id != messageId).ToList());
            }
            await Clients.All.SendAsync("DeleteMessage", messageId).ConfigureAwait(false);
        }

        public async Task UserWriting(string userName)
            => await Clients.Others.SendAsync("UserIsWriting", userName).ConfigureAwait(false);

        public async Task RequestMessages()
        {
            if (_cache.TryGetValue(_cacheListName, out IEnumerable<Message> messages))
            {
                await Clients.Caller.SendAsync("MessageListLoad", messages).ConfigureAwait(false);
            }
        }

        public ChannelReader<Message> RequestMessagesNew(CancellationToken cancellationToken)
        {
            var channel = Channel.CreateUnbounded<Message>();
            _ = RequestMessagesNewInternal(channel.Writer, cancellationToken);
            return channel.Reader;
        }

        private async Task RequestMessagesNewInternal(ChannelWriter<Message> writer, CancellationToken cancellationToken)
        {
            if (_cache.TryGetValue(_cacheListName, out IEnumerable<Message> messages))
            {
                foreach (Message message in messages)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await writer.WriteAsync(message, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        //private async IAsyncEnumerable<Message> RequestMessagesNewInternal([EnumeratorCancellation] CancellationToken cancellationToken)
        //{
        //    if (_cache.TryGetValue(_cacheListName, out IEnumerable<Message> messages))
        //    {
        //        foreach (Message message in messages)
        //        {
        //            cancellationToken.ThrowIfCancellationRequested();
        //            yield return message;
        //        }
        //    }
        //}

        public override Task OnConnectedAsync() => base.OnConnectedAsync();

        public override Task OnDisconnectedAsync(Exception exception) => base.OnDisconnectedAsync(exception);
    }
}