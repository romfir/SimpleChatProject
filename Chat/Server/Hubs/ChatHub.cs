using Chat.Server.Tools;
using Chat.Shared;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Tools;

namespace Chat.Server.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IMemoryCache _cache;

        public ChatHub(IMemoryCache cache)
            => _cache = cache;

        public async Task AddToGroup(string chatRoomName)
           => await Groups.AddToGroupAsync(Context.ConnectionId, chatRoomName).ConfigureAwait(false);

        public async Task RemoveFromGroup(string chatRoomName)
            => await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatRoomName).ConfigureAwait(false);

        //todo get na liste chatroomów?
        public async Task SendMessage(Message message, string chatRoomName)
        {
            Preconditions.CheckNotNull(message, nameof(message));
            Preconditions.CheckNotNull(chatRoomName, nameof(chatRoomName));

            message.TimeStamp = DateTime.Now;
            message.Id = Guid.NewGuid();

            await Clients.Group(chatRoomName).SendAsync("ReceiveMessage", message).ConfigureAwait(false);

            if (_cache.TryGetValue(CacheKeys.ChatRoomsListKey, out IEnumerable<string> chatRooms))
            {
                if (!chatRooms.Any(a => a == chatRoomName))
                {
                    _cache.Set(CacheKeys.ChatRoomsListKey, chatRooms.Append(chatRoomName));
                }
            }
            else
            {
                _cache.Set(CacheKeys.ChatRoomsListKey, chatRoomName.Yield());
            }

            string messageIdsKey = GetMessageListKey(chatRoomName);

            if (_cache.TryGetValue(messageIdsKey, out IEnumerable<Guid> messagesIds))
            {
                _cache.Set(messageIdsKey, messagesIds.Append(message.Id));
            }
            else
            {
                _cache.Set(messageIdsKey, message.Id.Yield());
            }

            _cache.Set(message.Id, message);
        }

        public async Task DeleteMessage(Guid messageId, string chatRoomName)
        {
            string messageIdsKey = GetMessageListKey(chatRoomName);

            if (_cache.TryGetValue(messageIdsKey, out IEnumerable<Guid> messageIds))
            {
                _cache.Set(messageIdsKey, messageIds.Where(i => i != messageId));
            }

            _cache.Remove(messageId);

            await Clients.Group(chatRoomName).SendAsync("DeleteMessage", messageId).ConfigureAwait(false);
        }

        public async Task UserWriting(string userName, string chatRoomName)
            => await Clients.OthersInGroup(chatRoomName).SendAsync("UserIsWriting", userName).ConfigureAwait(false);

        public ChannelReader<Message> RequestMessages(string chatRoomName, CancellationToken cancellationToken)
        {
            var channel = Channel.CreateUnbounded<Message>();
            _ = RequestMessagesInternal(chatRoomName, channel.Writer, cancellationToken);

            return channel.Reader;
        }

        private async Task RequestMessagesInternal(string chatRoomName, ChannelWriter<Message> writer, CancellationToken cancellationToken)
        {
            if (_cache.TryGetValue(GetMessageListKey(chatRoomName), out IEnumerable<Guid> messageIds))
            {
                foreach (Guid messageId in messageIds)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await writer.WriteAsync(_cache.Get<Message>(messageId), cancellationToken).ConfigureAwait(false);
                }
            }

            writer.Complete();
        }

        //todo liczba userow gdzies wyswietlona? (lista? do listy trzeba by miec username i handlowac jego zmiane... latwiej dodac normalne logowanie)
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync().ConfigureAwait(false);

            if (!_cache.TryGetValue(CacheKeys.ChatRoomsListKey, out IEnumerable<string> chatRooms))
            {
                chatRooms = "Global".Yield();
            }

            await Clients.Caller.SendAsync("ChannelsList", chatRooms).ConfigureAwait(false);
        }

        private static string GetMessageListKey(string chatRoomName)
            => CacheKeys.ChatRoomMessagesListPrefix + chatRoomName;
    }
}