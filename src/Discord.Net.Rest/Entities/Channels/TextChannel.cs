using System.Collections.Generic;
using System.Collections.Immutable;

namespace Discord.Rest
{
    public partial class RestTextChannel
    {
        IReadOnlyCollection<IMessage> IMessageChannel.CachedMessages => ImmutableArray.Create<RestMessage>();

        IMessage IMessageChannel.GetCachedMessage(ulong id)
            => null;
    }
}
