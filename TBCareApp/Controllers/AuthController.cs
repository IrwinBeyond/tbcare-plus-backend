using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TBCarePlus.API.Data;
using TBCarePlus.API.DTOs;
using TBCarePlus.API.Models;

namespace TBCarePlus.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly AppDbContext _db;

    public AuthController(IHttpClientFactory httpClientFactory, IConfiguration config, AppDbContext db)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _db = db;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] AuthRegisterRequest request)
    {
        var supabaseUrl = _config["Supabase:Url"];
        var supabaseKey = _config["Supabase:Key"];

        if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
            return StatusCode(500, ApiResponse<object>.Fail("Supabase configuration is missing."));

        using var http = _httpClientFactory.CreateClient();

        var signupPayload = new
        {
            email = request.Email,
            password = request.Password,
            options = new
            {
                data = new
                {
                    display_name = request.Nickname ?? "",
                    email_confirm = true
                }
            }
        };

        var requestBody = new StringContent(
            JsonSerializer.Serialize(signupPayload), Encoding.UTF8, "application/json");

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{supabaseUrl}/auth/v1/signup")
        {
            Content = requestBody
        };
        httpRequest.Headers.Add("apikey", supabaseKey);

        var response = await http.SendAsync(httpRequest);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var msg = ParseSupabaseError(responseBody, "Registration failed.");
            return BadRequest(ApiResponse<object>.Fail(msg));
        }

        var authResponse = BuildAuthResponse(responseBody);

        var profile = new Profile
        {
            Id = Guid.Parse(authResponse.User.Id),
            Nickname = request.Nickname ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Profiles.Add(profile);
        await _db.SaveChangesAsync();

        return Created(string.Empty, ApiResponse<AuthResponseDto>.Ok(authResponse, "Registration successful."));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AuthLoginRequest request)
    {
        var supabaseUrl = _config["Supabase:Url"];
        var supabaseKey = _config["Supabase:Key"];

        if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
            return StatusCode(500, ApiResponse<object>.Fail("Supabase configuration is missing."));

        using var http = _httpClientFactory.CreateClient();

        var loginPayload = new { email = request.Email, password = request.Password };

        var requestBody = new StringContent(
            JsonSerializer.Serialize(loginPayload), Encoding.UTF8, "application/json");

        var httpRequest = new HttpRequestMessage(HttpMethod.Post,
            $"{supabaseUrl}/auth/v1/token?grant_type=password")
        {
            Content = requestBody
        };
        httpRequest.Headers.Add("apikey", supabaseKey);

        var response = await http.SendAsync(httpRequest);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var msg = ParseSupabaseError(responseBody, "Login failed.");
            return Unauthorized(ApiResponse<object>.Fail(msg));
        }

        var authResponse = BuildAuthResponse(responseBody);

        // Override FullName with nickname from profiles table
        if (Guid.TryParse(authResponse.User.Id, out var userId))
        {
            var profile = await _db.Profiles.FindAsync(userId);
            if (profile != null)
            {
                if (!string.IsNullOrEmpty(profile.Nickname))
                    authResponse.User.FullName = profile.Nickname;
            }
            else
            {
                var nickname = authResponse.User.FullName ?? "";
                if (!string.IsNullOrEmpty(nickname))
                {
                    profile = new Profile
                    {
                        Id = userId,
                        Nickname = nickname,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _db.Profiles.Add(profile);
                    await _db.SaveChangesAsync();
                }
            }
        }

        return Ok(ApiResponse<AuthResponseDto>.Ok(authResponse, "Login successful."));
    }

    [HttpGet("me")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> GetMe()
    {
        var subClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                      ?? User.FindFirst("sub");
        if (subClaim is null || !Guid.TryParse(subClaim.Value, out var userId))
            return Unauthorized(ApiResponse<object>.Fail("User ID not found in token."));

        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value 
                    ?? User.FindFirst("email")?.Value;

        var profile = await _db.Profiles.FindAsync(userId);
        var nickname = profile?.Nickname;

        if (string.IsNullOrEmpty(nickname))
        {
            var nameClaim = User.FindFirst("name")?.Value 
                            ?? User.FindFirst("display_name")?.Value;
            nickname = nameClaim;
        }

        var userDto = new UserDto
        {
            Id = userId.ToString(),
            Email = email,
            FullName = nickname,
            ProfilePicture = profile?.ProfilePicture,
            Role = User.FindFirst("role")?.Value ?? "authenticated",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return Ok(ApiResponse<UserDto>.Ok(userDto, "User profile retrieved successfully."));
    }

    [HttpPut("me")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest request)
    {
        var subClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                      ?? User.FindFirst("sub");
        if (subClaim is null || !Guid.TryParse(subClaim.Value, out var userId))
            return Unauthorized(ApiResponse<object>.Fail("User ID not found in token."));

        var profile = await _db.Profiles.FindAsync(userId);
        if (profile == null)
        {
            profile = new Profile { Id = userId, CreatedAt = DateTime.UtcNow };
            _db.Profiles.Add(profile);
        }

        if (request.Nickname != null)
            profile.Nickname = request.Nickname;
        if (request.ProfilePicture != null)
            profile.ProfilePicture = request.ProfilePicture;
        profile.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new { message = "Profile updated." }, "Profile updated successfully."));
    }

    [HttpPost("change-password")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (string.IsNullOrEmpty(request.CurrentPassword) ||
            string.IsNullOrEmpty(request.NewPassword) ||
            string.IsNullOrEmpty(request.ConfirmPassword))
            return BadRequest(ApiResponse<object>.Fail("Semua kolom kata sandi harus diisi."));

        if (request.NewPassword != request.ConfirmPassword)
            return BadRequest(ApiResponse<object>.Fail("Kata sandi baru dan konfirmasi tidak cocok."));

        if (request.NewPassword.Length < 6)
            return BadRequest(ApiResponse<object>.Fail("Kata sandi baru minimal 6 karakter."));

        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                    ?? User.FindFirst("email")?.Value;
        if (string.IsNullOrEmpty(email))
            return Unauthorized(ApiResponse<object>.Fail("Email tidak ditemukan dalam token."));

        var supabaseUrl = _config["Supabase:Url"];
        var supabaseKey = _config["Supabase:Key"];

        if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
            return StatusCode(500, ApiResponse<object>.Fail("Supabase configuration is missing."));

        using var http = _httpClientFactory.CreateClient();

        // Step 1: Verify current password by attempting sign-in
        var verifyPayload = new { email, password = request.CurrentPassword };
        var verifyBody = new StringContent(
            JsonSerializer.Serialize(verifyPayload), Encoding.UTF8, "application/json");

        var verifyRequest = new HttpRequestMessage(HttpMethod.Post,
            $"{supabaseUrl}/auth/v1/token?grant_type=password")
        {
            Content = verifyBody
        };
        verifyRequest.Headers.Add("apikey", supabaseKey);

        var verifyResponse = await http.SendAsync(verifyRequest);
        if (!verifyResponse.IsSuccessStatusCode)
        {
            return BadRequest(ApiResponse<object>.Fail("Kata sandi saat ini salah."));
        }

        // Step 2: Change password using the current access token
        var authHeader = Request.Headers.Authorization.ToString();
        var changePayload = new { password = request.NewPassword };
        var changeBody = new StringContent(
            JsonSerializer.Serialize(changePayload), Encoding.UTF8, "application/json");

        var changeRequest = new HttpRequestMessage(HttpMethod.Put,
            $"{supabaseUrl}/auth/v1/user")
        {
            Content = changeBody
        };
        changeRequest.Headers.Add("apikey", supabaseKey);
        changeRequest.Headers.Add("Authorization", authHeader);

        var changeResponse = await http.SendAsync(changeRequest);
        var changeResponseBody = await changeResponse.Content.ReadAsStringAsync();

        if (!changeResponse.IsSuccessStatusCode)
        {
            var msg = ParseSupabaseError(changeResponseBody, "Gagal mengubah kata sandi.");
            return BadRequest(ApiResponse<object>.Fail(msg));
        }

        return Ok(ApiResponse<object>.Ok(new { message = "Password changed successfully." }, "Kata sandi berhasil diubah."));
    }

    /// <summary>
    /// Parses a Supabase error response body into a human-readable message.
    /// Supabase uses different error shapes depending on the endpoint:
    ///   OAuth token errors:  { "error": "...", "error_description": "..." }
    ///   Auth REST errors:    { "code": 400, "error_code": "...", "msg": "..." }
    ///   GoTrue v2 errors:    { "message": "..." }
    /// </summary>
    private static string ParseSupabaseError(string responseBody, string fallback)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            // OAuth token endpoint format
            if (root.TryGetProperty("error_description", out var ed) && ed.ValueKind == JsonValueKind.String)
                return ed.GetString()!;

            // GoTrue v2 / newer format
            if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
                return message.GetString()!;

            // GoTrue v1 format
            if (root.TryGetProperty("msg", out var msg) && msg.ValueKind == JsonValueKind.String)
                return msg.GetString()!;

            // Generic error field
            if (root.TryGetProperty("error", out var err) && err.ValueKind == JsonValueKind.String)
                return err.GetString()!;
        }
        catch { /* JSON parse failed, fall through */ }

        return fallback;
    }

    private static AuthResponseDto BuildAuthResponse(string supabaseResponseJson)
    {
        using var doc = JsonDocument.Parse(supabaseResponseJson);
        var root = doc.RootElement;

        var user = root.GetProperty("user");
        var userId = user.GetProperty("id").GetString()!;
        var email = user.GetProperty("email").GetString();
        var createdAt = user.TryGetProperty("created_at", out var ca)
            ? ca.GetDateTime() : DateTime.UtcNow;
        var updatedAt = user.TryGetProperty("updated_at", out var ua)
            ? ua.GetDateTime() : DateTime.UtcNow;

        var metadata = user.TryGetProperty("user_metadata", out var um) ? um : default;
        var fullName = metadata.ValueKind != JsonValueKind.Undefined
            && metadata.TryGetProperty("display_name", out var fn) ? fn.GetString() : null;

        return new AuthResponseDto
        {
            AccessToken = root.GetProperty("access_token").GetString()!,
            RefreshToken = root.GetProperty("refresh_token").GetString()!,
            ExpiresIn = root.GetProperty("expires_in").GetInt32(),
            User = new UserDto
            {
                Id = userId,
                Email = email,
                FullName = fullName,
                Role = user.TryGetProperty("role", out var r) ? r.GetString() : null,
                IsActive = true,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt,
            },
        };
    }
}

public class AuthRegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Nickname { get; set; }
}

public class AuthLoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public UserDto User { get; set; } = null!;
}
