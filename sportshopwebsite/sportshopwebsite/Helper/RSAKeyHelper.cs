using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Web;

namespace sportshopwebsite.Helper
{
    public class RSAKeyHelper
    {
        public static void GenerateKeys(int userId, string folderPath, out string privateKeyXml, out string publicKeyXml)
        {
            using (var rsa = new RSACryptoServiceProvider(4096))
            {
                rsa.PersistKeyInCsp = false;

                // Private key (server giữ)
                privateKeyXml = rsa.ToXmlString(true);
                File.WriteAllText(Path.Combine(folderPath, $"user{userId}_private.xml"), privateKeyXml);

                // Public key (lưu database)
                publicKeyXml = rsa.ToXmlString(false);
                // TODO: Lưu publicKeyXml vào database Users.PublicKey
            }
        }
    }
}