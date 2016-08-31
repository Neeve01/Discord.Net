#if !RPC
using Discord.API.Rest;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Model = Discord.API.Role;

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
    public partial class RestRole : RestEntity<ulong>, IRole
#elif WEBSOCKET
    public partial class SocketRole : SocketEntity<ulong>, IRole
#endif
    {
        public ulong Id { get; }
        public Guild Guild { get; }
        
        public Color Color { get; private set; }
        public bool IsHoisted { get; private set; }
        public bool IsManaged { get; private set; }
        public string Name { get; private set; }
        public GuildPermissions Permissions { get; private set; }
        public int Position { get; private set; }
        
        public bool IsEveryone => Id == Guild.Id;
        public string Mention => MentionsHelper.MentionRole(Id);
        internal override DiscordClient Discord => Guild.Discord;

#if REST
        public RestRole(Guild guild, Model model)
#elif WEBSOCKET
        public SocketRole(Guild guild, Model model)
#endif
        {
            Id = model.Id;
            Guild = guild;

            Update(model);
        }
        public void Update(Model model)
        {
            Name = model.Name;
            IsHoisted = model.Hoist;
            IsManaged = model.Managed;
            Position = model.Position;
            Color = new Color(model.Color);
            Permissions = new GuildPermissions(model.Permissions);
        }

        public async Task ModifyAsync(Action<ModifyGuildRoleParams> func)
        {
            if (func == null) throw new NullReferenceException(nameof(func));

            var args = new ModifyGuildRoleParams();
            func(args);
            var response = await Discord.ApiClient.ModifyGuildRoleAsync(Guild.Id, Id, args).ConfigureAwait(false);

            Update(response);
        }
        public async Task DeleteAsync()
        {
            await Discord.ApiClient.DeleteGuildRoleAsync(Guild.Id, Id).ConfigureAwait(false);
        }

        public Role Clone() => MemberwiseClone() as Role;
        
        public override string ToString() => Name;
        private string DebuggerDisplay => $"{Name} ({Id})";

        ulong IRole.GuildId => Guild.Id;
    }
}
#endif