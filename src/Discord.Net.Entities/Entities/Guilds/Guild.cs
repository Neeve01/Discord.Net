#if !RPC
using Discord.API.Rest;
using Discord.Audio;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using EmbedModel = Discord.API.GuildEmbed;
using Model = Discord.API.Guild;
using RoleModel = Discord.API.Role;

#if REST
using DiscordClient = Discord.Rest.DiscordRestClient;
using Role = Discord.Rest.RestRole;
using User = Discord.Rest.RestUser;
using GuildChannel = Discord.Rest.RestGuildChannel;
using TextChannel = Discord.Rest.RestTextChannel;
using VoiceChannel = Discord.Rest.RestVoiceChannel;
#elif WEBSOCKET
using DiscordClient = Discord.WebSocket.DiscordSocketClient;
using Role = Discord.WebSocket.SocketRole;
using User = Discord.WebSocket.SocketUser;
using GuildChannel = Discord.WebSocket.SocketGuildChannel;
using TextChannel = Discord.WebSocket.SocketTextChannel;
using VoiceChannel = Discord.WebSocket.SocketVoiceChannel;
#endif

#if REST
namespace Discord.Rest
#elif WEBSOCKET
namespace Discord.WebSocket
#endif
{
    [DebuggerDisplay(@"{DebuggerDisplay,nq}")]
#if REST
    public partial class RestGuild : RestEntity<ulong>, ISnowflakeEntity, IGuild
#elif WEBSOCKET
    public partial class SocketGuild : SocketEntity<ulong>, ISnowflakeEntity, IGuild
#endif
    {
        protected string _iconId, _splashId;
    
        public ulong Id { get; }
        internal override DiscordClient Discord { get; }

        public string Name { get; private set; }
        public int AFKTimeout { get; private set; }
        public bool IsEmbeddable { get; private set; }
        public VerificationLevel VerificationLevel { get; private set; }
        public MfaLevel MfaLevel { get; private set; }
        public DefaultMessageNotifications DefaultMessageNotifications { get; private set; }

        public ulong? AFKChannelId { get; private set; }
        public ulong? EmbedChannelId { get; private set; }
        public ulong OwnerId { get; private set; }
        public string VoiceRegionId { get; private set; }
        public ImmutableArray<Emoji> Emojis { get; protected set; }
        public ImmutableArray<string> Features { get; protected set; }

        public ulong DefaultChannelId => Id; 
        public string IconUrl => API.CDN.GetGuildIconUrl(Id, _iconId);
        public string SplashUrl => API.CDN.GetGuildSplashUrl(Id, _splashId);

        public Role EveryoneRole => GetRole(Id);
        public IReadOnlyCollection<IRole> Roles => _roles.ToReadOnlyCollection();

        public void Update(Model model)
        {
            PreUpdate(model);

            AFKChannelId = model.AFKChannelId;
            EmbedChannelId = model.EmbedChannelId;
            AFKTimeout = model.AFKTimeout;
            IsEmbeddable = model.EmbedEnabled;
            _iconId = model.Icon;
            Name = model.Name;
            OwnerId = model.OwnerId;
            VoiceRegionId = model.Region;
            _splashId = model.Splash;
            VerificationLevel = model.VerificationLevel;
            MfaLevel = model.MfaLevel;
            DefaultMessageNotifications = model.DefaultMessageNotifications;

            if (model.Emojis != null)
            {
                var emojis = ImmutableArray.CreateBuilder<Emoji>(model.Emojis.Length);
                for (int i = 0; i < model.Emojis.Length; i++)
                    emojis.Add(new Emoji(model.Emojis[i]));
                Emojis = emojis.ToImmutableArray();
            }
            else
                Emojis = ImmutableArray.Create<Emoji>();

            if (model.Features != null)
                Features = model.Features.ToImmutableArray();
            else
                Features = ImmutableArray.Create<string>();

            PostUpdate(model);
        }
        partial void PreUpdate(Model model);
        partial void PostUpdate(Model model);

        public RestGuild(DiscordClient discord, Model model)
        {
            Id = model.Id;
            Discord = discord;

            Update(model);
        }

        public void Update(EmbedModel model)
        {
            PreUpdate(model);

            IsEmbeddable = model.Enabled;
            EmbedChannelId = model.ChannelId;

            PostUpdate(model);
        }
        partial void PreUpdate(EmbedModel model);
        partial void PostUpdate(EmbedModel model);

        public void Update(IEnumerable<RoleModel> models)
        {
            Role role;
            foreach (var model in models)
            {
                if (_roles.TryGetValue(model.Id, out role))
                    role.Update(model);
            }
        }

        public async Task ModifyAsync(Action<ModifyGuildParams> func)
        {
            if (func == null) throw new NullReferenceException(nameof(func));

            var args = new ModifyGuildParams
            {
                SplashId = _splashId,
                IconId = _iconId
            };

            func(args);

            var model = await Discord.ApiClient.ModifyGuildAsync(Id, args).ConfigureAwait(false);
            Update(model);
        }
        public async Task ModifyEmbedAsync(Action<ModifyGuildEmbedParams> func)
        { 
            if (func == null) throw new NullReferenceException(nameof(func));

            var args = new ModifyGuildEmbedParams();
            func(args);
            var model = await Discord.ApiClient.ModifyGuildEmbedAsync(Id, args).ConfigureAwait(false);
            Update(model);
        }
        public async Task ModifyChannelsAsync(IEnumerable<ModifyGuildChannelsParams> args)
        {
            await Discord.ApiClient.ModifyGuildChannelsAsync(Id, args).ConfigureAwait(false);
        }
        public async Task ModifyRolesAsync(IEnumerable<ModifyGuildRolesParams> args)
        {            
            var models = await Discord.ApiClient.ModifyGuildRolesAsync(Id, args).ConfigureAwait(false);
            Update(models);
        }
        public async Task LeaveAsync()
        {
            await Discord.ApiClient.LeaveGuildAsync(Id).ConfigureAwait(false);
        }
        public async Task DeleteAsync()
        {
            await Discord.ApiClient.DeleteGuildAsync(Id).ConfigureAwait(false);
        }
        
        public async Task<IReadOnlyCollection<Ban>> GetBansAsync()
        {
            var models = await Discord.ApiClient.GetGuildBansAsync(Id).ConfigureAwait(false);
            return models.Select(x => new Ban(new User(x.User), x.Reason)).ToImmutableArray();
        }
        public Task AddBanAsync(IUser user, int pruneDays = 0) => AddBanAsync(user.Id, pruneDays);
        public async Task AddBanAsync(ulong userId, int pruneDays = 0)
        {
            var args = new CreateGuildBanParams() { DeleteMessageDays = pruneDays };
            await Discord.ApiClient.CreateGuildBanAsync(Id, userId, args).ConfigureAwait(false);
        }
        public Task RemoveBanAsync(IUser user) => RemoveBanAsync(user.Id);
        public async Task RemoveBanAsync(ulong userId)
        {
            await Discord.ApiClient.RemoveGuildBanAsync(Id, userId).ConfigureAwait(false);
        }
        
        public virtual async Task<IGuildChannel> GetChannelAsync(ulong id)
        {
            var model = await Discord.ApiClient.GetChannelAsync(Id, id).ConfigureAwait(false);
            if (model != null)
                return ToChannel(model);
            return null;
        }
        public virtual async Task<IReadOnlyCollection<IGuildChannel>> GetChannelsAsync()
        {
            var models = await Discord.ApiClient.GetGuildChannelsAsync(Id).ConfigureAwait(false);
            return models.Select(x => ToChannel(x)).ToImmutableArray();
        }
        public async Task<ITextChannel> CreateTextChannelAsync(string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            var args = new CreateGuildChannelParams() { Name = name, Type = ChannelType.Text };
            var model = await Discord.ApiClient.CreateGuildChannelAsync(Id, args).ConfigureAwait(false);
            return new TextChannel(this, model);
        }
        public async Task<IVoiceChannel> CreateVoiceChannelAsync(string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            var args = new CreateGuildChannelParams { Name = name, Type = ChannelType.Voice };
            var model = await Discord.ApiClient.CreateGuildChannelAsync(Id, args).ConfigureAwait(false);
            return new VoiceChannel(this, model);
        }
        
        public async Task<IReadOnlyCollection<IGuildIntegration>> GetIntegrationsAsync()
        {
            var models = await Discord.ApiClient.GetGuildIntegrationsAsync(Id).ConfigureAwait(false);
            return models.Select(x => new GuildIntegration(Id, x)).ToImmutableArray();
        }
        public async Task<IGuildIntegration> CreateIntegrationAsync(ulong id, string type)
        {
            var args = new CreateGuildIntegrationParams { Id = id, Type = type };
            var model = await Discord.ApiClient.CreateGuildIntegrationAsync(Id, args).ConfigureAwait(false);
            return new GuildIntegration(Id, model);
        }
        
        public async Task<IReadOnlyCollection<IInviteMetadata>> GetInvitesAsync()
        {
            var models = await Discord.ApiClient.GetGuildInvitesAsync(Id).ConfigureAwait(false);
            return models.Select(x => new InviteMetadata(Discord, x)).ToImmutableArray();
        }
        
        public Role GetRole(ulong id)
        {
            Role result = null;
            if (_roles?.TryGetValue(id, out result) == true)
                return result;
            return null;
        }        
        public async Task<IRole> CreateRoleAsync(string name, GuildPermissions? permissions = null, Color? color = null, bool isHoisted = false)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            
            var model = await Discord.ApiClient.CreateGuildRoleAsync(Id).ConfigureAwait(false);
            var role = new Role(this, model);

            await role.ModifyAsync(x =>
            {
                x.Name = name;
                x.Permissions = (permissions ?? role.Permissions).RawValue;
                x.Color = (color ?? Color.Default).RawValue;
                x.Hoist = isHoisted;
            }).ConfigureAwait(false);

            return role;
        }

        public async Task<int> PruneUsersAsync(int days = 30, bool simulate = false)
        {
            var args = new GuildPruneParams() { Days = days };
            GetGuildPruneCountResponse model;
            if (simulate)
                model = await Discord.ApiClient.GetGuildPruneCountAsync(Id, args).ConfigureAwait(false);
            else
                model = await Discord.ApiClient.BeginGuildPruneAsync(Id, args).ConfigureAwait(false);
            return model.Pruned;
        }
        public virtual Task DownloadUsersAsync()
        {
            throw new NotSupportedException();
        }

        internal GuildChannel ToChannel(API.Channel model)
        {
            switch (model.Type)
            {
                case ChannelType.Text:
                    return new TextChannel(this, model);
                case ChannelType.Voice:
                    return new VoiceChannel(this, model);
                default:
                    throw new InvalidOperationException($"Unexpected channel type: {model.Type}");
            }
        }

        public override string ToString() => Name;

        private string DebuggerDisplay => $"{Name} ({Id})";

        bool IGuild.Available => false;
        IRole IGuild.EveryoneRole => EveryoneRole;
        IReadOnlyCollection<Emoji> IGuild.Emojis => Emojis;
        IReadOnlyCollection<string> IGuild.Features => Features;
        IAudioClient IGuild.AudioClient => null;

        IRole IGuild.GetRole(ulong id) => GetRole(id);
    }
}
#endif