using System.Diagnostics;
using System.Threading.Tasks;
using Model = Discord.API.Invite;

namespace Discord.Rest
{
    [DebuggerDisplay(@"{DebuggerDisplay,nq}")]
    public class Invite : IEntity<string>, IInvite
    {
        private readonly IDiscordClient _discord;

        public string Code { get; }
        public string ChannelName { get; private set; }
        public string GuildName { get; private set; }

        public ulong ChannelId { get; private set; }
        public ulong GuildId { get; private set; }

        public string Url => $"{DiscordConfig.InviteUrl}/{Code}";

        public Invite(IDiscordClient discord, Model model)
        {
            _discord = discord;
            Code = model.Code;

            Update(model);
        }
        public void Update(Model model)
        {
            GuildId = model.Guild.Id;
            ChannelId = model.Channel.Id;
            GuildName = model.Guild.Name;
            ChannelName = model.Channel.Name;
        }

        public async Task AcceptAsync()
        {
            await _discord.ApiClient.AcceptInviteAsync(Code).ConfigureAwait(false);
        }
        public async Task DeleteAsync()
        {
            await _discord.ApiClient.DeleteInviteAsync(Code).ConfigureAwait(false);
        }

        public override string ToString() => Url;
        private string DebuggerDisplay => $"{Url} ({GuildName} / {ChannelName})";

        string IEntity<string>.Id => Code;
    }
}
