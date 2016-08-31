#if !RPC
using System;
using System.Diagnostics;
using Model = Discord.API.User;

#if REST
using DiscordClient = Discord.Rest.DiscordRestClient;
using Guild = Discord.Rest.RestGuild;
using Role = Discord.Rest.RestRole;
using User = Discord.Rest.RestUser;
#elif WEBSOCKET
using DiscordClient = Discord.WebSocket.DiscordSocketClient;
using Guild = Discord.WebSocket.SocketGuild;
using Role = Discord.WebSocket.SocketRole;
using User = Discord.WebSocket.SocketUser;
#endif

#if REST
namespace Discord.Rest
#elif WEBSOCKET
namespace Discord.WebSocket
#endif
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
#if REST
    public partial class RestUser : RestEntity<ulong>, IUser
#elif WEBSOCKET
    public partial class SocketUser : SocketEntity<ulong>, IUser
#endif
    {
        protected string _avatarId;

        public ulong Id { get; }
        public bool IsBot { get; private set; }
        public string Username { get; private set; }
        public ushort DiscriminatorValue { get; private set; }

        internal override DiscordClient Discord { get { throw new NotSupportedException(); } }

        public string AvatarUrl => API.CDN.GetUserAvatarUrl(Id, _avatarId);
        public string Discriminator => DiscriminatorValue.ToString("D4");
        public string Mention => MentionsHelper.MentionUser(Id);
        public virtual Game Game => null;
        public virtual UserStatus Status => UserStatus.Unknown;
#if REST
        public RestUser(Model model)
#elif WEBSOCKET
        public SocketUser(Model model)
#endif
        {
            Id = model.Id;

            Update(model);
        }
        public virtual void Update(Model model)
        {
            PreUpdate(model);
            if (model.Avatar.IsSpecified)
                _avatarId = model.Avatar.Value;
            if (model.Discriminator.IsSpecified)
                DiscriminatorValue = ushort.Parse(model.Discriminator.Value);
            if (model.Bot.IsSpecified)
                IsBot = model.Bot.Value;
            if (model.Username.IsSpecified)
                Username = model.Username.Value;
            PostUpdate(model);
        }
        partial void PreUpdate(Model model);
        partial void PostUpdate(Model model);

        public override string ToString() => $"{Username}#{Discriminator}";
        private string DebuggerDisplay => $"{Username}#{Discriminator} ({Id})";
    }
}
#endif