/**
 * 这是一个用于表示微信登录会话结果的DTO类。它包含了微信登录接口返回的相关字段，如openid、session_key、unionid、errcode和errmsg。
 * 这些字段分别表示用户的唯一标识、会话密钥、用户在多个应用间的唯一标识、错误码和错误信息。
 * 通过这个类，开发者可以方便地处理微信登录接口的响应数据。
 */
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
