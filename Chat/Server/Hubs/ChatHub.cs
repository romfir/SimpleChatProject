using Chat.Shared;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public async Task AddToGroup(string chatRoomName)
           => await Groups.AddToGroupAsync(Context.ConnectionId, chatRoomName).ConfigureAwait(false);

        public async Task RemoveFromGroup(string chatRoomName)
            => await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatRoomName).ConfigureAwait(false);

        //todo get na liste chatroomów?
        //todo sprawdzac czy id chatRoomName nie jest nullem/pusty?
        public async Task SendMessage(Message message, string chatRoomName)
        {
            message.TimeStamp = DateTime.Now;
            message.Id = Guid.NewGuid();

            await Clients.Group(chatRoomName).SendAsync("ReceiveMessage", message).ConfigureAwait(false);

            string chatGroupListName = GetGroupCacheListName(chatRoomName);

            if (_cache.TryGetValue(chatGroupListName, out IEnumerable<Message> messages))
            {
                //todo to trzymanie list jest po prostu złe
                //dodanie nowego elementu nie powinno wymagac pobrania calej listy!
                //to bedzie staszne jakbym dodal obsluge fotek w wiadomosciach (base64?) chyba ze bylyby na serwerze z jakims id po prostu

                //jesli nie wymysle nic lepszego to mozna trzymac jedna liste guidów i kazda wiadomosc oddzielnie w memorycache to nie wydaje sie zle
                //+ lista chatroomow?

                // 1. Lista chatroomów key: chatrooms i w nim ienumerable z nazwami
                //      1.1 Przy nowej wiadomosci jest sprawdzane czy w liscie jest ten channel, jak jest to get listy i do niej dodanie + dodanie normalne

                // 2. Lista widomosci na chatroom, key: messages:chatroomnamne i w nim ienumerable z guidami

                // 3. Same wiadomosci trzymane po prostu message:guid i message w nim
                _cache.Set(chatGroupListName, messages.Append(message));
            }
            else
            {
                _cache.Set(chatGroupListName, (IEnumerable<Message>)new List<Message> { message }); //todo dodac extension method .Yield?
            }
        }

        //todo 2 walidacja usera ofc jakby jakas byla

        // IMemoryCache NIE jest thread safe! Zrobic wlasny locking mechanism?
        //todo isemaphoreslim? Ale tylko na ta dodatkowa liste z guidami jak bedzie
        public async Task DeleteMessage(Guid messageId, string chatRoomName)
        {
            string chatGroupListName = GetGroupCacheListName(chatRoomName);

            if (_cache.TryGetValue(chatGroupListName, out IEnumerable<Message> messages))
            {
                _cache.Set(chatGroupListName, messages.Where(m => m.Id != messageId));
            }

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
            if (_cache.TryGetValue(GetGroupCacheListName(chatRoomName), out IEnumerable<Message> messages))
            {
                foreach (Message message in messages)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await writer.WriteAsync(message, cancellationToken).ConfigureAwait(false);
                }

                writer.Complete(); //?
            }
        }

        //todo liczba userow gdzies wyswietlona? (lista? do listy trzeba by miec username i handlowac jego zmiane... latwiej dodac normalne logowanie)
        public override Task OnConnectedAsync() => base.OnConnectedAsync();

        public override Task OnDisconnectedAsync(Exception? exception) => base.OnDisconnectedAsync(exception);

        private static string GetGroupCacheListName(string groupName)
            => _cacheListName + ":" + groupName;
    }
}