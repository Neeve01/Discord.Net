using System.Collections.Immutable;
using System.Threading.Tasks;
using Model = Discord.API.GuildMember;
using PresenceModel = Discord.API.Presence;

namespace Discord.Rest
{
    public partial class RestGuildUser
    {
        public ImmutableDictionary<ulong, RestRole> Roles { get; private set; }

        public async Task UpdateAsync()
        {
            var model = await Discord.ApiClient.GetGuildMemberAsync(Guild.Id, Id).ConfigureAwait(false);
            Update(model);
        }

        partial void PreUpdate(Model model)
        {
            UpdateRoles(model.Roles);
        }
        partial void PreUpdate(PresenceModel model)
        {
            if (model.Roles.IsSpecified)
                UpdateRoles(model.Roles.Value);
        }

        private void UpdateRoles(ulong[] roleIds)
        {
            var roles = ImmutableDictionary.CreateBuilder<ulong, RestRole>(roleIds.Length + 1);
            roles.Add(Guild.EveryoneRole);
            for (int i = 0; i < roleIds.Length; i++)
            {
                var role = Guild.GetRole(roleIds[i]);
                if (role != null)
                    roles.Add(role);
            }
            Roles = roles.ToImmutable();
        }
    }
}
