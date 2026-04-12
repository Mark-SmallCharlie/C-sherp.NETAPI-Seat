
using System.Security.Cryptography;
using System.Text;
using WebApplication1.Data;
using WebApplication1.Models.Entities;

namespace WebApplication1.Data;

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
