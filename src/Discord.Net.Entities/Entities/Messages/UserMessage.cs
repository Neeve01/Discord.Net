#if !RPC
using Discord.API.Rest;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading.Tasks;
using Model = Discord.API.Message;

#if REST
using Role = Discord.Rest.RestRole;
using User = Discord.Rest.RestUser;
using GuildChannel = Discord.Rest.RestGuildChannel;
using IMessageChannel = Discord.Rest.IRestMessageChannel;
#elif WEBSOCKET
using Role = Discord.WebSocket.SocketRole;
using User = Discord.WebSocket.SocketUser;
using GuildChannel = Discord.WebSocket.SocketGuildChannel;
using IMessageChannel = Discord.Socket.ISocketMessageChannel;
#endif

#if REST
namespace Discord.Rest
#elif WEBSOCKET
namespace Discord.WebSocket
#endif
{
    [DebuggerDisplay(@"{DebuggerDisplay,nq}")]
#if REST
    public partial class RestUserMessage : RestMessage, IUserMessage
#elif WEBSOCKET
    public partial class SocketUserMessage : SocketMessage, IUserMessage
#endif
    {
        private bool _isMentioningEveryone, _isTTS, _isPinned;
        private long? _editedTimestampTicks;
        private IReadOnlyCollection<Attachment> _attachments;
        private IReadOnlyCollection<Embed> _embeds;
        private IReadOnlyCollection<ulong> _mentionedChannelIds;
        private IReadOnlyCollection<Role> _mentionedRoles;
        private IReadOnlyCollection<User> _mentionedUsers;

        public override bool IsTTS => _isTTS;
        public override bool IsPinned => _isPinned;
        public override DateTimeOffset? EditedTimestamp => DateTimeUtils.FromTicks(_editedTimestampTicks);

        public override IReadOnlyCollection<Attachment> Attachments => _attachments;
        public override IReadOnlyCollection<Embed> Embeds => _embeds;
        public override IReadOnlyCollection<ulong> MentionedChannelIds => _mentionedChannelIds;
        public override IReadOnlyCollection<Role> MentionedRoles => _mentionedRoles;
        public override IReadOnlyCollection<User> MentionedUsers => _mentionedUsers;

#if REST
        public RestUserMessage(IMessageChannel channel, User author, Model model)
#elif WEBSOCKET
        public SocketUserMessage(IMessageChannel channel, User author, Model model)
#endif
            : base(channel, author, model)
        {
            _mentionedChannelIds = ImmutableArray.Create<ulong>();
            _mentionedRoles = ImmutableArray.Create<Role>();
            _mentionedUsers = ImmutableArray.Create<User>();

            Update(model);
        }
        public override void Update(Model model)
        {
            var guildChannel = Channel as GuildChannel;
            var guild = guildChannel?.Guild;

            if (model.IsTextToSpeech.IsSpecified)
                _isTTS = model.IsTextToSpeech.Value;
            if (model.Pinned.IsSpecified)
                _isPinned = model.Pinned.Value;
            if (model.EditedTimestamp.IsSpecified)
                _editedTimestampTicks = model.EditedTimestamp.Value?.UtcTicks;
            if (model.MentionEveryone.IsSpecified)
                _isMentioningEveryone = model.MentionEveryone.Value;

            if (model.Attachments.IsSpecified)
            {
                var value = model.Attachments.Value;
                if (value.Length > 0)
                {
                    var attachments = new Attachment[value.Length];
                    for (int i = 0; i < attachments.Length; i++)
                        attachments[i] = new Attachment(value[i]);
                    _attachments = ImmutableArray.Create(attachments);
                }
                else
                    _attachments = ImmutableArray.Create<Attachment>();
            }

            if (model.Embeds.IsSpecified)
            {
                var value = model.Embeds.Value;
                if (value.Length > 0)
                {
                    var embeds = new Embed[value.Length];
                    for (int i = 0; i < embeds.Length; i++)
                        embeds[i] = new Embed(value[i]);
                    _embeds = ImmutableArray.Create(embeds);
                }
                else
                    _embeds = ImmutableArray.Create<Embed>();
            }

            ImmutableArray<IUser> mentions = ImmutableArray.Create<IUser>();
            if (model.Mentions.IsSpecified)
            {
                var value = model.Mentions.Value;
                if (value.Length > 0)
                {
                    var newMentions = new IUser[value.Length];
                    for (int i = 0; i < value.Length; i++)
                        newMentions[i] = new User(value[i]);
                    mentions = ImmutableArray.Create(newMentions);
                }
            }

            if (model.Content.IsSpecified)
            {
                var text = model.Content.Value;
                
                if (guildChannel != null)
                {
                    _mentionedUsers = MentionsHelper.GetUserMentions(text, Channel, mentions);
                    _mentionedChannelIds = MentionsHelper.GetChannelMentions(text, guildChannel.Guild);
                    _mentionedRoles = MentionsHelper.GetRoleMentions(text, guildChannel.Guild);
                }
                model.Content = text;
            }

            base.Update(model);

            PostUpdate(model);
        }
        partial void PreUpdate(Model model);
        partial void PostUpdate(Model model);

        public async Task ModifyAsync(Action<ModifyMessageParams> func)
        {
            if (func == null) throw new NullReferenceException(nameof(func));

            var args = new ModifyMessageParams();
            func(args);
            var guildChannel = Channel as GuildChannel;

            Model model;
            if (guildChannel != null)
                model = await Discord.ApiClient.ModifyMessageAsync(guildChannel.Guild.Id, Channel.Id, Id, args).ConfigureAwait(false);
            else
                model = await Discord.ApiClient.ModifyDMMessageAsync(Channel.Id, Id, args).ConfigureAwait(false);
                
            Update(model);
        }        
        public async Task DeleteAsync()
        {
            var guildChannel = Channel as GuildChannel;
            if (guildChannel != null)
                await Discord.ApiClient.DeleteMessageAsync(guildChannel.Id, Channel.Id, Id).ConfigureAwait(false);
            else
                await Discord.ApiClient.DeleteDMMessageAsync(Channel.Id, Id).ConfigureAwait(false);
        }
        public async Task PinAsync()
        {
            await Discord.ApiClient.AddPinAsync(Channel.Id, Id).ConfigureAwait(false);
        }
        public async Task UnpinAsync()
        {
            await Discord.ApiClient.RemovePinAsync(Channel.Id, Id).ConfigureAwait(false);
        }
        
        public string Resolve(int startIndex, int length, UserMentionHandling userHandling, ChannelMentionHandling channelHandling,
            RoleMentionHandling roleHandling, EveryoneMentionHandling everyoneHandling)
            => Resolve(Content.Substring(startIndex, length), userHandling, channelHandling, roleHandling, everyoneHandling);
        public string Resolve(UserMentionHandling userHandling, ChannelMentionHandling channelHandling, 
            RoleMentionHandling roleHandling, EveryoneMentionHandling everyoneHandling)
            => Resolve(Content, userHandling, channelHandling, roleHandling, everyoneHandling);
        
        private string Resolve(string text, UserMentionHandling userHandling, ChannelMentionHandling channelHandling,
            RoleMentionHandling roleHandling, EveryoneMentionHandling everyoneHandling)
        {
            text = MentionsHelper.ResolveUserMentions(text, Channel, MentionedUsers, userHandling);
            text = MentionsHelper.ResolveChannelMentions(text, (Channel as IGuildChannel)?.Guild, channelHandling);
            text = MentionsHelper.ResolveRoleMentions(text, MentionedRoles, roleHandling);
            text = MentionsHelper.ResolveEveryoneMentions(text, everyoneHandling);
            return text;
        }

        public override string ToString() => Content;
        private string DebuggerDisplay => $"{Author}: {Content}{(Attachments.Count > 0 ? $" [{Attachments.Count} Attachments]" : "")}";
    }
}
#endif