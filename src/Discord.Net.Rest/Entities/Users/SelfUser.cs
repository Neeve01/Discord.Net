using System.Threading.Tasks;

namespace Discord.Rest
{
    public partial class RestSelfUser
    {
        public async Task UpdateAsync()
        {
            var model = await Discord.ApiClient.GetMyUserAsync().ConfigureAwait(false);
            Update(model);
        }
    }
}
