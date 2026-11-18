using System.Security.Claims;
using System.Security.Cryptography;
using Mandeville.Library.Model;
using Mandeville.Library.Repository;
using Microsoft.AspNetCore.Identity;

namespace Mandeville.Web.Security;

/// <summary>Sign-in rules: password checks, lockout, hashing, claims and the very first administrator.</summary>
public class AccountService(IAppUserRepository userRepo, IRoleRepository roleRepo, IConfiguration configuration, ILogger<AccountService> logger)
{
    #region Private Fields

    private const int MaxAttempts = 5;
    private const int LockMinutes = 10;
    private const string Generic = "Incorrect email or password.";

    private readonly IAppUserRepository __userRepo = userRepo;
    private readonly IRoleRepository __roleRepo = roleRepo;
    private readonly IConfiguration __configuration = configuration;
    private readonly ILogger<AccountService> __logger = logger;
    private readonly PasswordHasher<AppUserModel> __hasher = new();

    #endregion

    #region Sign in

    /// <summary>Checks an email and password. Returns the account, or a message that is safe to show.</summary>
    public async Task<(AppUserModel? User, string? Error)> ValidateAsync(string? email, string? password, CancellationToken cancellationToken = default)
    {
        email = email?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            return (null, Generic);

        var user = (await __userRepo.SelectAppUser(email: email, cancellationToken: cancellationToken)).FirstOrDefault();

        if (user is null)
        {
            // Same work as a real check so the response time does not reveal which emails exist
            __hasher.VerifyHashedPassword(new AppUserModel(), __hasher.HashPassword(new AppUserModel(), "x"), password);
            return (null, Generic);
        }

        if (!user.fblnActive)
            return (null, Generic);

        if (user.IsLocked)
            return (null, $"Too many failed attempts. Try again in about {LockMinutes} minutes.");

        var result = __hasher.VerifyHashedPassword(user, user.fstrPasswordHash, password);
        if (result == PasswordVerificationResult.Failed)
        {
            await __userRepo.RecordSignIn(user.flngKey, false, MaxAttempts, LockMinutes, cancellationToken);
            return (null, Generic);
        }

        await __userRepo.RecordSignIn(user.flngKey, true, MaxAttempts, LockMinutes, cancellationToken);
        return (user, null);
    }

