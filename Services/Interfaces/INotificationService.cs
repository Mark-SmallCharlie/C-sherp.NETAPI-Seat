using WebApplication1.Models.Entities;

namespace WebApplication1.Services.Interfaces;

/// <summary>
/// 通知服务接口，提供通知创建、查询、标记已读等功能。
/// </summary>
public interface INotificationService
{
    /// <summary>为用户创建通知</summary>
    Task<Notification> CreateNotificationAsync(int userId, string title, string content,
        NotificationType type = NotificationType.SystemMessage, int? relatedReservationId = null);

    /// <summary>获取用户的所有通知（最新在前）</summary>
    Task<List<Notification>> GetUserNotificationsAsync(int userId, bool unreadOnly = false);

    /// <summary>获取用户未读通知数量</summary>
    Task<int> GetUnreadCountAsync(int userId);

    /// <summary>标记单条通知为已读</summary>
    Task<bool> MarkAsReadAsync(int notificationId, int userId);

    /// <summary>标记用户所有通知为已读</summary>
    Task<int> MarkAllAsReadAsync(int userId);
}
