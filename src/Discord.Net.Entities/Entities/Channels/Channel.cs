#if !RPC
using Model = Discord.API.Channel;

#if REST
using DiscordClient = Discord.Rest.DiscordRestClient;
#elif WEBSOCKET
using DiscordClient = Discord.WebSockets.DiscordSocketClient;
#endif

#if REST
namespace Discord.Rest
#elif WEBSOCKET
namespace Discord.WebSocket
#endif
{
#if REST
    public abstract partial class RestChannel : ISnowflakeEntity, IChannel
#elif WEBSOCKET
    public abstract partial class SocketChannel : ISnowflakeEntity, IChannel
#endif
    {
        public abstract ulong Id { get; }
        internal virtual DiscordClient Discord { get; }

        internal abstract void Update(Model model);
    }
}
#endif