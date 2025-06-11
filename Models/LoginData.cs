using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ModPC_Gui.Models
{
    public class LoginData
    {
        [JsonPropertyName("uid")]
        public string uid { get; set; }

        [JsonPropertyName("token")]
        public string token { get; set; }

        [JsonPropertyName("refresh_token")]
        public string refresh_token { get; set; }

        [JsonPropertyName("expires_at")]
        public DateTime expires_at { get; set; }

        [JsonPropertyName("nickname")]
        public string nickname { get; set; }

        [JsonPropertyName("avatar_url")]
        public string avatar_url { get; set; }
    }
}

