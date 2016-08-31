using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
namespace Discord.Rest
{
    public partial class RestGroupChannel
    {
        IReadOnlyCollection<IMessage> IMessageChannel.CachedMessages => ImmutableArray.Create<RestMessage>();
        IMessage IMessageChannel.GetCachedMessage(ulong id) => null;
    }
}
