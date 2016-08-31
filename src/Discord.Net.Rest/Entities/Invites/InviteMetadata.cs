using System;
using Model = Discord.API.InviteMetadata;

namespace Discord.Rest
{
    public class InviteMetadata : Invite, IInviteMetadata
    {
        private long _createdAtTicks;

        public bool IsRevoked { get; private set; }
        public bool IsTemporary { get; private set; }
        public int? MaxAge { get; private set; }
        public int? MaxUses { get; private set; }
        public int Uses { get; private set; }
        public RestUser Inviter { get; private set; }
        IUser IInviteMetadata.Inviter => Inviter;

        public DateTimeOffset CreatedAt => DateTimeUtils.FromTicks(_createdAtTicks);

        public InviteMetadata(IDiscordClient discord, Model model)
            : base(discord, model)
        {
            Update(model);
        }
        public void Update(Model model)
        {
            Inviter = new RestUser(model.Inviter);
            IsRevoked = model.Revoked;
            IsTemporary = model.Temporary;
            MaxAge = model.MaxAge != 0 ? model.MaxAge : (int?)null;
            MaxUses = model.MaxUses;
            Uses = model.Uses;
            _createdAtTicks = model.CreatedAt.UtcTicks;
        }
    }
}
