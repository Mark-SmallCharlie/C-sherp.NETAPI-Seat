using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WebApplication1.Data;
using WebApplication1.Models.Entities;
using WebApplication1.Services.Interfaces;
using WebApplication1.Models.DTOs.Requests;
using WebApplication1.Models.DTOs.Responses;
using WebApplication1.Security;
namespace WebApplication1.API.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _context;
    private readonly ILogger<UserService> _logger;

    public UserService(AppDbContext context, ILogger<UserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<User?> GetUserByOpenIdAsync(string openId)
    {
        try
        {
            _logger.LogInformation("查询用户 OpenId: {OpenId}", openId);

            if (string.IsNullOrWhiteSpace(openId))
            {
                _logger.LogWarning("OpenId 为空");
                return null;
            }

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.OpenId == openId);

            if (user == null)
            {
                _logger.LogInformation("未找到 OpenId 为 {OpenId} 的用户", openId);
            }
            else
            {
                _logger.LogInformation("找到用户: {NickName} (ID: {UserId})", user.NickName, user.Id);
            }

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询用户 OpenId: {OpenId} 时发生错误", openId);
            throw;
        }
    }

    public async Task<User> CreateUserAsync(string openId, string nickName, string? avatarUrl)
    {
        try
        {
            _logger.LogInformation("创建新用户 - OpenId: {OpenId}, NickName: {NickName}", openId, nickName);

            // 验证输入
            if (string.IsNullOrWhiteSpace(openId))
                throw new ArgumentException("OpenId 不能为空", nameof(openId));

            if (string.IsNullOrWhiteSpace(nickName))
                throw new ArgumentException("昵称不能为空", nameof(nickName));

            // 检查是否已存在
            var existingUser = await GetUserByOpenIdAsync(openId);
            if (existingUser != null)
            {
                _logger.LogWarning("用户 OpenId: {OpenId} 已存在", openId);
                return existingUser; // 返回已存在的用户
            }

            var user = new User
            {
                OpenId = openId.Trim(),
                NickName = nickName.Trim(),
                AvatarUrl = avatarUrl,
                Role = UserRole.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("用户创建成功 - ID: {UserId}, NickName: {NickName}", user.Id, user.NickName);
            return user;
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "数据库保存用户失败 - OpenId: {OpenId}", openId);
            throw new InvalidOperationException("创建用户时发生数据库错误", dbEx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建用户失败 - OpenId: {OpenId}, NickName: {NickName}", openId, nickName);
            throw;
        }
    }

    public async Task<List<User>> GetPendingUsersAsync()
    {
        try
        {
            _logger.LogInformation("获取待审核用户列表");

            var users = await _context.Users
                .AsNoTracking()
                .Where(u => u.Role == UserRole.Pending)
                .OrderBy(u => u.CreatedAt)
                .ToListAsync();

            _logger.LogInformation("找到 {Count} 个待审核用户", users.Count);
            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取待审核用户列表失败");
            throw;
        }
    }

    public async Task<bool> ApproveUserAsync(int userId, bool isApproved, string? note = null)
    {
        try
        {
            _logger.LogInformation("审核用户 - UserId: {UserId}, 是否通过: {IsApproved}", userId, isApproved);

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("用户 ID: {UserId} 不存在", userId);
                return false;
            }

            if (user.Role != UserRole.Pending)
            {
                _logger.LogWarning("用户 ID: {UserId} 状态为 {CurrentRole}，无需审核", userId, user.Role);
                return false;
            }

            user.Role = isApproved ? UserRole.User : UserRole.Rejected;
            // 可以添加审核时间和审核人信息
            await _context.SaveChangesAsync();

            _logger.LogInformation("用户审核完成 - UserId: {UserId}, 新状态: {NewRole}", userId, user.Role);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "审核用户失败 - UserId: {UserId}", userId);
            throw;
        }
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        try
        {
            _logger.LogInformation("获取所有用户列表");

            var users = await _context.Users
                .AsNoTracking()
                .OrderBy(u => u.CreatedAt)
                .ToListAsync();

            _logger.LogInformation("总用户数: {Count}", users.Count);
            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户列表失败");
            throw;
        }
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        try
        {
            _logger.LogInformation("查询用户 ID: {UserId}", userId);

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                _logger.LogWarning("用户 ID: {UserId} 不存在", userId);
            }

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询用户 ID: {UserId} 失败", userId);
            throw;
        }
    }

    public async Task<bool> UpdateUserRoleAsync(int userId, UserRole newRole)
    {
        try
        {
            _logger.LogInformation("更新用户角色 - UserId: {UserId}, 新角色: {NewRole}", userId, newRole);

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("用户 ID: {UserId} 不存在", userId);
                return false;
            }

            user.Role = newRole;
            await _context.SaveChangesAsync();

            _logger.LogInformation("用户角色更新成功");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新用户角色失败 - UserId: {UserId}", userId);
            throw;
        }
    }
    public async Task<RegisterResult> RegisterAsync(RegisterRequest request)
    {
        try
        {
            _logger.LogInformation("注册新用户 - OpenId: {OpenId}, NickName: {NickName}", request.OpenId, request.NickName);

            // 验证输入
            if (string.IsNullOrWhiteSpace(request.OpenId))
                throw new ArgumentException("OpenId 不能为空", nameof(request.OpenId));

            if (string.IsNullOrWhiteSpace(request.NickName))
                throw new ArgumentException("昵称不能为空", nameof(request.NickName));

            var hasPassword = !string.IsNullOrWhiteSpace(request.Password);
            if (hasPassword && request.Password!.Length < 6)
            {
                return new RegisterResult
                {
                    Success = false,
                    Message = "密码长度至少 6 位"
                };
            }

            // 检查是否已存在
            var existingUser = await GetUserByOpenIdAsync(request.OpenId);
            if (existingUser != null)
            {
                _logger.LogWarning("用户 OpenId: {OpenId} 已存在", request.OpenId);
                return new RegisterResult
                {
                    Success = false,
                    Message = "用户已存在"
                };
            }

            string? passwordHash = null;
            var role = UserRole.Pending;
            if (hasPassword)
            {
                passwordHash = PasswordHasher.Hash(request.Password!.Trim());
                role = UserRole.User;
            }

            var user = new User
            {
                OpenId = request.OpenId.Trim(),
                NickName = request.NickName.Trim(),
                AvatarUrl = request.AvatarUrl,
                PasswordHash = passwordHash,
                Role = role,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("用户注册成功 - ID: {UserId}, NickName: {NickName}", user.Id, user.NickName);
            return new RegisterResult
            {
                Success = true,
                UserId = user.Id,
                Message = "注册成功",
                OpenId = user.OpenId,
                NickName = user.NickName
            };
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "数据库保存用户失败 - OpenId: {OpenId}", request.OpenId);
            throw new InvalidOperationException("注册用户时发生数据库错误", dbEx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "注册用户失败 - OpenId: {OpenId}, NickName: {NickName}", request.OpenId, request.NickName);
            throw;
        }
    }
}
