using System.Text.Json.Serialization;
namespace WebApplication1.Models.DTOs.Responses
{
    public class WeChatSessionResult
    {
        [JsonPropertyName("openid")]
        public string? OpenId { get; set; }

        [JsonPropertyName("session_key")]
        public string? SessionKey { get; set; }

        [JsonPropertyName("unionid")]
        public string? UnionId { get; set; }

        [JsonPropertyName("errcode")]
        public int ErrorCode { get; set; }

        [JsonPropertyName("errmsg")]
        public string? ErrorMessage { get; set; }
    }
}
