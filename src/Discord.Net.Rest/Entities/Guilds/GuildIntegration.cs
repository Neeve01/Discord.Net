using Discord.API.Rest;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Model = Discord.API.Integration;

namespace Discord.Rest
{
    [DebuggerDisplay(@"{DebuggerDisplay,nq}")]
    public class RestGuildIntegration : IEntity<ulong>, IGuildIntegration
    {
        private long _syncedAtTicks;

        public ulong Id { get; }
        internal IDiscordClient Discord { get; }

        public string Name { get; private set; }
        public string Type { get; private set; }
        public bool IsEnabled { get; private set; }
        public bool IsSyncing { get; private set; }
        public ulong ExpireBehavior { get; private set; }
        public ulong ExpireGracePeriod { get; private set; }

        public ulong GuildId { get; private set; }
        public ulong RoleId { get; private set; }
        public RestUser User { get; private set; }
        public IntegrationAccount Account { get; private set; }

        public DateTimeOffset SyncedAt => DateTimeUtils.FromTicks(_syncedAtTicks);

        public RestGuildIntegration(IDiscordClient discord, ulong guildId, Model model)
        {
            Discord = discord;
            Id = model.Id;
            GuildId = guildId;

            Update(model);
        }

        public void Update(Model model)
        {
            Name = model.Name;
            Type = model.Type;
            IsEnabled = model.Enabled;
            IsSyncing = model.Syncing;
            ExpireBehavior = model.ExpireBehavior;
            ExpireGracePeriod = model.ExpireGracePeriod;
            _syncedAtTicks = model.SyncedAt.UtcTicks;

            RoleId = model.RoleId;
            User = new RestUser(model.User);
        }
        
        public async Task DeleteAsync()
        {
            await Discord.ApiClient.DeleteGuildIntegrationAsync(Guild.Id, Id).ConfigureAwait(false);
        }
        public async Task ModifyAsync(Action<ModifyGuildIntegrationParams> func)
        {
            if (func == null) throw new NullReferenceException(nameof(func));

            var args = new ModifyGuildIntegrationParams();
            func(args);
            var model = await Discord.ApiClient.ModifyGuildIntegrationAsync(Guild.Id, Id, args).ConfigureAwait(false);

            Update(model);
        }
        public async Task SyncAsync()
        {
            await Discord.ApiClient.SyncGuildIntegrationAsync(Guild.Id, Id).ConfigureAwait(false);
        }

        public override string ToString() => Name;
        private string DebuggerDisplay => $"{Name} ({Id}{(IsEnabled ? ", Enabled" : "")})";

        IUser IGuildIntegration.User => User;
    }
}
