/***这是一个用于API响应的类，包含了一个泛型版本和一个非泛型版本。泛型版本可以返回数据，
 * 而非泛型版本则适用于不需要返回数据的操作。每个类都包含一个表示操作是否成功的布尔值和一个消息字符串。
***/
namespace WebApplication1.Models.DTOs.Responses
{
    // 泛型响应类，用于返回数据
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
    }

    // 非泛型响应类，用于不返回数据的操作
    public class ApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        public static ApiResponse Ok(string message = "操作成功") => new() { Success = true, Message = message };
        public static ApiResponse Fail(string message = "操作失败") => new() { Success = false, Message = message };
    }

}
