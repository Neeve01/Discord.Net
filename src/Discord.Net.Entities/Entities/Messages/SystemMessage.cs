#if !RPC
using System.Diagnostics;
using Model = Discord.API.Message;

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
    public partial class RestSystemMessage : RestMessage, ISystemMessage
#elif WEBSOCKET
    public partial class SocketSystemMessage : SocketMessage, ISystemMessage
#endif
    {        
        public MessageType Type { get; }

        public RestSystemMessage(IMessageChannel channel, User author, Model model)
            : base(channel, author, model)
        {
            Type = model.Type;
        }

        public override string ToString() => Content;
        private string DebuggerDisplay => $"[{Type}] {Author}{(!string.IsNullOrEmpty(Content) ? $": ({Content})" : "")}";
    }
}
#endif