#if !RPC
using Discord.API.Rest;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Model = Discord.API.GuildMember;
using PresenceModel = Discord.API.Presence;

#if REST
using DiscordClient = Discord.Rest.DiscordRestClient;
using Guild = Discord.Rest.RestGuild;
using Role = Discord.Rest.RestRole;
using User = Discord.Rest.RestUser;
using DMChannel = Discord.Rest.RestDMChannel;
#elif WEBSOCKET
using DiscordClient = Discord.WebSocket.DiscordSocketClient;
using Guild = Discord.WebSocket.SocketGuild;
using Role = Discord.WebSocket.SocketRole;
using User = Discord.WebSocket.SocketUser;
using DMChannel = Discord.WebSocket.SocketDMChannel;
#endif

#if REST
namespace Discord.Rest
#elif WEBSOCKET
namespace Discord.WebSocket
#endif
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
#if REST
    public partial class RestGuildUser : RestEntity<ulong>, IGuildUser
#elif WEBSOCKET
    public partial class SocketGuildUser : SocketEntity<ulong>, IGuildUser
#endif
    {
        private long? _joinedAtTicks;
        
        public string Nickname { get; private set; }
        public GuildPermissions GuildPermissions { get; private set; }

        public Guild Guild { get; private set; }
        public User User { get; private set; }

        public ulong Id => User.Id;
        public string AvatarUrl => User.AvatarUrl;
        public string Discriminator => User.Discriminator;
        public ushort DiscriminatorValue => User.DiscriminatorValue;
        public bool IsBot => User.IsBot;
        public string Mention => MentionsHelper.MentionUser(Id, Nickname != null);
        public string Username => User.Username;
        public UserStatus Status => UserStatus.Unknown;
        public Game Game => null;
        public DateTimeOffset? JoinedAt => DateTimeUtils.FromTicks(_joinedAtTicks);
        internal override DiscordClient Discord => Guild.Discord;

#if REST
        public RestGuildUser(Guild guild, User user)
#elif WEBSOCKET
        public SocketGuildUser(Guild guild, User user)
#endif
        {
            Guild = guild;
            User = user;
            Roles = ImmutableArray.Create<Role>();
        }
#if REST
        public RestGuildUser(Guild guild, User user, Model model)
#elif WEBSOCKET
        public SocketGuildUser(Guild guild, User user, Model model)
#endif
            : this(guild, user)
        {
            Update(model);
        }
#if REST
        public RestGuildUser(Guild guild, User user, PresenceModel model)
#elif WEBSOCKET
        public SocketGuildUser(Guild guild, User user, PresenceModel model)
#endif
            : this(guild, user)
        {
            Update(model);
        }
        public void Update(Model model)
        {
            PreUpdate(model);

            _joinedAtTicks = model.JoinedAt.UtcTicks;
            if (model.Nick.IsSpecified)
                Nickname = model.Nick.Value;
            GuildPermissions = new GuildPermissions(Permissions.ResolveGuild(this));

            PostUpdate(model);
        }
        partial void PreUpdate(Model model);
        partial void PostUpdate(Model model);

        public virtual void Update(PresenceModel model)
        {
            PreUpdate(model);
            if (model.Nick.IsSpecified)
                Nickname = model.Nick.Value;
            if (model.Roles.IsSpecified)
                GuildPermissions = new GuildPermissions(Permissions.ResolveGuild(this));
            PostUpdate(model);
        }
        partial void PreUpdate(PresenceModel model);
        partial void PostUpdate(PresenceModel model);

        public async Task ModifyAsync(Action<ModifyGuildMemberParams> func)
        {
            if (func == null) throw new NullReferenceException(nameof(func));

            var args = new ModifyGuildMemberParams();
            func(args);

            await Discord.ApiClient.ModifyGuildMemberAsync(Guild.Id, Id, args).ConfigureAwait(false);
            Update(args);
        }
        public async Task KickAsync()
        {
            await Discord.ApiClient.RemoveGuildMemberAsync(Guild.Id, Id).ConfigureAwait(false);
        }

        public override string ToString() => $"{Username}#{Discriminator}";
        private string DebuggerDisplay => $"{Username}#{Discriminator} ({Id})";

        public ChannelPermissions GetPermissions(IGuildChannel channel)
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            return new ChannelPermissions(Permissions.ResolveChannel(this, channel, GuildPermissions.RawValue));
        }
        
        public async Task<IDMChannel> CreateDMChannelAsync()
        {
            var args = new CreateDMChannelParams { Recipient = this };
            var model = await Discord.ApiClient.CreateDMChannelAsync(args).ConfigureAwait(false);

            return new DMChannel(Discord, new User(model.Recipients.Value[0]), model);
        }

        //Interfaces
        IGuild IGuildUser.Guild => Guild;
        IReadOnlyCollection<IRole> IGuildUser.Roles => Roles;
        bool IVoiceState.IsDeafened => false;
        bool IVoiceState.IsMuted => false;
        bool IVoiceState.IsSelfDeafened => false;
        bool IVoiceState.IsSelfMuted => false;
        bool IVoiceState.IsSuppressed => false;
        IVoiceChannel IVoiceState.VoiceChannel => null;
        string IVoiceState.VoiceSessionId => null;
    }
}
#endif