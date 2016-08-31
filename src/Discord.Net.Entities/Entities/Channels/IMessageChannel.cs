#if !RPC
#if REST
namespace Discord.Rest
#elif WEBSOCKET
namespace Discord.WebSocket
#endif
{
    #if REST
        public partial interface IRestMessageChannel : IMessageChannel
    #elif WEBSOCKET
        public partial interface ISocketMessageChannel : IMessageChannel
    #endif
    {
        DiscordRestClient Discord { get; }
    }
}
#endif