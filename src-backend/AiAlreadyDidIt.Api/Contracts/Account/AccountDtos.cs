using System.ComponentModel.DataAnnotations;

namespace AiAlreadyDidIt.Api.Contracts.Account;

public class SignUpRequest
{
    [Required, EmailAddress, MaxLength(256)] public string Email { get; set; } = string.Empty;
    [Required, MinLength(3), MaxLength(40)] public string Username { get; set; } = string.Empty;
    [MaxLength(80)] public string? DisplayName { get; set; }
    [Required, MinLength(8), MaxLength(128)] public string Password { get; set; } = string.Empty;
    public string? Locale { get; set; }
}

public class SignInRequest
{
    /// <summary>E-mail or username.</summary>
    [Required] public string Login { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; } = true;
}

public class GoogleSignInRequest
{
    [Required] public string IdToken { get; set; } = string.Empty;
    public string? Locale { get; set; }
}

public class ForgotPasswordRequest
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    [Required] public string Token { get; set; } = string.Empty;
    [Required, MinLength(8), MaxLength(128)] public string Password { get; set; } = string.Empty;
}

public class VerifyEmailRequest
{
    [Required] public string Token { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    public string? CurrentPassword { get; set; }
    [Required, MinLength(8), MaxLength(128)] public string NewPassword { get; set; } = string.Empty;
}

public class UpdateProfileRequest
{
    [Required, MaxLength(80)] public string DisplayName { get; set; } = string.Empty;
    [MaxLength(1000)] public string? Bio { get; set; }
    [MaxLength(512)] public string? Website { get; set; }
    public string? Locale { get; set; }
}

public class MeDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public string? Website { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public int TrustLevel { get; set; }
    public string Locale { get; set; } = "en-US";
    public bool HasPassword { get; set; }
    public bool HasGoogle { get; set; }
    public int UnreadNotificationCount { get; set; }
    public int AppCount { get; set; }
    public int PendingAppCount { get; set; }
    public int FavoriteCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime TokenExpireDate { get; set; }
    public MeDto User { get; set; } = null!;
}

public class SessionDto
{
    public long Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? Ip { get; set; }
    public string? UserAgent { get; set; }
    public bool IsCurrent { get; set; }
}
