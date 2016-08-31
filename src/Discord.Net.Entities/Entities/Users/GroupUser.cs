#if !RPC
using Discord.API.Rest;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

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
    [DebuggerDisplay(@"{DebuggerDisplay,nq}")]
#if REST
    public partial class RestGroupUser : RestEntity<ulong>, IGroupUser
#elif WEBSOCKET
    public partial class SocketGroupUser : SocketEntity<ulong>, IGroupUser
#endif
    {
        public RestGroupChannel Channel { get; private set; }
        public User User { get; private set; }

        public ulong Id => User.Id;
        public string AvatarUrl => User.AvatarUrl;
        public string Discriminator => User.Discriminator;
        public ushort DiscriminatorValue => User.DiscriminatorValue;
        public bool IsBot => User.IsBot;
        public string Username => User.Username;
        public string Mention => MentionsHelper.MentionUser(Id, false);

        public virtual UserStatus Status => UserStatus.Unknown;
        public virtual Game Game => null;

        internal override DiscordClient Discord => Channel.Discord;

#if REST
        public RestGroupUser(RestGroupChannel channel, User user)
#elif WEBSOCKET
        public SocketGroupUser(SocketGroupChannel channel, User user)
#endif
        {
            Channel = channel;
            User = user;
        }

        public async Task KickAsync()
        {
            await Discord.ApiClient.RemoveGroupRecipientAsync(Channel.Id, Id).ConfigureAwait(false);
        }

        public async Task<IDMChannel> CreateDMChannelAsync()
        {
            var args = new CreateDMChannelParams { Recipient = this };
            var model = await Discord.ApiClient.CreateDMChannelAsync(args).ConfigureAwait(false);

            return new RestDMChannel(Discord, new User(model.Recipients.Value[0]), model);
        }
    }
}
#endif