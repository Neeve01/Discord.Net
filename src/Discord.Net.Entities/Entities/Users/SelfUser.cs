#if !RPC
using Discord.API.Rest;
using System;
using System.Threading.Tasks;
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
#if REST
    public partial class RestSelfUser : RestUser, ISelfUser
#elif WEBSOCKET
    public partial class SocketSelfUser : SocketUser, ISelfUser
#endif
    {
        protected long _idleSince;
        protected UserStatus _status;
        protected Game _game;

        internal override DiscordClient Discord { get; }

        public string Email { get; private set; }
        public bool IsVerified { get; private set; }
        public bool IsMfaEnabled { get; private set; }

        public override UserStatus Status => _status;
        public override Game Game => _game;

#if REST
        public RestSelfUser(DiscordClient discord, Model model)
#elif WEBSOCKET
        public SocketSelfUser(DiscordClient discord, Model model)
#endif
            : base(model)
        {
            Discord = discord;
        }
        public override void Update(Model model)
        {
            PreUpdate(model);
            base.Update(model);

            if (model.Email.IsSpecified)
                Email = model.Email.Value;
            if (model.Verified.IsSpecified)
                IsVerified = model.Verified.Value;
            if (model.MfaEnabled.IsSpecified)
                IsMfaEnabled = model.MfaEnabled.Value;
            PostUpdate(model);
        }
        partial void PreUpdate(Model model);
        partial void PostUpdate(Model model);

        public async Task ModifyAsync(Action<ModifyCurrentUserParams> func)
        {
            if (func == null) throw new NullReferenceException(nameof(func));

            var args = new ModifyCurrentUserParams
            {
                Username = Username,
                AvatarId = _avatarId
            };
            func(args);


            var model = await Discord.ApiClient.ModifySelfAsync(args).ConfigureAwait(false);
            Update(model);
        }

        Task ISelfUser.ModifyStatusAsync(Action<ModifyPresenceParams> func) { throw new NotSupportedException(); }
    }
}
#endif