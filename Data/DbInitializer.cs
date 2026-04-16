
using System.Security.Cryptography;
using System.Text;
using WebApplication1.Data;
using WebApplication1.Models.Entities;

namespace WebApplication1.Data;
/**
 DbInitializer类负责初始化数据库，确保数据库已创建，
并在没有管理员账户的情况下添加一个默认管理员账户。
它还包含一个HashPassword方法，用于将密码进行哈希处理，以提高安全性。
 */
public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext context)
    {
        // 确保数据库已创建
        await context.Database.EnsureCreatedAsync();

        // 检查是否已有管理员账户
        if (!context.AdminUsers.Any())
        {
            var adminUser = new AdminUser
            {
                Username = "admin",
                PasswordHash = HashPassword("admin"), // 默认密码
                DisplayName = "系统管理员",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.AdminUsers.Add(adminUser);
            await context.SaveChangesAsync();
        }
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(hashedBytes);
    }
}
