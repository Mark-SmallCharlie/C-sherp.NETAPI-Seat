using Microsoft.EntityFrameworkCore;
using WebApplication1.Models.Entities;
using WebApplication1.Security;

namespace WebApplication1.Data;
/**
 DbInitializer类负责初始化数据库，确保数据库已创建，
并在没有管理员账户的情况下添加一个默认管理员账户。
使用 PasswordHasher（BCrypt）进行密码哈希，与登录验证保持一致。
 */
public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext context)
    {
        // 确保数据库已创建
        await context.Database.EnsureCreatedAsync();

        // 检查是否已有管理员账户
        var adminUser = context.AdminUsers.FirstOrDefault(a => a.Username == "admin");

        if (adminUser == null)
        {
            // 不存在则创建，默认密码 admin
            adminUser = new AdminUser
            {
                Username = "admin",
                PasswordHash = PasswordHasher.Hash("admin"),
                DisplayName = "系统管理员",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.AdminUsers.Add(adminUser);
            await context.SaveChangesAsync();
        }
        else if (!adminUser.PasswordHash.StartsWith("$2"))
        {
            // 已存在但密码是旧的SHA256格式（不是BCrypt的$2开头），重新用BCrypt哈希
            adminUser.PasswordHash = PasswordHasher.Hash("admin");
            await context.SaveChangesAsync();
        }
    }
}
