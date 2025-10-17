using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace sportshopwebsite.Helper
{
    public class JwtHelper
    {
        private readonly RSACryptoServiceProvider rsa;
        private const int ACCESS_TOKEN_EXPIRY_DAYS = 3;
        private const int REFRESH_TOKEN_EXPIRY_DAYS = 7;

        // Constructor: Khởi tạo với Private Key
        public JwtHelper(string privateKeyXml)
        {
            // Kiểm tra và loại bỏ header/footer nếu PrivateKey được lưu dưới dạng khác XML
            if (privateKeyXml.Contains("BEGIN RSA PRIVATE KEY"))
            {
                // Giả định PrivateKey được lưu dưới dạng XML
                throw new ArgumentException("PrivateKey must be provided in RSA XML string format.");
            }

            rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(privateKeyXml);
        }

        // -------------------------------------------------------------------
        // FUNCTION 1: Generate Access/Refresh Token
        // -------------------------------------------------------------------
        public string GenerateToken(int userId, string type)
        {
            type = type.ToLower();
            var key = new RsaSecurityKey(rsa);
            var creds = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);

            var claims = new[]
            {
                // Thêm claim 'typ' để phân biệt Access/Refresh Token (tùy chọn)
                new Claim("typ", type),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            DateTime? expires = null;
            if (type == "access")
            {
                expires = DateTime.UtcNow.AddDays(ACCESS_TOKEN_EXPIRY_DAYS);
            }
            else if (type == "refresh")
            {
                expires = DateTime.UtcNow.AddDays(REFRESH_TOKEN_EXPIRY_DAYS);
            }
            else
            {
                throw new ArgumentException("Token type must be 'access' or 'refresh'.");
            }

            var token = new JwtSecurityToken(
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // -------------------------------------------------------------------
        // FUNCTION 2: Validate Token (Giữ nguyên)
        // -------------------------------------------------------------------
        public static bool ValidateToken(string token, string publicKeyXml)
        {
            var rsaPub = new RSACryptoServiceProvider();
            try
            {
                rsaPub.FromXmlString(publicKeyXml);
            }
            catch (CryptographicException)
            {
                // Xử lý nếu PublicKey không hợp lệ
                return false;
            }

            var key = new RsaSecurityKey(rsaPub);

            var handler = new JwtSecurityTokenHandler();
            var validationParams = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = false,
                ValidateAudience = false,
                RequireExpirationTime = true,
                ClockSkew = TimeSpan.Zero // Yêu cầu kiểm tra thời gian chính xác
            };

            try
            {
                handler.ValidateToken(token, validationParams, out _);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // -------------------------------------------------------------------
        // FUNCTION 3: Get Claims from Validated Token (Mới)
        // Dùng để lấy UserId sau khi token được xác thực
        // -------------------------------------------------------------------
        public static ClaimsPrincipal GetPrincipalFromToken(string token, string publicKeyXml)
        {
            var rsaPub = new RSACryptoServiceProvider();
            try
            {
                rsaPub.FromXmlString(publicKeyXml);
            }
            catch (CryptographicException)
            {
                return null;
            }

            var key = new RsaSecurityKey(rsaPub);
            var handler = new JwtSecurityTokenHandler();

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = false,
                ValidateAudience = false,
                RequireExpirationTime = true,
                ClockSkew = TimeSpan.Zero
            };

            try
            {
                // Validate và trả về ClaimsPrincipal
                return handler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
            }
            catch
            {
                return null;
            }
        }

        // -------------------------------------------------------------------
        // FUNCTION 4: Generate RSA Key Pair (Mới)
        // Dùng khi người dùng đăng ký để lưu Private/Public Key vào bảng KeyTokens
        // -------------------------------------------------------------------
        public static KeyValuePair<string, string> GenerateRsaKeyPair()
        {
            using (var rsa = new RSACryptoServiceProvider(2048)) // Sử dụng kích thước khóa 2048 bit
            {
                // Lấy Private Key (bao gồm cả Public Key)
                string privateKeyXml = rsa.ToXmlString(true);

                // Lấy Public Key (chỉ Public Key)
                string publicKeyXml = rsa.ToXmlString(false);

                return new KeyValuePair<string, string>(privateKeyXml, publicKeyXml);
            }
        }

        // -------------------------------------------------------------------
        // FUNCTION 5: Lấy thời gian hết hạn của token
        // -------------------------------------------------------------------
        public static DateTime GetTokenExpiryTime(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadToken(token) as JwtSecurityToken;

            if (jwtToken?.ValidTo != null)
            {
                return jwtToken.ValidTo;
            }
            // Trả về thời gian tối thiểu nếu không đọc được (hoặc xử lý lỗi tùy chọn)
            return DateTime.MinValue;
        }

        // -------------------------------------------------------------------
        // PROPERTY: Lấy thời gian hết hạn cho Refresh Token (Dùng trong accessService)
        // -------------------------------------------------------------------
        public static int RefreshTokenExpiryDays => REFRESH_TOKEN_EXPIRY_DAYS;
    }
}