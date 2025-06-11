// Models/LoginResult.cs
using System;
using System.Text.Json.Serialization;

namespace ModPC_Gui.Models
{
    public class LoginResult
    {
        [JsonPropertyName("code")]
        public int code { get; set; }

        [JsonPropertyName("message")]
        public string message { get; set; }

        [JsonPropertyName("data")]
        public LoginData data { get; set; }
    }
}