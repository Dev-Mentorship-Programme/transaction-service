using System.Security.Cryptography;
using System.Text;

namespace TransactionService.Infrastructure.Signing;
public interface ILinkSigner
{
    string SignToken(Guid resourceId, TimeSpan ttl);
    bool VerifyToken(string token, out Guid resourceId);
}

public class HmacLinkSigner : ILinkSigner
{
    private readonly byte[] _key;
    public HmacLinkSigner(string key)
    {
        if (string.IsNullOrEmpty(key)) key = "dev-key";
        _key = Encoding.UTF8.GetBytes(key);
    }

    public string SignToken(Guid resourceId, TimeSpan ttl)
    {
        var payload = $"{resourceId}|{DateTimeOffset.UtcNow.Add(ttl).ToUnixTimeSeconds()}";
        var sig = HmacSha256(payload);
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload)) + "." + Convert.ToBase64String(sig);
        return token;
    }

    public bool VerifyToken(string token, out Guid resourceId)
    {
        resourceId = Guid.Empty;
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 2) return false;
            var payload = Encoding.UTF8.GetString(Convert.FromBase64String(parts[0]));
            var sig = Convert.FromBase64String(parts[1]);
            var expected = HmacSha256(payload);
            if (!sig.SequenceEqual(expected)) return false;
            var pieces = payload.Split('|');
            resourceId = Guid.Parse(pieces[0]);
            var exp = DateTimeOffset.FromUnixTimeSeconds(long.Parse(pieces[1]));
            return DateTimeOffset.UtcNow <= exp;
        }
        catch { return false; }
    }

    private byte[] HmacSha256(string text)
    {
        using var hmac = new HMACSHA256(_key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(text));
    }
}
