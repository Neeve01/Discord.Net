#if !RPC
using Discord.API.Rest;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Model = Discord.API.Channel;
using Discord.Audio;

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
    public partial class RestVoiceChannel : RestGuildChannel, IVoiceChannel
#elif WEBSOCKET
    public partial class SocketVoiceChannel : SocketGuildChannel, IVoiceChannel
#endif
    {
        public int Bitrate { get; private set; }
        public int UserLimit { get; private set; }

#if REST
        internal RestVoiceChannel(Guild guild, Model model)
#elif WEBSOCKET
        internal SocketVoiceChannel(Guild guild, Model model)
#endif
            : base(guild, model)
        {
            Update(model);
        }

        public override void Update(Model model)
        {
            PreUpdate(model);

            base.Update(model);
            Bitrate = model.Bitrate.Value;
            UserLimit = model.UserLimit.Value;

            PostUpdate(model);
        }
        partial void PreUpdate(Model model);
        partial void PostUpdate(Model model);

        public async Task ModifyAsync(Action<ModifyVoiceChannelParams> func)
        {
            if (func == null) throw new NullReferenceException(nameof(func));

            var args = new ModifyVoiceChannelParams
            {
                Name = Name
            };
            func(args);

            var model = await Discord.ApiClient.ModifyGuildChannelAsync(Id, args).ConfigureAwait(false);
            Update(model);
        }

        public virtual Task<IAudioClient> ConnectAsync() { throw new NotSupportedException(); }

        private string DebuggerDisplay => $"{Name} ({Id}, Voice)";
    }
}
#endif