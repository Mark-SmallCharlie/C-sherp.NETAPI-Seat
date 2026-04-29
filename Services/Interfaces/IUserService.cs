using WebApplication1.Models.Entities;
using WebApplication1.Models.DTOs.Requests;
using WebApplication1.Models.DTOs.Responses;
// 这是一个用于定义用户服务接口的代码文件。
// IUserService接口包含了获取用户信息、创建用户、审批用户、更新用户角色以及注册用户等方法。
namespace WebApplication1.Services.Interfaces
{
    public interface IUserService
    {
        Task<User?> GetUserByOpenIdAsync(string openId);
        Task<User> CreateUserAsync(string openId, string nickName, string? avatarUrl);
        Task<List<User>> GetPendingUsersAsync();
        Task<bool> ApproveUserAsync(int userId, bool isApproved, string? note = null);
        Task<List<User>> GetAllUsersAsync();
        Task<User?> GetUserByIdAsync(int userId);
        //Task<User?> UpdateUserAsync(int userId, UserUpdateRequest request);
        Task<bool> UpdateUserRoleAsync(int userId, UserRole newRole);
        Task<RegisterResult> RegisterAsync(RegisterRequest request);
    }
}
