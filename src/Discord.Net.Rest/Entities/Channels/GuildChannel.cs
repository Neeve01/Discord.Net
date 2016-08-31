using System.Collections.Generic;
using System.Collections.Immutable;
using Model = Discord.API.Channel;

namespace Discord.Rest
{
    public partial class RestGuildChannel
    {
        private ImmutableArray<Overwrite> _overwrites;

        partial void PostUpdate(Model model)
        {
            if (model.PermissionOverwrites.IsSpecified)
            {
                var overwrites = model.PermissionOverwrites.Value;
                var newOverwrites = ImmutableArray.CreateBuilder<Overwrite>(overwrites.Length);
                for (int i = 0; i < overwrites.Length; i++)
                    newOverwrites.Add(new Overwrite(overwrites[i]));
                _overwrites = newOverwrites.ToImmutable();
            }
        }

        IReadOnlyCollection<IUser> IChannel.CachedUsers => ImmutableArray.Create<IUser>();
        IReadOnlyCollection<IGuildUser> IGuildChannel.CachedUsers => ImmutableArray.Create<IGuildUser>();
        IUser IChannel.GetCachedUser(ulong id) => null;
        IGuildUser IGuildChannel.GetCachedUser(ulong id) => null;
    }
}
