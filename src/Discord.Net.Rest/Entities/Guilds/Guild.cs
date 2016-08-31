using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Model = Discord.API.Guild;

namespace Discord.Rest
{
    public partial class RestGuild : IUpdateable
    {
        protected ImmutableDictionary<ulong, RestRole> _roles;
        
        partial void PostUpdate(Model model)
        {
            var roles = ImmutableDictionary.CreateBuilder<ulong, RestRole>();
            if (model.Roles != null)
            {
                for (int i = 0; i < model.Roles.Length; i++)
                    roles[model.Roles[i].Id] = new RestRole(this, model.Roles[i]);
            }
            _roles = roles.ToImmutable();
        }

        public async Task UpdateAsync()
        {
            var response = await Discord.ApiClient.GetGuildAsync(Id).ConfigureAwait(false);
            Update(response);
        }

        public virtual async Task<RestGuildUser> GetUserAsync(ulong id)
        {
            var model = await Discord.ApiClient.GetGuildMemberAsync(Id, id).ConfigureAwait(false);
            if (model != null)
                return new RestGuildUser(this, new RestUser(model.User), model);
            return null;
        }
        async Task<IGuildUser> IGuild.GetUserAsync(ulong id) 
            => await GetUserAsync(id).ConfigureAwait(false);
        public virtual async Task<RestGuildUser> GetCurrentUserAsync()
        {
            var currentUser = await Discord.GetCurrentUserAsync().ConfigureAwait(false);
            return await GetUserAsync(currentUser.Id).ConfigureAwait(false);
        }
        async Task<IGuildUser> IGuild.GetCurrentUserAsync() 
            => await GetCurrentUserAsync().ConfigureAwait(false);
        public virtual async Task<IReadOnlyCollection<IGuildUser>> GetUsersAsync()
        {
            var args = new GetGuildMembersParams();
            var models = await Discord.ApiClient.GetGuildMembersAsync(Id, args).ConfigureAwait(false);
            return models.Select(x => new GuildUser(this, new User(x.User), x)).ToImmutableArray();
        }
        async Task<IReadOnlyCollection<IGuildUser>> IGuild.GetUsersAsync() 
            => await GetUsersAsync().ConfigureAwait(false);

        IGuildChannel IGuild.GetCachedChannel(ulong id) => null;
        IGuildUser IGuild.GetCachedUser(ulong id) => null;
        IReadOnlyCollection<IGuildUser> IGuild.CachedUsers => ImmutableArray.Create<IGuildUser>();
    }
}
