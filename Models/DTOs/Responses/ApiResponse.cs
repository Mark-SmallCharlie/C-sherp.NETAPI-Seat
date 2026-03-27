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
