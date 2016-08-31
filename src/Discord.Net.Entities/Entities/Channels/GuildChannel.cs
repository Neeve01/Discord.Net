#if !RPC
using Discord.API.Rest;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Model = Discord.API.Channel;

#if REST
using DiscordClient = Discord.Rest.DiscordRestClient;
using Guild = Discord.Rest.RestGuild;
using Role = Discord.Rest.RestRole;
using User = Discord.Rest.RestUser;
#elif WEBSOCKET
using DiscordClient = Discord.WebSocket.DiscordSocketClient;
using Guild = Discord.WebSocket.SocketGuild;
using Role = Discord.WebSocket.SocketRole;
using User = Discord.WebSocket.SocketUser;
#endif

#if REST
namespace Discord.Rest
#elif WEBSOCKET
namespace Discord.WebSocket
#endif
{
    [DebuggerDisplay(@"{DebuggerDisplay,nq}")]
#if REST
    public abstract partial class RestGuildChannel : RestEntity<ulong>, ISnowflakeEntity, IGuildChannel
#elif WEBSOCKET
    public abstract partial class SocketGuildChannel : SocketEntity<ulong>, ISnowflakeEntity, IGuildChannel
#endif
    {
        public ulong Id { get; }
        public string Name { get; private set; }
        public int Position { get; private set; }

        public Guild Guild { get; }

        internal override DiscordClient Discord => Guild.Discord;

        internal RestGuildChannel(Guild guild, Model model)
        {
            Guild = guild;
            Id = model.Id;
        }

        public virtual void Update(Model model)
        {
            PreUpdate(model);

            Name = model.Name.Value;
            Position = model.Position.Value;

            PostUpdate(model);
        }
        partial void PreUpdate(Model model);
        partial void PostUpdate(Model model);

        public async Task ModifyAsync(Action<ModifyGuildChannelParams> func)
        {
            if (func == null) throw new NullReferenceException(nameof(func));

            var args = new ModifyGuildChannelParams
            {
                Name = Name
            };
            func(args);

            var model = await Discord.ApiClient.ModifyGuildChannelAsync(Id, args).ConfigureAwait(false);
            Update(model);
        }
        public async Task DeleteAsync()
        {
            await Discord.ApiClient.DeleteChannelAsync(Id).ConfigureAwait(false);
        }

        public abstract Task<RestGuildUser> GetUserAsync(ulong id);
        public abstract Task<IImmutableList<RestGuildUser>> GetUsersAsync();
        public async Task<IReadOnlyCollection<IInviteMetadata>> GetInvitesAsync()
        {
            var models = await Discord.ApiClient.GetChannelInvitesAsync(Id).ConfigureAwait(false);
            return models.Select(x => new InviteMetadata(Discord, x)).ToImmutableArray();
        }
        public async Task<IInviteMetadata> CreateInviteAsync(int? maxAge, int? maxUses, bool isTemporary)
        {
            var args = new CreateChannelInviteParams
            {
                MaxAge = maxAge ?? 0,
                MaxUses = maxUses ?? 0,
                Temporary = isTemporary
            };
            var model = await Discord.ApiClient.CreateChannelInviteAsync(Id, args).ConfigureAwait(false);
            return new InviteMetadata(Discord, model);
        }

        public OverwritePermissions? GetPermissionOverwrite(IUser user)
        {
            for (int i = 0; i < _overwrites.Length; i++)
            {
                if (_overwrites[i].TargetId == user.Id)
                    return _overwrites[i].Permissions;
            }
            return null;
        }
        public OverwritePermissions? GetPermissionOverwrite(IRole role)
        {
            for (int i = 0; i < _overwrites.Length; i++)
            {
                if (_overwrites[i].TargetId == role.Id)
                    return _overwrites[i].Permissions;
            }
            return null;
        }
        
        public async Task AddPermissionOverwriteAsync(IUser user, OverwritePermissions perms)
        {
            var args = new ModifyChannelPermissionsParams { Allow = perms.AllowValue, Deny = perms.DenyValue, Type = "member" };
            await Discord.ApiClient.ModifyChannelPermissionsAsync(Id, user.Id, args).ConfigureAwait(false);
            _overwrites.Add(new Overwrite(new API.Overwrite { Allow = perms.AllowValue, Deny = perms.DenyValue, TargetId = user.Id, TargetType = PermissionTarget.User }));
        }
        public async Task AddPermissionOverwriteAsync(IRole role, OverwritePermissions perms)
        {
            var args = new ModifyChannelPermissionsParams { Allow = perms.AllowValue, Deny = perms.DenyValue, Type = "role" };
            await Discord.ApiClient.ModifyChannelPermissionsAsync(Id, role.Id, args).ConfigureAwait(false);
            _overwrites.Add(new Overwrite(new API.Overwrite { Allow = perms.AllowValue, Deny = perms.DenyValue, TargetId = role.Id, TargetType = PermissionTarget.Role }));
        }
        public async Task RemovePermissionOverwriteAsync(IUser user)
        {
            await Discord.ApiClient.DeleteChannelPermissionAsync(Id, user.Id).ConfigureAwait(false);

            for (int i = 0; i < _overwrites.Length; i++)
            {
                if (_overwrites[i].TargetId == user.Id)
                {
                    _overwrites.RemoveAt(i);
                    return;
                }
            }
        }
        public async Task RemovePermissionOverwriteAsync(IRole role)
        {
            await Discord.ApiClient.DeleteChannelPermissionAsync(Id, role.Id).ConfigureAwait(false);

            for (int i = 0; i < _overwrites.Length; i++)
            {
                if (_overwrites[i].TargetId == role.Id)
                {
                    _overwrites.RemoveAt(i);
                    return;
                }
            }
        }

        public override string ToString() => Name;
        private string DebuggerDisplay => $"{Name} ({Id})";

        //Interfaces
        IGuild IGuildChannel.Guild => Guild;
        IReadOnlyCollection<Overwrite> IGuildChannel.PermissionOverwrites => _overwrites;

        async Task<IUser> IChannel.GetUserAsync(ulong id) 
            => await GetUserAsync(id).ConfigureAwait(false);
        async Task<IGuildUser> IGuildChannel.GetUserAsync(ulong id) 
            => await GetUserAsync(id).ConfigureAwait(false);
        async Task<IReadOnlyCollection<IUser>> IChannel.GetUsersAsync() 
            => await GetUsersAsync().ConfigureAwait(false);
        async Task<IReadOnlyCollection<IGuildUser>> IGuildChannel.GetUsersAsync() 
            => await GetUsersAsync().ConfigureAwait(false);
    }
}
#endif