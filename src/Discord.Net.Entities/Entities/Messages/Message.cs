#if !RPC
using Discord.API.Rest;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading.Tasks;
using Model = Discord.API.Message;

#if REST
using DiscordClient = Discord.Rest.DiscordRestClient;
using Guild = Discord.Rest.RestGuild;
using GuildChannel = Discord.Rest.RestGuildChannel;
using Role = Discord.Rest.RestRole;
using User = Discord.Rest.RestUser;
using MessageChannel = Discord.Rest.IRestMessageChannel;
#elif WEBSOCKET
using DiscordClient = Discord.WebSocket.DiscordSocketClient;
using Guild = Discord.WebSocket.SocketGuild;
using GuildChannel = Discord.WebSocket.SocketGuildChannel;
using Role = Discord.WebSocket.SocketRole;
using User = Discord.WebSocket.SocketUser;
using IMessageChannel = Discord.WebSocket.ISocketMessageChannel;
#endif

#if REST
namespace Discord.Rest
#elif WEBSOCKET
namespace Discord.WebSocket
#endif
{
    [DebuggerDisplay(@"{DebuggerDisplay,nq}")]
#if REST
    public abstract partial class RestMessage : RestEntity<ulong>, ISnowflakeEntity, IMessage
#elif WEBSOCKET
    public abstract partial class SocketMessage : SocketEntity<ulong>, ISnowflakeEntity, IGuild
#endif
    {
        private long _timestampTicks;
        
        public ulong Id { get; }
        public MessageChannel Channel { get; }
        IMessageChannel IMessage.Channel => Channel;
        public User Author { get; }
        IUser IMessage.Author => Author;

        public string Content { get; private set; }

        internal override DiscordClient Discord => Channel.Discord;

        public virtual bool IsTTS => false;
        public virtual bool IsPinned => false;
        public virtual DateTimeOffset? EditedTimestamp => null;

        public virtual IReadOnlyCollection<Attachment> Attachments => ImmutableArray.Create<Attachment>();
        IReadOnlyCollection<IAttachment> IMessage.Attachments => Attachments;
        public virtual IReadOnlyCollection<Embed> Embeds => ImmutableArray.Create<Embed>();
        IReadOnlyCollection<IEmbed> IMessage.Embeds => Embeds;
        public virtual IReadOnlyCollection<ulong> MentionedChannelIds => ImmutableArray.Create<ulong>();
        public virtual IReadOnlyCollection<Role> MentionedRoles => ImmutableArray.Create<Role>();
        IReadOnlyCollection<IRole> IMessage.MentionedRoles => MentionedRoles;
        public virtual IReadOnlyCollection<User> MentionedUsers => ImmutableArray.Create<User>();
        IReadOnlyCollection<IUser> IMessage.MentionedUsers => MentionedUsers;

        public DateTimeOffset Timestamp => DateTimeUtils.FromTicks(_timestampTicks);

#if REST
        public RestMessage(IMessageChannel channel, User author, Model model)
#elif WEBSOCKET
        public SocketMessage(IMessageChannel channel, User author, Model model)
#endif
        {
            Id = model.Id;
            Channel = channel;
            Author = author;

            Update(model);
        }

        public virtual void Update(Model model)
        {
            PreUpdate(model);

            var guildChannel = Channel as GuildChannel;
            var guild = guildChannel?.Guild;
            
            if (model.Timestamp.IsSpecified)
                _timestampTicks = model.Timestamp.Value.UtcTicks;

            if (model.Content.IsSpecified)
                Content = model.Content.Value;

            PostUpdate(model);
        }
        partial void PreUpdate(Model model);
        partial void PostUpdate(Model model);

        public async Task ModifyAsync(Action<ModifyMessageParams> func)
        {
            if (func == null) throw new NullReferenceException(nameof(func));

            var args = new ModifyMessageParams();
            func(args);
            var guildChannel = Channel as GuildChannel;

            Model model;
            if (guildChannel != null)
                model = await Discord.ApiClient.ModifyMessageAsync(guildChannel.Guild.Id, Channel.Id, Id, args).ConfigureAwait(false);
            else
                model = await Discord.ApiClient.ModifyDMMessageAsync(Channel.Id, Id, args).ConfigureAwait(false);
                
            Update(model);
        }        
        public async Task DeleteAsync()
        {
            var guildChannel = Channel as GuildChannel;
            if (guildChannel != null)
                await Discord.ApiClient.DeleteMessageAsync(guildChannel.Id, Channel.Id, Id).ConfigureAwait(false);
            else
                await Discord.ApiClient.DeleteDMMessageAsync(Channel.Id, Id).ConfigureAwait(false);
        }
        public async Task PinAsync()
        {
            await Discord.ApiClient.AddPinAsync(Channel.Id, Id).ConfigureAwait(false);
        }
        public async Task UnpinAsync()
        {
            await Discord.ApiClient.RemovePinAsync(Channel.Id, Id).ConfigureAwait(false);
        }

        public override string ToString() => Content;
        private string DebuggerDisplay => $"{Author}: {Content}{(Attachments.Count > 0 ? $" [{Attachments.Count} Attachments]" : "")}";
    }
}
#endif