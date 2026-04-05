using HappyCraftEvent.Contracts.DTOs.Users;
using HappyCraftEvent.Contracts.Enums;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace HappyCraftEvent.Helper.Utilities;

/// <summary>
/// Service for generating and managing JWT tokens.
/// </summary>
public class JwtTokenService
{
    private readonly string _issuer;
    private readonly string _audience;
    private readonly string _key;
    private readonly int _accessTokenMinutes;

    public JwtTokenService(IConfiguration configuration)
    {
        _issuer = configuration["Jwt:Issuer"] ?? "HappyCraftEvent";
        _audience = configuration["Jwt:Audience"] ?? "HappyCraftevent-API";
        _key = configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key not configured");
        _accessTokenMinutes = int.Parse(configuration["Jwt:AccessTokenMinutes"] ?? "15");
    }

    /// <summary>
    /// Generates a JWT access token with user claims from UserDto.
    /// </summary>
    public string GenerateAccessToken(UserDto user, IEnumerable<string> scopes)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_key);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new("role", user.Role.ToString()),
            new("status", user.Status.ToString()),
            new("jti", Guid.NewGuid().ToString())
        };

        if (user.Gender.HasValue)
            claims.Add(new Claim("gender", user.Gender.Value.ToString()));

        // Add scope claims (multiple claims approach)
        foreach (var scope in scopes)
        {
            claims.Add(new Claim("scope", scope));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_accessTokenMinutes),
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Generates a random refresh token string.
    /// </summary>
    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[3200];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    /// <summary>
    /// Returns the configured access token lifetime in seconds.
    /// </summary>
    public int GetAccessTokenExpirySeconds() => _accessTokenMinutes * 60;
}
