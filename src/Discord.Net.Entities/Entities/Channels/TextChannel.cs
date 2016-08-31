#if !RPC
using Discord.API.Rest;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Model = Discord.API.Channel;
using MessageModel = Discord.API.Message;
using System.IO;

#if REST
using Guild = Discord.Rest.RestGuild;
using GuildUser = Discord.Rest.RestGuildUser;
using User = Discord.Rest.RestUser;
using Message = Discord.Rest.RestMessage;
using UserMessage = Discord.Rest.RestUserMessage;
using SystemMessage = Discord.Rest.RestSystemMessage;
#elif WEBSOCKET
using Guild = Discord.WebSocket.SocketGuild;
using GuildUser = Discord.WebSocket.SocketGuildUser;
using User = Discord.WebSocket.SocketUser;
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
    public partial class RestTextChannel : RestGuildChannel, ITextChannel
#elif WEBSOCKET
    public partial class SocketTextChannel : SocketGuildChannel, ITextChannel
#endif
    {
        public string Topic { get; private set; }
        
        public string Mention => MentionsHelper.MentionChannel(Id);
        public virtual IReadOnlyCollection<IMessage> CachedMessages => ImmutableArray.Create<IMessage>();

#if REST
        internal RestTextChannel(Guild guild, Model model)
#elif WEBSOCKET
        internal SocketTextChannel(Guild guild, Model model)
#endif
            : base(guild, model)
        {
            Update(model);
        }

        public override void Update(Model model)
        {
            PreUpdate(model);

            Topic = model.Topic.Value;
            base.Update(model);

            PostUpdate(model);
        }
        partial void PreUpdate(Model model);
        partial void PostUpdate(Model model);

        public async Task ModifyAsync(Action<ModifyTextChannelParams> func)
        {
            if (func == null) throw new NullReferenceException(nameof(func));

            var args = new ModifyTextChannelParams
            {
                Name = Name
            };
            func(args);
            
            var model = await Discord.ApiClient.ModifyGuildChannelAsync(Id, args).ConfigureAwait(false);
            Update(model);
        }
        
        public override async Task<GuildUser> GetUserAsync(ulong id)
        {
            var user = await Guild.GetUserAsync(id).ConfigureAwait(false);
            if (user != null && Permissions.GetValue(Permissions.ResolveChannel(user, this, user.GuildPermissions.RawValue), ChannelPermission.ReadMessages))
                return user;
            return null;
        }
        public override async Task<IReadOnlyCollection<GuildUser>> GetUsersAsync()
        {
            var users = await Guild.GetUsersAsync().ConfigureAwait(false);
            return users.Where(x => Permissions.GetValue(Permissions.ResolveChannel(x, this, x.GuildPermissions.RawValue), ChannelPermission.ReadMessages)).ToImmutableArray();
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

        private UserMessage CreateOutgoingMessage(MessageModel model)
        {
            return new UserMessage(this, new User(model.Author.Value), model);
        }
        private Message CreateIncomingMessage(MessageModel model)
        {
            if (model.Type == MessageType.Default)
                return new UserMessage(this, new User(model.Author.Value), model);
            else
                return new SystemMessage(this, new User(model.Author.Value), model);
        }

        private string DebuggerDisplay => $"{Name} ({Id}, Text)";

        //Interfaces
        async Task<IUserMessage> IMessageChannel.SendMessageAsync(string text, bool isTTS)
            => await SendMessageAsync(text, isTTS).ConfigureAwait(false);
        async Task<IUserMessage> IMessageChannel.SendFileAsync(string filePath, string text, bool isTTS)
            => await SendFileAsync(filePath, text, isTTS).ConfigureAwait(false);
        async Task<IUserMessage> IMessageChannel.SendFileAsync(Stream stream, string filename, string text, bool isTTS)
            => await SendFileAsync(stream, filename, text, isTTS).ConfigureAwait(false);
        async Task<IMessage> IMessageChannel.GetMessageAsync(ulong id)
            => await GetMessageAsync(id);
        async Task<IReadOnlyCollection<IMessage>> IMessageChannel.GetMessagesAsync(int limit)
            => await GetMessagesAsync(limit);
        async Task<IReadOnlyCollection<IMessage>> IMessageChannel.GetMessagesAsync(ulong fromMessageId, Direction dir, int limit)
            => await GetMessagesAsync(fromMessageId, dir, limit);
        async Task<IReadOnlyCollection<IMessage>> IMessageChannel.GetPinnedMessagesAsync()
            => await GetPinnedMessagesAsync();
    }
}
#endif