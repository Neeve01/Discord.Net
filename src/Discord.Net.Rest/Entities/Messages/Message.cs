using System.Threading.Tasks;

namespace Discord.Rest
{
    public partial class RestMessage : IUpdateable
    {
        public async Task UpdateAsync()
        {
            var model = await Discord.ApiClient.GetChannelMessageAsync(Channel.Id, Id).ConfigureAwait(false);
            Update(model);
        }
    }
}
