using System;

namespace Discord.Rest
{
    public abstract partial class RestEntity<TId>
        where TId : IEquatable<TId>
    {
        internal abstract DiscordRestClient Discord { get; }
    }
}
