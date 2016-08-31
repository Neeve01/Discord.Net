using System.Diagnostics;
using System.Threading.Tasks;
using Model = Discord.API.UserGuild;

namespace Discord.Rest
{
    [DebuggerDisplay(@"{DebuggerDisplay,nq}")]
    public class RestUserGuild : ISnowflakeEntity, IUserGuild
    {
        private string _iconId;
                
        public ulong Id { get; }
        public string Name { get; private set; }
        public bool IsOwner { get; private set; }
        public GuildPermissions Permissions { get; private set; }

        public DiscordRestClient Discord { get; }

        public string IconUrl => API.CDN.GetGuildIconUrl(Id, _iconId);

        public RestUserGuild(DiscordRestClient discord, Model model)
        {
            Discord = discord;
            Id = model.Id;

            Update(model);
        }
        public void Update(Model model)
        {
            _iconId = model.Icon;
            IsOwner = model.Owner;
            Name = model.Name;
            Permissions = new GuildPermissions(model.Permissions);
        }
        
        public async Task LeaveAsync()
        {
            await Discord.ApiClient.LeaveGuildAsync(Id).ConfigureAwait(false);
        }
        public async Task DeleteAsync()
        {
            await Discord.ApiClient.DeleteGuildAsync(Id).ConfigureAwait(false);
        }

        public override string ToString() => Name;
        private string DebuggerDisplay => $"{Name} ({Id}{(IsOwner ? ", Owned" : "")})";
    }
}
