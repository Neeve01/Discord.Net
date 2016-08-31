using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Discord.Rest
{
    public partial class RestVoiceChannel
    {
        Task<IGuildUser> IGuildChannel.GetUserAsync(ulong id) { throw new NotSupportedException(); }
        Task<IUser> IChannel.GetUserAsync(ulong id) { throw new NotSupportedException(); }
        Task<IReadOnlyCollection<IGuildUser>> IGuildChannel.GetUsersAsync() { throw new NotSupportedException(); }
        Task<IReadOnlyCollection<IUser>> IChannel.GetUsersAsync() { throw new NotSupportedException(); }
    }
}
