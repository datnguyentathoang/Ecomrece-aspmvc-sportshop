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


        public JwtHelper(string privateKeyXml)
        {
            if (string.IsNullOrEmpty(privateKeyXml))
            {
                throw new ArgumentException("PrivateKey cannot be null or empty.");
            }

            if (privateKeyXml.Contains("BEGIN RSA PRIVATE KEY"))
            {
                throw new ArgumentException("PrivateKey must be provided in RSA XML string format.");
            }

            rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(privateKeyXml);
        }


        public string GenerateToken(int userId, string type)
        {
            type = type.ToLower();
            var key = new RsaSecurityKey(rsa);
            var creds = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);

            var claims = new[]
            {
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


        public static bool ValidateToken(string token, string publicKeyXml)
        {
            var rsaPub = new RSACryptoServiceProvider();
            try
            {
                rsaPub.FromXmlString(publicKeyXml);
            }
            catch (CryptographicException)
            {

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
                ClockSkew = TimeSpan.Zero 
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
                return handler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
            }
            catch
            {
                return null;
            }
        }


        public static KeyValuePair<string, string> GenerateRsaKeyPair()
        {
            var rsa = new RSACryptoServiceProvider(2048);
            try
            {
                string privateKeyXml = rsa.ToXmlString(true);
                string publicKeyXml = rsa.ToXmlString(false);

                if (string.IsNullOrWhiteSpace(privateKeyXml) || !privateKeyXml.Contains("<RSAKeyValue>"))
                {
                    throw new Exception("Không thể tạo private key hợp lệ. RSA provider không trả về XML key.");
                }

                return new KeyValuePair<string, string>(privateKeyXml, publicKeyXml);
            }
            finally
            {
                rsa.PersistKeyInCsp = false;
                rsa.Clear();
            }
        }



        public static DateTime GetTokenExpiryTime(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadToken(token) as JwtSecurityToken;

            if (jwtToken?.ValidTo != null)
            {
                return jwtToken.ValidTo;
            }
            return DateTime.MinValue;
        }


        public static int RefreshTokenExpiryDays => REFRESH_TOKEN_EXPIRY_DAYS;
    }
}