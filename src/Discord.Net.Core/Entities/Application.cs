using Discord.API;
using System;
using System.Threading.Tasks;
using Model = Discord.API.Application;

namespace Discord
{
    public class Application : ISnowflakeEntity, IApplication
    {
        protected string _iconId;
        private readonly DiscordRestApiClient _discord;

        public ulong Id { get; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public string[] RPCOrigins { get; private set; }
        public ulong Flags { get; private set; }

        public IUser Owner { get; private set; }

        public string IconUrl => API.CDN.GetApplicationIconUrl(Id, _iconId);

        public Application(DiscordRestApiClient discord, Model model)
        {
            _discord = discord;
            Id = model.Id;

            Update(model);
        }

        internal void Update(Model model)
        {            
            Description = model.Description;
            RPCOrigins = model.RPCOrigins;
            Name = model.Name;
            Flags = model.Flags;
            Owner = new RestUser(model.Owner);
            _iconId = model.Icon;
        }

        public async Task UpdateAsync()
        {
            var response = await _discord.GetMyApplicationAsync().ConfigureAwait(false);
            if (response.Id != Id)
                throw new InvalidOperationException("Unable to update this object from a different application token.");
            Update(response);
        }
    }
}
