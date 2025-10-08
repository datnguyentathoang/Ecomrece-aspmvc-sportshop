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

        public JwtHelper(string privateKeyXml)
        {
            rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(privateKeyXml);
        }

        public string GenerateAccessToken(int userId)
        {
            var key = new RsaSecurityKey(rsa);
            var creds = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);

            var claims = new[]
            {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken(int userId)
        {
            var key = new RsaSecurityKey(rsa);
            var creds = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);

            var claims = new[]
            {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public static bool ValidateToken(string token, string publicKeyXml)
        {
            var rsaPub = new RSACryptoServiceProvider();
            rsaPub.FromXmlString(publicKeyXml);

            var key = new RsaSecurityKey(rsaPub);

            var handler = new JwtSecurityTokenHandler();
            var validationParams = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = false,
                ValidateAudience = false,
                RequireExpirationTime = true
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
    }
}