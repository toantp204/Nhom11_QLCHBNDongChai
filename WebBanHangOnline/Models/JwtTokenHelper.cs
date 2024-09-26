using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

public class JwtTokenHelper
{
    private const string SecretKey = "eiljkrfsymaknbjsremgzcwladkgtmeqssclevybpllxpxrudpnknrykeebjmockwcgfyapnnloxtztwlcnamivnybjwixznvozwboriyntizmuetrddjflpzhejjorphjqkldgzfekairmjaaioyapoaowaeyrdfuuiaxlkxhbfthoravpzmdhtiyrnnvwboxixiounkbctbqcwwfgsthzjbvdccdykxrdosngwvhgvplsgxtutmauzqpdcbbvo\t"; // Thay bằng chuỗi bí mật mạnh hơn
    private const int TokenExpiryTimeInMinutes = 60; // Token hết hạn sau 60 phút

    // Hàm tạo JWT
    public static string GenerateToken(string username, string role)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(SecretKey);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role)
            }),
            Expires = DateTime.UtcNow.AddMinutes(TokenExpiryTimeInMinutes),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    // Hàm xác thực JWT và trả về thông tin người dùng
    public static ClaimsPrincipal GetPrincipal(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(SecretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            };

            SecurityToken validatedToken;
            var principal = tokenHandler.ValidateToken(token, validationParameters, out validatedToken);
            return principal;
        }
        catch
        {
            return null;
        }
    }
}
