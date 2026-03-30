using HospitalManagementSystem.Core.DTOs;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
namespace HospitalManagementSystem.Web.Helper
{
    public class JWTTokenManager
    {
        private readonly IConfiguration _configuration;

        public JWTTokenManager(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public static string GenerateJWTToken(JWTokenDTO claims, IConfiguration configuration)
        {
            string result = null;

            if (claims == null)
            {
                return result;
            }

            try
            {
                byte[] privateKeyRaw = { };

                // ✅ Read from appsettings
                var privateKeyPem = configuration["PrivateKey"];

                privateKeyPem = privateKeyPem.Replace("\\n", "\n");

                privateKeyPem = privateKeyPem.Replace("-----BEGIN PRIVATE KEY-----", "");
                privateKeyPem = privateKeyPem.Replace("-----END PRIVATE KEY-----", "");

                privateKeyRaw = Convert.FromBase64String(privateKeyPem);

                RSACryptoServiceProvider provider = new RSACryptoServiceProvider();
                provider.ImportPkcs8PrivateKey(new ReadOnlySpan<byte>(privateKeyRaw), out _);

                var rsaSecurityKey = new RsaSecurityKey(provider);

                var now = DateTime.UtcNow;
                var unixTimeSeconds = new DateTimeOffset(now).ToUnixTimeSeconds();

                var userClaims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Iat, unixTimeSeconds.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

                if (claims.Issuer != null)
                    userClaims.Add(new Claim(JwtRegisteredClaimNames.Iss, claims.Issuer));

                if (claims.Audience != null)
                    userClaims.Add(new Claim(JwtRegisteredClaimNames.Aud, claims.Audience));

                if (claims.Subject != null)
                    userClaims.Add(new Claim(JwtRegisteredClaimNames.Sub, claims.Subject));

                var jwt = new JwtSecurityToken(
                    claims: userClaims,
                    notBefore: now,
                    expires: now.AddMinutes(claims.Expiry),
                    signingCredentials: new SigningCredentials(rsaSecurityKey, SecurityAlgorithms.RsaSha256)
                );

                result = new JwtSecurityTokenHandler().WriteToken(jwt);
            }
            catch (Exception)
            {
                return null;
            }

            return result;
        }
    }
}
