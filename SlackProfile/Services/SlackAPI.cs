using SlackProfile.Helpers;
using SlackProfile.Items.GetUserProfile.Response;
using SlackProfile.Items.SetUsersProfile.Request;
using SlackProfile.Items.SetUsersProfile.Response;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace SlackProfile.Services
{
    public class SlackAPI
    {
        private readonly HttpClient client;

        public SlackAPI(string token)
        {
            this.client = new HttpClient();
            this.client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        /// <summary>
        /// 프로필 가져오기 <para/>
        /// https://api.slack.com/methods/users.profile.get
        /// </summary>
        /// <param name="profile"></param>
        /// <returns></returns>
        public async Task<GetUserProfileResponse> GetUsersProfileAsync()
        {
            var response = await client.GetAsync("https://slack.com/api/users.profile.get");
            var getUserProfileResponse = await response.Content.ReadFromJsonAsync<GetUserProfileResponse>();

            return getUserProfileResponse;
        }

        /// <summary>
        /// 프로필 변경하기 <para/>
        /// https://api.slack.com/methods/users.profile.set
        /// </summary>
        /// <param name="profile"></param>
        /// <returns></returns>
        public async Task<SetUserProfileResponse> SetUsersProfileAsync(Items.SetUsersProfile.Request.Profile profile)
        {
            var data = new SetUserProfileRequest(profile).ToJsonContent();
            var response = await client.PostAsync("https://slack.com/api/users.profile.set", data);
            var setUserProfileResponse = await response.Content.ReadFromJsonAsync<SetUserProfileResponse>();

            return setUserProfileResponse;
        }
    }
}
