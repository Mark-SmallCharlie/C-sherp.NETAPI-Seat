using Microsoft.EntityFrameworkCore;    
using WebApplication1.Models.Entities;
/**
 *AppDbContext是应用程序的数据库上下文类，负责管理与数据库的连接和操作。
 *它继承自Entity Framework Core的DbContext类，并定义了应用程序中使用的实体集合（DbSet）。
 *在OnModelCreating方法中，可以对模型进行额外的配置，例如设置索引、添加种子数据等。
 */
namespace WebApplication1.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // 将实体类映射到数据库表
        public DbSet<User> Users { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<SeatStatusHistory> SeatStatusHistories { get; set; }
        public DbSet<AdminUser> AdminUsers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 可以对模型进行额外的配置，例如索引、种子数据等
            modelBuilder.Entity<User>()
                .HasIndex(u => u.OpenId)
                .IsUnique(); // 确保OpenId唯一

            modelBuilder.Entity<Reservation>()
                .HasIndex(r => new { r.SeatNumber, r.StartTime, r.EndTime }); // 复合索引用于快速查找冲突

            // 可以在这里添加初始管理员账户（种子数据）
            modelBuilder.Entity<AdminUser>().HasData(
                new AdminUser
                {
                    Id = 1,
                    Username = "admin",
                    // 密码是 "admin123" 的哈希值 (仅供演示，生产环境应用更安全的方法)
                    PasswordHash = "240BE518FABD2724DDB6F04EEB1DA5967448D7E831C08C8FA822809F74C720A9",
                    DisplayName = "系统管理员",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            );
        }
    }
}
