using System.Text.Json.Serialization;

namespace SlackProfile.Items.SetUsersProfile.Request
{
    /// <summary>
    /// 
    /// </summary>
    public class SetUserProfileRequest
    {
        public SetUserProfileRequest(Profile profile)
        {
            this.Profile = profile;
        }

        [JsonPropertyName("profile")]
        public Profile Profile { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class Profile
    {
        [JsonPropertyName("real_name")]
        public string RealName { get; set; }

        [JsonPropertyName("status_text")]
        public string StatusText { get; set; }

        [JsonPropertyName("status_emoji")]
        public string StatusEmoji { get; set; }

        [JsonPropertyName("status_expiration")]
        public long StatusExpiration { get; set; }
    }
}
