using AiAlreadyDidIt.Api.Contracts.Account;
using AiAlreadyDidIt.Api.Contracts.Common;
using AiAlreadyDidIt.Api.Data;
using AiAlreadyDidIt.Api.Data.Seed;
using AiAlreadyDidIt.Api.Entities;
using AiAlreadyDidIt.Api.Infrastructure;
using AiAlreadyDidIt.Api.Infrastructure.Auth;
using AiAlreadyDidIt.Api.Infrastructure.Email;
using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace AiAlreadyDidIt.Api.Services.Account;

/// <summary>Sign-up / sign-in (password + Google), refresh-token rotation, e-mail verification, password reset, profile.</summary>
public partial class AccountService(AadiDbContext db, IPasswordHasher hasher, JwtTokenService tokens, IEmailSender email,
    IOptions<JwtOptions> jwt, IOptions<SiteOptions> site, ICurrentUser currentUser, ILogger<AccountService> logger)
{
    [GeneratedRegex("^[a-z0-9][a-z0-9_.-]{2,39}$")] private static partial Regex UsernameRegex();
    private static readonly string[] ReservedUsernames = ["admin", "administrator", "root", "system", "api", "mcp", "support", "help", "about", "app", "apps", "search", "dashboard", "login", "signup", "me", "null", "undefined"];

    public sealed record IssuedTokens(AuthResponse Response, string RefreshToken, DateTime RefreshExpires);

    // ---------------------------------------------------------------- sign up / sign in

    public async Task<IssuedTokens> SignUpAsync(SignUpRequest request, CancellationToken ct)
    {
        var bag = new ValidationBag();
        var emailAddr = request.Email.Trim().ToLowerInvariant();
        var username = request.Username.Trim().ToLowerInvariant();
        bag.Require(UsernameRegex().IsMatch(username), "username", "Username must be 3-40 characters: letters, digits, dot, dash or underscore, starting with a letter or digit.");
        bag.Require(!ReservedUsernames.Contains(username), "username", "This username is reserved.");
        bag.Require(request.Password.Length >= 8, "password", "Password must be at least 8 characters.");
        bag.ThrowIfAny();
        if (await db.Users.AnyAsync(u => u.Email == emailAddr, ct)) throw ApiException.Unprocessable("An account with this e-mail already exists. Sign in instead.", "email");
        if (await db.Users.AnyAsync(u => u.Username == username, ct)) throw ApiException.Unprocessable("This username is already taken.", "username");

        var role = await db.Roles.FirstAsync(r => r.Name == ReferenceDataSeeder.MemberRoleName, ct);
        var user = new User
        {
            Email = emailAddr,
            Username = username,
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? request.Username.Trim() : request.DisplayName.Trim(),
            PasswordHash = hasher.Hash(request.Password),
            RoleId = role.Id,
            Role = role,
            Locale = NormalizeLocale(request.Locale),
            IsActive = true,
            CreatedAt = Clock.Now,
            UpdatedAt = Clock.Now,
            LastLoginAt = Clock.Now
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        await SendVerificationAsync(user, ct);
        return await IssueAsync(user, ct);
    }

    public async Task<IssuedTokens> SignInAsync(SignInRequest request, CancellationToken ct)
    {
        var login = request.Login.Trim().ToLowerInvariant();
        var user = await db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == login || u.Username == login, ct);
        if (user is null || user.PasswordHash is null || !hasher.Verify(request.Password, user.PasswordHash))
            throw ApiException.Unauthorized("E-mail/username or password is incorrect.");
        EnsureUsable(user);
        user.LastLoginAt = Clock.Now;
        await db.SaveChangesAsync(ct);
        return await IssueAsync(user, ct);
    }

    public async Task<IssuedTokens> GoogleSignInAsync(GoogleSignInRequest request, CancellationToken ct)
    {
        var clientId = site.Value.GoogleClientId;
        if (string.IsNullOrWhiteSpace(clientId)) throw ApiException.Unavailable("Google sign-in is not configured on this server.");
        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, new GoogleJsonWebSignature.ValidationSettings { Audience = [clientId] });
        }
        catch (InvalidJwtException ex)
        {
            logger.LogWarning(ex, "Google token validation failed");
            throw ApiException.Unauthorized("Google sign-in token is invalid or expired.");
        }
        if (!payload.EmailVerified) throw ApiException.Unauthorized("Your Google account e-mail is not verified.");

        var emailAddr = payload.Email.Trim().ToLowerInvariant();
        var external = await db.ExternalLogins.Include(e => e.User).ThenInclude(u => u.Role)
            .FirstOrDefaultAsync(e => e.Provider == "google" && e.ProviderUserId == payload.Subject, ct);
        User user;
        if (external is not null)
        {
            user = external.User;
        }
        else
        {
            user = await db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == emailAddr, ct) ?? await CreateGoogleUserAsync(payload, emailAddr, request.Locale, ct);
            db.ExternalLogins.Add(new ExternalLogin { UserId = user.Id, Provider = "google", ProviderUserId = payload.Subject, Email = emailAddr, CreatedAt = Clock.Now });
            user.EmailVerifiedAt ??= Clock.Now;
        }
        EnsureUsable(user);
        if (user.AvatarUrl is null && !string.IsNullOrWhiteSpace(payload.Picture)) user.AvatarUrl = payload.Picture;
        user.LastLoginAt = Clock.Now;
        await db.SaveChangesAsync(ct);
        return await IssueAsync(user, ct);
    }

    private async Task<User> CreateGoogleUserAsync(GoogleJsonWebSignature.Payload payload, string emailAddr, string? locale, CancellationToken ct)
    {
        var role = await db.Roles.FirstAsync(r => r.Name == ReferenceDataSeeder.MemberRoleName, ct);
        var baseName = TextUtil.Slugify(payload.Name ?? emailAddr.Split('@')[0]).Replace('-', '_');
        if (baseName.Length < 3) baseName = "user";
        var username = baseName;
        var i = 1;
        while (await db.Users.AnyAsync(u => u.Username == username, ct) || ReservedUsernames.Contains(username)) username = $"{baseName}{++i}";
        var user = new User
        {
            Email = emailAddr,
            EmailVerifiedAt = Clock.Now,
            Username = username,
            DisplayName = payload.Name ?? username,
            AvatarUrl = payload.Picture,
            RoleId = role.Id,
            Role = role,
            Locale = NormalizeLocale(locale),
            IsActive = true,
            CreatedAt = Clock.Now,
            UpdatedAt = Clock.Now
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return user;
    }

    private static void EnsureUsable(User user)
    {
        if (!user.IsActive) throw ApiException.Forbidden("This account is disabled.");
        if (user.IsBanned) throw ApiException.Forbidden("This account has been suspended." + (string.IsNullOrEmpty(user.BanReason) ? string.Empty : " Reason: " + user.BanReason));
    }

    // ---------------------------------------------------------------- refresh tokens

    private async Task<IssuedTokens> IssueAsync(User user, CancellationToken ct)
    {
        if (user.Role is null) await db.Entry(user).Reference(u => u.Role).LoadAsync(ct);
        var (access, expires) = tokens.CreateAccessToken(user);
        var refresh = tokens.NewRefreshToken();
        var refreshExpires = Clock.Now.AddDays(jwt.Value.RefreshTokenDays);
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = JwtTokenService.HashToken(refresh),
            ExpiresAt = refreshExpires,
            CreatedAt = Clock.Now,
            CreatedIp = currentUser.IpAddress,
            UserAgent = TextUtil.Truncate(currentUser.UserAgent, 500)
        });
        await db.SaveChangesAsync(ct);
        return new IssuedTokens(new AuthResponse { Token = access, TokenExpireDate = expires, User = await GetMeAsync(user.Id, ct) }, refresh, refreshExpires);
    }

    public async Task<IssuedTokens> RefreshAsync(string? refreshToken, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) throw ApiException.Unauthorized("No session. Please sign in.");
        var hash = JwtTokenService.HashToken(refreshToken);
        var row = await db.RefreshTokens.Include(r => r.User).ThenInclude(u => u.Role).FirstOrDefaultAsync(r => r.TokenHash == hash, ct);
        if (row is null) throw ApiException.Unauthorized("Session not found. Please sign in.");
        if (row.RevokedAt is not null)
        {
            // Reuse of a rotated token: someone replayed it — revoke the whole chain for safety.
            await db.RefreshTokens.Where(r => r.UserId == row.UserId && r.RevokedAt == null).ExecuteUpdateAsync(s => s.SetProperty(r => r.RevokedAt, Clock.Now), ct);
            throw ApiException.Unauthorized("Session was revoked. Please sign in again.");
        }
        if (row.ExpiresAt <= Clock.Now) throw ApiException.Unauthorized("Session expired. Please sign in again.");
        EnsureUsable(row.User);

        var newRefresh = tokens.NewRefreshToken();
        var newHash = JwtTokenService.HashToken(newRefresh);
        row.RevokedAt = Clock.Now;
        row.ReplacedByTokenHash = newHash;
        row.LastUsedAt = Clock.Now;
        var refreshExpires = Clock.Now.AddDays(jwt.Value.RefreshTokenDays);
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = row.UserId, TokenHash = newHash, ExpiresAt = refreshExpires, CreatedAt = Clock.Now,
            CreatedIp = currentUser.IpAddress, UserAgent = TextUtil.Truncate(currentUser.UserAgent, 500)
        });
        var (access, expires) = tokens.CreateAccessToken(row.User);
        await db.SaveChangesAsync(ct);
        return new IssuedTokens(new AuthResponse { Token = access, TokenExpireDate = expires, User = await GetMeAsync(row.UserId, ct) }, newRefresh, refreshExpires);
    }

    public async Task SignOutAsync(string? refreshToken, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return;
        var hash = JwtTokenService.HashToken(refreshToken);
        await db.RefreshTokens.Where(r => r.TokenHash == hash && r.RevokedAt == null).ExecuteUpdateAsync(s => s.SetProperty(r => r.RevokedAt, Clock.Now), ct);
    }

    public async Task<List<SessionDto>> GetSessionsAsync(string? currentRefreshToken, CancellationToken ct)
    {
        var currentHash = currentRefreshToken is null ? null : JwtTokenService.HashToken(currentRefreshToken);
        var now = Clock.Now;
        return await db.RefreshTokens.AsNoTracking()
            .Where(r => r.UserId == currentUser.Id && r.RevokedAt == null && r.ExpiresAt > now)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new SessionDto
            {
                Id = r.Id, CreatedAt = r.CreatedAt, LastUsedAt = r.LastUsedAt, ExpiresAt = r.ExpiresAt, Ip = r.CreatedIp, UserAgent = r.UserAgent,
                IsCurrent = currentHash != null && r.TokenHash == currentHash
            }).ToListAsync(ct);
    }

    public async Task RevokeSessionAsync(long id, CancellationToken ct)
    {
        await db.RefreshTokens.Where(r => r.Id == id && r.UserId == currentUser.Id).ExecuteUpdateAsync(s => s.SetProperty(r => r.RevokedAt, Clock.Now), ct);
    }

    public async Task RevokeAllSessionsAsync(CancellationToken ct)
    {
        await db.RefreshTokens.Where(r => r.UserId == currentUser.Id && r.RevokedAt == null).ExecuteUpdateAsync(s => s.SetProperty(r => r.RevokedAt, Clock.Now), ct);
    }

    // ---------------------------------------------------------------- me / profile

    public async Task<MeDto> GetMeAsync(int userId, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId, ct)
                   ?? throw ApiException.Unauthorized("Account not found.");
        var unread = await db.Notifications.CountAsync(n => n.UserId == userId && n.ReadAt == null, ct);
        var appCount = await db.Apps.CountAsync(a => a.UploaderUserId == userId && a.Status == AppStatus.Published, ct);
        var pending = await db.Apps.CountAsync(a => a.UploaderUserId == userId && (a.Status == AppStatus.PendingReview || a.Status == AppStatus.PendingScan), ct);
        var favorites = await db.Favorites.CountAsync(f => f.UserId == userId, ct);
        var hasGoogle = await db.ExternalLogins.AnyAsync(e => e.UserId == userId && e.Provider == "google", ct);
        return new MeDto
        {
            Id = user.Id, Email = user.Email, EmailVerified = user.EmailVerifiedAt != null, Username = user.Username, DisplayName = user.DisplayName,
            AvatarUrl = user.AvatarUrl, Bio = user.Bio, Website = user.Website, Role = user.Role.Name, IsAdmin = user.Role.IsAdmin, TrustLevel = user.TrustLevel,
            Locale = user.Locale, HasPassword = user.PasswordHash != null, HasGoogle = hasGoogle, UnreadNotificationCount = unread,
            AppCount = appCount, PendingAppCount = pending, FavoriteCount = favorites, CreatedAt = user.CreatedAt
        };
    }

    public async Task<MeDto> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct)
    {
        var user = await db.Users.FirstAsync(u => u.Id == currentUser.Id, ct);
        user.DisplayName = request.DisplayName.Trim();
        user.Bio = string.IsNullOrWhiteSpace(request.Bio) ? null : request.Bio.Trim();
        user.Website = string.IsNullOrWhiteSpace(request.Website) ? null : request.Website.Trim();
        if (user.Website is not null && !Uri.TryCreate(user.Website, UriKind.Absolute, out _)) throw ApiException.Unprocessable("Website must be an absolute URL.", "website");
        if (request.Locale is not null) user.Locale = NormalizeLocale(request.Locale);
        user.UpdatedAt = Clock.Now;
        await db.SaveChangesAsync(ct);
        return await GetMeAsync(user.Id, ct);
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct)
    {
        var user = await db.Users.FirstAsync(u => u.Id == currentUser.Id, ct);
        if (user.PasswordHash is not null)
        {
            if (string.IsNullOrEmpty(request.CurrentPassword) || !hasher.Verify(request.CurrentPassword, user.PasswordHash))
                throw ApiException.Unprocessable("Current password is incorrect.", "currentPassword");
        }
        user.PasswordHash = hasher.Hash(request.NewPassword);
        user.UpdatedAt = Clock.Now;
        await db.SaveChangesAsync(ct);
    }

    // ---------------------------------------------------------------- e-mail verification / password reset

    public async Task ResendVerificationAsync(CancellationToken ct)
    {
        var user = await db.Users.FirstAsync(u => u.Id == currentUser.Id, ct);
        if (user.EmailVerifiedAt is not null) return;
        await SendVerificationAsync(user, ct);
    }

    private async Task SendVerificationAsync(User user, CancellationToken ct)
    {
        var token = TextUtil.RandomToken();
        db.EmailTokens.Add(new EmailToken { UserId = user.Id, Kind = EmailTokenKind.VerifyEmail, TokenHash = TextUtil.Sha256Hex(token), ExpiresAt = Clock.Now.AddDays(3), CreatedAt = Clock.Now });
        await db.SaveChangesAsync(ct);
        var link = $"{site.Value.PublicUrl}/verify-email?token={token}";
        await SafeSend(user.Email, $"Verify your e-mail — {site.Value.Name}",
            $"<p>Hi {user.DisplayName},</p><p>Confirm your e-mail address by opening this link:</p><p><a href=\"{link}\">{link}</a></p><p>The link is valid for 3 days.</p>",
            $"Confirm your e-mail: {link}", ct);
    }

    public async Task VerifyEmailAsync(VerifyEmailRequest request, CancellationToken ct)
    {
        var row = await FindToken(request.Token, EmailTokenKind.VerifyEmail, ct);
        row.UsedAt = Clock.Now;
        row.User.EmailVerifiedAt ??= Clock.Now;
        await db.SaveChangesAsync(ct);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct)
    {
        var emailAddr = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == emailAddr, ct);
        if (user is null) return; // do not reveal whether the address exists
        var token = TextUtil.RandomToken();
        db.EmailTokens.Add(new EmailToken { UserId = user.Id, Kind = EmailTokenKind.ResetPassword, TokenHash = TextUtil.Sha256Hex(token), ExpiresAt = Clock.Now.AddHours(2), CreatedAt = Clock.Now });
        await db.SaveChangesAsync(ct);
        var link = $"{site.Value.PublicUrl}/reset-password?token={token}";
        await SafeSend(user.Email, $"Reset your password — {site.Value.Name}",
            $"<p>Hi {user.DisplayName},</p><p>Set a new password here (valid for 2 hours):</p><p><a href=\"{link}\">{link}</a></p><p>If you did not ask for this, ignore this e-mail.</p>",
            $"Reset your password: {link}", ct);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct)
    {
        var row = await FindToken(request.Token, EmailTokenKind.ResetPassword, ct);
        row.UsedAt = Clock.Now;
        row.User.PasswordHash = hasher.Hash(request.Password);
        row.User.EmailVerifiedAt ??= Clock.Now;
        row.User.UpdatedAt = Clock.Now;
        await db.RefreshTokens.Where(r => r.UserId == row.UserId && r.RevokedAt == null).ExecuteUpdateAsync(s => s.SetProperty(r => r.RevokedAt, Clock.Now), ct);
        await db.SaveChangesAsync(ct);
    }

    private async Task<EmailToken> FindToken(string token, EmailTokenKind kind, CancellationToken ct)
    {
        var hash = TextUtil.Sha256Hex(token.Trim());
        var row = await db.EmailTokens.Include(t => t.User).FirstOrDefaultAsync(t => t.TokenHash == hash && t.Kind == kind, ct);
        if (row is null || row.UsedAt is not null || row.ExpiresAt <= Clock.Now)
            throw ApiException.Unprocessable("This link is invalid or has expired.", "token");
        return row;
    }

    private async Task SafeSend(string to, string subject, string html, string text, CancellationToken ct)
    {
        try { await email.SendAsync(to, subject, html, text, ct); }
        catch (Exception ex) { logger.LogError(ex, "Could not send e-mail to {To}", to); }
    }

    public static string NormalizeLocale(string? locale) => locale?.StartsWith("tr", StringComparison.OrdinalIgnoreCase) == true ? "tr-TR" : "en-US";
}