    /// <summary>
    /// The cookie identity for an account: who they are, plus the pages, tabs and "own records only" settings of their role.
    /// These are copied into the cookie (signed and encrypted) at sign-in, so nothing has to be looked up on every click.
    /// </summary>
    public async Task<ClaimsPrincipal> CreatePrincipalAsync(AppUserModel user, CancellationToken cancellationToken = default)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.flngKey.ToString()),
            new(ClaimTypes.Name, user.fstrDisplayName),
            new(ClaimTypes.Email, user.fstrEmail),
            new(ClaimTypes.Role, user.fstrRole),
        };
        if (user.flngMemberKey is Guid memberKey) claims.Add(new Claim(AppClaims.MemberKey, memberKey.ToString()));
        if (user.fblnMustChangePassword) claims.Add(new Claim(AppClaims.MustChange, "1"));

        var role = (await __roleRepo.SelectRole(name: user.fstrRole, cancellationToken: cancellationToken)).FirstOrDefault();
        claims.Add(new Claim(AppClaims.RoleLabel, role?.fstrLabel ?? AppRoles.Label(user.fstrRole)));

        // The Administrator is allowed everything by rule, so only other roles carry page claims
        if (user.fstrRole != AppRoles.Administrator)
        {
            var permissions = await __roleRepo.SelectRolePermission(roleName: user.fstrRole, cancellationToken: cancellationToken);
            claims.AddRange(AccessClaims.Build(permissions));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Cookies", ClaimTypes.Name, ClaimTypes.Role));
    }

    /// <summary>The first page a person lands on after signing in: the first page of the menu their role may open.</summary>
    public static string LandingFor(ClaimsPrincipal user)
    {
        if (user.IsInRole(AppRoles.Administrator)) return "/";

        var first = PageRegistry.All.FirstOrDefault(p => user.CanPage(p.Key));

        // No page at all: the one place everyone can open
        return first is null ? "/account/password" : "/" + first.Href;
    }

    /// <summary>Accepts only local, relative return addresses (stops "sign in then jump to another site").</summary>
    public static string? SafeReturnUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        url = url.Trim();
        if (!url.StartsWith('/')) url = "/" + url;
        if (url.StartsWith("//") || url.StartsWith("/\\") || url.Contains("://")) return null;
        if (url.StartsWith("/account", StringComparison.OrdinalIgnoreCase)) return null;
        return url;
    }

    #endregion

    #region Passwords

    /// <summary>Hashes a password for storage.</summary>
    public string HashPassword(string password) => __hasher.HashPassword(new AppUserModel(), password);

    /// <summary>Null when the password is acceptable, otherwise what is wrong with it.</summary>
    public static string? CheckPolicy(string? password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 8) return "Use at least 8 characters.";
        if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit)) return "Use letters and at least one number.";
        return null;
    }

    /// <summary>A random temporary password (no look-alike characters) that always passes the policy.</summary>
    public static string GeneratePassword(int length = 12)
    {
        const string letters = "abcdefghjkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ";
        const string digits = "23456789";
        var all = letters + digits;
        var chars = new char[length];
        for (var i = 0; i < length; i++) chars[i] = all[RandomNumberGenerator.GetInt32(all.Length)];
        chars[RandomNumberGenerator.GetInt32(length)] = letters[RandomNumberGenerator.GetInt32(letters.Length)];
        var d = RandomNumberGenerator.GetInt32(length);
        chars[d] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
        // make sure the letter written above was not overwritten by the digit
        if (!chars.Any(char.IsLetter)) chars[(d + 1) % length] = letters[RandomNumberGenerator.GetInt32(letters.Length)];
        return new string(chars);
    }

    /// <summary>Saves a new password. <paramref name="mustChange"/> forces the person to pick their own next time.</summary>
    public async Task<ToastMessageModel> SetPasswordAsync(AppUserModel user, string newPassword, bool mustChange, string? who, CancellationToken cancellationToken = default)
    {
        user.fstrPasswordHash = HashPassword(newPassword);
        user.fblnMustChangePassword = mustChange;
        user.fstrWho = who;
        return await __userRepo.AddOrUpdateAppUser(user, insert: false, cancellationToken);
    }

    #endregion

    #region First administrator

    /// <summary>
    /// When there are no accounts at all, creates the first administrator from appsettings
    /// (Security:FirstAdminEmail / Security:FirstAdminPassword). Without a password a random one is made and logged once.
    /// </summary>
    public async Task EnsureFirstAdminAsync(CancellationToken cancellationToken = default)
    {
        // When a system administrator secret is set, that account is the way in, so no extra first admin is made.
        if ((!string.IsNullOrWhiteSpace(__configuration["Security:SysAdmin:Password"]) && !string.IsNullOrWhiteSpace(__configuration["Security:SysAdmin:Email"]))
            || __configuration.GetSection("Security:SysAdmins").GetChildren().Any(c => !string.IsNullOrWhiteSpace(c["Password"]))) return;

        var existing = await __userRepo.SelectAppUser(cancellationToken: cancellationToken);
        if (existing.Count > 0) return;

        var email = (__configuration["Security:FirstAdminEmail"] ?? "admin@wafif.org").Trim().ToLowerInvariant();
        var password = __configuration["Security:FirstAdminPassword"];
        var generated = string.IsNullOrWhiteSpace(password) || CheckPolicy(password) is not null;
        if (generated) password = GeneratePassword();

        var admin = new AppUserModel
        {
            fstrEmail = email,
            fstrDisplayName = "Administrator",
            fstrRole = AppRoles.Administrator,
            fstrPasswordHash = HashPassword(password!),
            fblnMustChangePassword = true,
            fstrWho = "System",
        };

        var result = await __userRepo.AddOrUpdateAppUser(admin, insert: true, cancellationToken);
        if (result.ToastType == ToastTypeModel.Success)
        {
            if (generated)
                __logger.LogWarning("First administrator created. Sign in with {Email} and the one-time password {Password}, then choose your own.", email, password);
            else
                __logger.LogWarning("First administrator created. Sign in with {Email} and the password from Security:FirstAdminPassword, then choose your own.", email);
        }
        else
        {
            __logger.LogError("Could not create the first administrator: {Message}", result.Message);
        }
    }

    #endregion

    #region System administrators (kept in user secrets)

    /// <summary>
    /// Keeps the system administrator accounts in step with the secret settings. Use either one pair,
    /// Security:SysAdmin:Email and Security:SysAdmin:Password, or a list, Security:SysAdmins (each with Email and Password), or both.
    /// Passwords are never in code or appsettings.json: they come from user secrets while developing, or from the host's
    /// application settings once published. Runs at every start, so the secret is the source of truth
    /// (a changed secret changes the password, and a lockout is cleared).
    /// </summary>
    public async Task EnsureSysAdminAsync(CancellationToken cancellationToken = default)
    {
        var entries = new List<(string? Email, string? Password)>
        {
            (__configuration["Security:SysAdmin:Email"], __configuration["Security:SysAdmin:Password"])
        };
        foreach (var section in __configuration.GetSection("Security:SysAdmins").GetChildren())
            entries.Add((section["Email"], section["Password"]));

        foreach (var (email, password) in entries)
        {
            if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(password)) continue;
            await EnsureOneSysAdminAsync(email, password, cancellationToken);
        }
    }

    private async Task EnsureOneSysAdminAsync(string? rawEmail, string? password, CancellationToken cancellationToken)
    {
        var email = (rawEmail ?? string.Empty).Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(password) || !System.Net.Mail.MailAddress.TryCreate(email, out _))
        {
            __logger.LogError("A system administrator secret was not used. Each one needs a valid Email and a Password.");
            return;
        }

        var policy = CheckPolicy(password);
        if (policy is not null)
        {
            __logger.LogError("The system administrator secret for {Email} was not used. {Policy}", email, policy);
            return;
        }

        var user = (await __userRepo.SelectAppUser(email: email, cancellationToken: cancellationToken)).FirstOrDefault();

        if (user is null)
        {
            var created = await __userRepo.AddOrUpdateAppUser(new AppUserModel
            {
                fstrEmail = email,
                fstrDisplayName = "System administrator",
                fstrRole = AppRoles.Administrator,
                fstrPasswordHash = HashPassword(password),
                fblnMustChangePassword = false,
                fstrWho = "System",
            }, insert: true, cancellationToken);

            if (created.ToastType == ToastTypeModel.Success) __logger.LogInformation("System administrator {Email} created from the secret settings.", email);
            else __logger.LogError("Could not create the system administrator {Email}: {Message}", email, created.Message);
            return;
        }

        var samePassword = __hasher.VerifyHashedPassword(user, user.fstrPasswordHash, password) != PasswordVerificationResult.Failed;
        if (samePassword && user.fblnActive && user.fstrRole == AppRoles.Administrator && !user.fblnMustChangePassword && !user.IsLocked && user.flngFailedAttempts == 0)
            return;

        user.fstrPasswordHash = samePassword ? user.fstrPasswordHash : HashPassword(password);
        user.fstrRole = AppRoles.Administrator;
        user.fblnActive = true;
        user.fblnMustChangePassword = false;
        user.fstrWho = "System";

        var updated = await __userRepo.AddOrUpdateAppUser(user, insert: false, cancellationToken);
        if (updated.ToastType != ToastTypeModel.Success)
            __logger.LogError("Could not update the system administrator {Email}: {Message}", email, updated.Message);
    }

    #endregion
}
