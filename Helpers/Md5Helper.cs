using System.Security.Cryptography;

namespace BackblazeB2.Helpers;

public class Md5Helper
{
    public static string FromFile(IFormFile file)
    {
        using (var stream = file.OpenReadStream())
        {
            using (var md5 = MD5.Create())
            {
                byte[] hashBytes = md5.ComputeHash(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }
    }
}