using WebApplication1.Models.Entities;
using WebApplication1.Models.DTOs.Requests;
using WebApplication1.Models.DTOs.Responses;
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
