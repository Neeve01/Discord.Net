#if !RPC
using Discord.API.Rest;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Model = Discord.API.Channel;
using MessageModel = Discord.API.Message;

#if REST
using DiscordClient = Discord.Rest.DiscordRestClient;
using User = Discord.Rest.RestUser;
using GroupUser = Discord.Rest.RestGroupUser;
using Message = Discord.Rest.RestMessage;
using UserMessage = Discord.Rest.RestUserMessage;
using SystemMessage = Discord.Rest.RestSystemMessage;
#elif WEBSOCKET
using DiscordClient = Discord.WebSocket.DiscordSocketClient;
using User = Discord.WebSocket.SocketUser;
using GroupUser = Discord.WebSocket.SocketGroupUser;
using Message = Discord.WebSocket.SocketMessage;
using UserMessage = Discord.WebSocket.SocketUserMessage;
using SystemMessage = Discord.WebSocket.SocketSystemMessage;
#endif

#if REST
namespace Discord.Rest
#elif WEBSOCKET
namespace Discord.WebSocket
#endif
{
    [DebuggerDisplay(@"{DebuggerDisplay,nq}")]
#if REST
    public partial class RestGroupChannel : RestChannel, ISnowflakeEntity, IGroupChannel
#elif WEBSOCKET
    public partial class SocketGroupChannel : SocketChannel, ISnowflakeEntity, IGroupChannel
#endif
    {
        private string _iconId;
        private ImmutableDictionary<ulong, RestGroupUser> _users;

        public override ulong Id { get; }
        internal override DiscordClient Discord { get; }

        public string Name { get; private set; }

        public IReadOnlyCollection<RestGroupUser> Users => _users.ToReadOnlyCollection();
        public IReadOnlyCollection<User> Recipients => _users.Concat(new User[] { Discord.CurrentUser }).ToReadOnlyCollection(() => _users.Count + 1);
        public virtual IReadOnlyCollection<Message> CachedMessages => ImmutableArray.Create<Message>();
        public string IconUrl => API.CDN.GetChannelIconUrl(Id, _iconId);
        
        internal RestGroupChannel(DiscordClient discord, Model model)
        {
            Id = model.Id;
            Discord = discord;

            Update(model);
        }

        internal override void Update(Model model)
        {
            PreUpdate(model);

            if (model.Name.IsSpecified)
                Name = model.Name.Value;
            if (model.Icon.IsSpecified)
                _iconId = model.Icon.Value;

            if (model.Recipients.IsSpecified)
            {
                var recipients = model.Recipients.Value;
                var users = ImmutableDictionary.CreateBuilder<ulong, RestGroupUser>();
                for (int i = 0; i < recipients.Length; i++)
                    users[recipients[i].Id] = new RestGroupUser(this, new RestUser(recipients[i]));
                _users = users.ToImmutable();
            }
            
            PostUpdate(model);
        }
        partial void PreUpdate(Model model);
        partial void PostUpdate(Model model);
        
        public async Task LeaveAsync()
        {
            await Discord.ApiClient.DeleteChannelAsync(Id).ConfigureAwait(false);
        }

        public User GetUser(ulong id)
        {
            GroupUser user;
            if (_users.TryGetValue(id, out user))
                return user;
            var currentUser = Discord.CurrentUser;
            if (id == currentUser.Id)
                return currentUser;
            return null;
        }
        IUser IChannel.GetCachedUser(ulong id) => GetUser(id);
        Task<IUser> IChannel.GetUserAsync(ulong id) => Task.FromResult<IUser>(GetUser(id));
        public async Task AddUserAsync(IUser user)
        {
            await Discord.ApiClient.AddGroupRecipientAsync(Id, user.Id).ConfigureAwait(false);
        }

        public async Task<UserMessage> SendMessageAsync(string text, bool isTTS)
        {
            var args = new CreateMessageParams { Content = text, IsTTS = isTTS };
            var model = await Discord.ApiClient.CreateDMMessageAsync(Id, args).ConfigureAwait(false);
            return CreateOutgoingMessage(model);
        }
        public async Task<UserMessage> SendFileAsync(string filePath, string text, bool isTTS)
        {
            string filename = Path.GetFileName(filePath);
            using (var file = File.OpenRead(filePath))
            {
                var args = new UploadFileParams(file) { Filename = filename, Content = text, IsTTS = isTTS };
                var model = await Discord.ApiClient.UploadDMFileAsync(Id, args).ConfigureAwait(false);
                return CreateOutgoingMessage(model);
            }
        }
        public async Task<UserMessage> SendFileAsync(Stream stream, string filename, string text, bool isTTS)
        {
            var args = new UploadFileParams(stream) { Filename = filename, Content = text, IsTTS = isTTS };
            var model = await Discord.ApiClient.UploadDMFileAsync(Id, args).ConfigureAwait(false);
            return CreateOutgoingMessage(model);
        }
        public virtual async Task<Message> GetMessageAsync(ulong id)
        {
            var model = await Discord.ApiClient.GetChannelMessageAsync(Id, id).ConfigureAwait(false);
            if (model != null)
                return CreateIncomingMessage(model);
            return null;
        }
        public virtual async Task<IReadOnlyCollection<Message>> GetMessagesAsync(int limit)
        {
            var args = new GetChannelMessagesParams { Limit = limit };
            var models = await Discord.ApiClient.GetChannelMessagesAsync(Id, args).ConfigureAwait(false);
            return models.Select(x => CreateIncomingMessage(x)).ToImmutableArray();
        }
        public virtual async Task<IReadOnlyCollection<Message>> GetMessagesAsync(ulong fromMessageId, Direction dir, int limit)
        {
            var args = new GetChannelMessagesParams { Limit = limit, RelativeMessageId = fromMessageId, RelativeDirection = dir };
            var models = await Discord.ApiClient.GetChannelMessagesAsync(Id, args).ConfigureAwait(false);
            return models.Select(x => CreateIncomingMessage(x)).ToImmutableArray();
        }
        public async Task DeleteMessagesAsync(IEnumerable<IMessage> messages)
        {
            await Discord.ApiClient.DeleteDMMessagesAsync(Id, new DeleteMessagesParams { MessageIds = messages.Select(x => x.Id) }).ConfigureAwait(false);
        }
        public async Task<IReadOnlyCollection<Message>> GetPinnedMessagesAsync()
        {
            var models = await Discord.ApiClient.GetPinsAsync(Id);
            return models.Select(x => CreateIncomingMessage(x)).ToImmutableArray();
        }

        public async Task TriggerTypingAsync()
        {
            await Discord.ApiClient.TriggerTypingIndicatorAsync(Id).ConfigureAwait(false);
        }

        internal UserMessage CreateOutgoingMessage(MessageModel model)
        {
            return new UserMessage(this, new User(model.Author.Value), model);
        }
        internal Message CreateIncomingMessage(MessageModel model)
        {
            if (model.Type == MessageType.Default)
                return new UserMessage(this, new User(model.Author.Value), model);
            else
                return new SystemMessage(this, new User(model.Author.Value), model);
        }

        public override string ToString() => Name;
        private string DebuggerDisplay => $"@{Name} ({Id}, Group)";

        //Interfaces
        IReadOnlyCollection<IUser> IPrivateChannel.Recipients => Recipients;

        async Task<IUserMessage> IMessageChannel.SendMessageAsync(string text, bool isTTS)
            => await SendMessageAsync(text, isTTS).ConfigureAwait(false);
        async Task<IUserMessage> IMessageChannel.SendFileAsync(string filePath, string text, bool isTTS)
            => await SendFileAsync(filePath, text, isTTS).ConfigureAwait(false);
        async Task<IUserMessage> IMessageChannel.SendFileAsync(Stream stream, string filename, string text, bool isTTS)
            => await SendFileAsync(stream, filename, text, isTTS).ConfigureAwait(false);
        async Task<IMessage> IMessageChannel.GetMessageAsync(ulong id)
            => await GetMessageAsync(id).ConfigureAwait(false);
        async Task<IReadOnlyCollection<IMessage>> IMessageChannel.GetMessagesAsync(int limit)
            => await GetMessagesAsync(limit).ConfigureAwait(false);
        async Task<IReadOnlyCollection<IMessage>> IMessageChannel.GetMessagesAsync(ulong fromMessageId, Direction dir, int limit)
            => await GetMessagesAsync(fromMessageId, dir, limit).ConfigureAwait(false);
        async Task<IReadOnlyCollection<IMessage>> IMessageChannel.GetPinnedMessagesAsync()
            => await GetPinnedMessagesAsync().ConfigureAwait(false);
    }
}
#endif