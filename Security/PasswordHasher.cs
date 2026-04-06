using System.Security.Cryptography;
using System.Text;

namespace WebApplication1.Security;

/// <summary>
/// 与现有 AdminUser 种子数据一致的 SHA256 十六进制哈希，便于与 AuthService 校验逻辑统一。
/// </summary>
public static class PasswordHasher
{
    public static string Hash(string password)
    {
        using var sha256 = SHA256.Create();
        return Convert.ToHexString(sha256.ComputeHash(Encoding.UTF8.GetBytes(password)));
    }

    public static bool Verify(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(storedHash)) return false;
        try
        {
            var hashed = Hash(password);
            return string.Equals(hashed, storedHash, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
