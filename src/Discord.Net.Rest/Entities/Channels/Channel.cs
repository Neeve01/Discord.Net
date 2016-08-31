using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
namespace Discord.Rest
{
    public partial class RestChannel : IUpdateable
    {
        public async Task UpdateAsync()
        {
            var model = await Discord.ApiClient.GetChannelAsync(Id).ConfigureAwait(false);
            Update(model);
        }

        Task<IUser> IChannel.GetUserAsync(ulong id) => Task.FromResult<IUser>(null);
        Task<IReadOnlyCollection<IUser>> IChannel.GetUsersAsync() => Task.FromResult<IReadOnlyCollection<IUser>>(ImmutableArray.Create<IUser>());

        IUser IChannel.GetCachedUser(ulong id) => null;
        IReadOnlyCollection<IUser> IChannel.CachedUsers => ImmutableArray.Create<IUser>();
    }
}
