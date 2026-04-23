//这是一个用于批准用户请求的DTO类，包含一个布尔属性Approve表示是否批准，以及一个可选的字符串属性Note用于添加备注信息。
namespace WebApplication1.Models.DTOs.Requests
{
    public class ApproveUserRequest
    {
        public bool Approve { get; set; }
        public string? Note { get; set; }
    }
}
