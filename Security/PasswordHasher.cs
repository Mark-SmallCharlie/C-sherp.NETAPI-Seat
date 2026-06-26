namespace WebApplication1.Security;

/// <summary>
/// 使用 BCrypt 进行密码哈希与验证，自动加盐，抵抗彩虹表攻击。
/// 生成的哈希字符串格式为 $2a$...，长度约 60 字符。
/// </summary>
public static class PasswordHasher
{
    public static string Hash(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("密码不能为空", nameof(password));

        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public static bool Verify(string password, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(storedHash))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, storedHash);
        }
        catch
        {
            return false;
        }
    }
}
