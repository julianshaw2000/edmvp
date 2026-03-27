# Entra External ID → ASP.NET Core Identity Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace Entra External ID (MSAL/Microsoft.Identity.Web) with ASP.NET Core Identity using self-issued JWT access tokens + HttpOnly refresh token cookie, adding password reset and email confirmation flows.

**Architecture:** The API becomes the identity provider using ASP.NET Core Identity with EF Core on the existing PostgreSQL database. Login returns a short-lived JWT access token (15 min) in the response body; the frontend stores it in memory (signal) and attaches it as `Authorization: Bearer` header. A long-lived refresh token is set as an HttpOnly secure cookie on `/api/auth/refresh` only — no cross-origin cookie issues since only the refresh endpoint needs it. The existing `UserEntity` stays but gains a link to `AspNetUsers` via `IdentityUserId`. Identity handles password hashing, email tokens, lockout. The app's role system (`SUPPLIER`, `BUYER`, `PLATFORM_ADMIN`, `TENANT_ADMIN`) stays in `UserEntity.Role` — we do NOT use ASP.NET Identity roles.

**Tech Stack:** ASP.NET Core Identity (no Identity UI/Razor Pages), `System.IdentityModel.Tokens.Jwt`, EF Core, Resend (existing email service), Angular 21 standalone components.

---

## Current State Summary

### Backend (API)
- **Auth:** `Microsoft.Identity.Web` v4.6.0 validates Entra CIAM JWT tokens
- **User identity column:** `UserEntity.EntraOid` (string, max 200, unique index) — maps Entra OID to app user
- **Role resolution:** `ICurrentUserService` reads `oid` claim from JWT → looks up `UserEntity` by `EntraOid`
- **Authorization:** Custom `RoleAuthorizationHandler` + `TenantAccessHandler` using `ICurrentUserService`
- **API key auth:** `ApiKeyMiddleware` sets synthetic `ClaimsPrincipal` with `oid` claim
- **Invited users:** Created with `EntraOid = "pending|{guid}"`, linked on first login via `/api/me`
- **Signup:** Stripe webhook creates tenant + user with `pending|` prefix

### Frontend (Web)
- **MSAL Angular:** `@azure/msal-angular` v3.1.0 + `@azure/msal-browser` v3.30.0
- **7 files** import MSAL: `app.config.ts`, `app.ts`, `auth.service.ts`, `auth.guard.ts`, `login.component.ts`, `environment.ts`, `environment.production.ts`
- **Auth flow:** MSAL redirect → `/login` → `loadProfile()` calls `/api/me` → navigate by role
- **Token attachment:** `MsalInterceptor` auto-attaches bearer tokens to API calls

---

## File Map

### Backend — New Files
- `packages/api/src/Tungsten.Api/Infrastructure/Identity/AppIdentityUser.cs` — custom IdentityUser with `AppUserId` FK
- `packages/api/src/Tungsten.Api/Infrastructure/Identity/IdentityDbContext.cs` — separate DbContext for Identity tables
- `packages/api/src/Tungsten.Api/Common/Auth/JwtTokenService.cs` — generates JWT access tokens + refresh tokens
- `packages/api/src/Tungsten.Api/Common/Auth/RefreshTokenEntity.cs` — persisted refresh tokens
- `packages/api/src/Tungsten.Api/Features/Auth/Login.cs` — POST `/api/auth/login` → returns JWT + sets refresh cookie
- `packages/api/src/Tungsten.Api/Features/Auth/Logout.cs` — POST `/api/auth/logout` → revokes refresh token
- `packages/api/src/Tungsten.Api/Features/Auth/RefreshToken.cs` — POST `/api/auth/refresh` → rotates tokens
- `packages/api/src/Tungsten.Api/Features/Auth/Register.cs` — POST `/api/auth/register`
- `packages/api/src/Tungsten.Api/Features/Auth/ForgotPassword.cs` — POST `/api/auth/forgot-password`
- `packages/api/src/Tungsten.Api/Features/Auth/ResetPassword.cs` — POST `/api/auth/reset-password`
- `packages/api/src/Tungsten.Api/Features/Auth/ConfirmEmail.cs` — GET `/api/auth/confirm-email`
- `packages/api/src/Tungsten.Api/Features/Auth/ResendConfirmation.cs` — POST `/api/auth/resend-confirmation`

### Backend — Modified Files
- `packages/api/src/Tungsten.Api/Tungsten.Api.csproj` — swap `Microsoft.Identity.Web` for Identity + JWT packages
- `packages/api/src/Tungsten.Api/Program.cs` — replace Entra auth with Identity + JWT bearer validation
- `packages/api/src/Tungsten.Api/Common/Auth/CurrentUserService.cs` — read `sub` claim from self-issued JWT
- `packages/api/src/Tungsten.Api/Common/Auth/ApiKeyMiddleware.cs` — update claim name
- `packages/api/src/Tungsten.Api/Common/Auth/RoleAuthorizationHandler.cs` — use `NameIdentifier` claim
- `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Entities/UserEntity.cs` — rename `EntraOid` → `IdentityUserId`
- `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Configurations/UserConfiguration.cs` — update column mapping
- `packages/api/src/Tungsten.Api/Infrastructure/Persistence/AppDbContext.cs` — add `RefreshTokens` DbSet
- `packages/api/src/Tungsten.Api/Features/Signup/StripeWebhookHandler.cs` — create Identity user on checkout
- `packages/api/src/Tungsten.Api/Features/Users/CreateUser.cs` — create Identity user when inviting
- EF migration for column rename + Identity tables + refresh tokens table

### Frontend — Modified Files
- `packages/web/package.json` — remove `@azure/msal-angular`, `@azure/msal-browser`
- `packages/web/src/environments/environment.ts` — remove `msal` config
- `packages/web/src/environments/environment.production.ts` — remove `msal` config
- `packages/web/src/app/app.config.ts` — remove all MSAL providers
- `packages/web/src/app/app.ts` — remove MSAL initialization
- `packages/web/src/app/core/auth/auth.service.ts` — replace MSAL with JWT token management
- `packages/web/src/app/core/auth/auth.guard.ts` — replace MsalGuard with token-aware guard
- `packages/web/src/app/features/auth/login.component.ts` — email/password login form
- `packages/web/src/app/core/http/error.interceptor.ts` — handle 401 with auto-refresh

### Frontend — New Files
- `packages/web/src/app/core/auth/token.interceptor.ts` — attaches Bearer token, auto-refreshes on 401
- `packages/web/src/app/features/auth/register.component.ts` — registration form
- `packages/web/src/app/features/auth/forgot-password.component.ts` — forgot password form
- `packages/web/src/app/features/auth/reset-password.component.ts` — reset password form

---

## Chunk 1: Backend Identity Infrastructure

### Task 1: Update NuGet packages

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Tungsten.Api.csproj`

- [ ] **Step 1: Swap packages**

Replace:
```xml
    <PackageReference Include="Microsoft.Identity.Web" Version="4.6.0" />
```
With:
```xml
    <PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="10.0.0" />
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.0" />
```

- [ ] **Step 2: Verify restore**

Run: `cd packages/api && dotnet restore`

---

### Task 2: Create Identity user and DbContext

**Files:**
- Create: `packages/api/src/Tungsten.Api/Infrastructure/Identity/AppIdentityUser.cs`
- Create: `packages/api/src/Tungsten.Api/Infrastructure/Identity/IdentityDbContext.cs`

- [ ] **Step 1: Create AppIdentityUser**

```csharp
using Microsoft.AspNetCore.Identity;

namespace Tungsten.Api.Infrastructure.Identity;

public class AppIdentityUser : IdentityUser
{
    public Guid? AppUserId { get; set; }
}
```

- [ ] **Step 2: Create IdentityDbContext**

```csharp
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Tungsten.Api.Infrastructure.Identity;

public class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : IdentityDbContext<AppIdentityUser>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("identity");
    }
}
```

---

### Task 3: Create JWT token service and refresh token entity

**Files:**
- Create: `packages/api/src/Tungsten.Api/Common/Auth/JwtTokenService.cs`
- Create: `packages/api/src/Tungsten.Api/Common/Auth/RefreshTokenEntity.cs`
- Create: `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Configurations/RefreshTokenConfiguration.cs`

- [ ] **Step 1: Create RefreshTokenEntity**

```csharp
namespace Tungsten.Api.Common.Auth;

public class RefreshTokenEntity
{
    public Guid Id { get; set; }
    public required string IdentityUserId { get; set; }
    public required string TokenHash { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRevoked { get; set; }
}
```

- [ ] **Step 2: Add DbSet to AppDbContext**

In `AppDbContext.cs`, add:
```csharp
public DbSet<RefreshTokenEntity> RefreshTokens => Set<RefreshTokenEntity>();
```
Add using: `using Tungsten.Api.Common.Auth;`

- [ ] **Step 3: Create RefreshTokenConfiguration**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tungsten.Api.Common.Auth;

namespace Tungsten.Api.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshTokenEntity>
{
    public void Configure(EntityTypeBuilder<RefreshTokenEntity> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.IdentityUserId).IsRequired().HasMaxLength(450);
        builder.Property(r => r.TokenHash).IsRequired().HasMaxLength(128);
        builder.HasIndex(r => r.TokenHash).IsUnique();
        builder.HasIndex(r => r.IdentityUserId);
        builder.Property(r => r.CreatedAt).HasDefaultValueSql("now()");
    }
}
```

- [ ] **Step 4: Create JwtTokenService**

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Common.Auth;

public interface IJwtTokenService
{
    string GenerateAccessToken(string identityUserId, string email);
    string GenerateRefreshToken();
    string HashToken(string token);
    Task<RefreshTokenEntity> SaveRefreshTokenAsync(string identityUserId, string refreshToken, CancellationToken ct);
    Task<RefreshTokenEntity?> ValidateRefreshTokenAsync(string refreshToken, CancellationToken ct);
    Task RevokeRefreshTokenAsync(string tokenHash, CancellationToken ct);
    Task RevokeAllUserTokensAsync(string identityUserId, CancellationToken ct);
}

public class JwtTokenService(IConfiguration config, AppDbContext db) : IJwtTokenService
{
    public string GenerateAccessToken(string identityUserId, string email)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, identityUserId),
            new Claim(ClaimTypes.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    public string HashToken(string token)
    {
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    public async Task<RefreshTokenEntity> SaveRefreshTokenAsync(string identityUserId, string refreshToken, CancellationToken ct)
    {
        var entity = new RefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            IdentityUserId = identityUserId,
            TokenHash = HashToken(refreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(14),
            CreatedAt = DateTime.UtcNow,
        };
        db.RefreshTokens.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<RefreshTokenEntity?> ValidateRefreshTokenAsync(string refreshToken, CancellationToken ct)
    {
        var hash = HashToken(refreshToken);
        return await db.RefreshTokens
            .FirstOrDefaultAsync(r => r.TokenHash == hash && !r.IsRevoked && r.ExpiresAt > DateTime.UtcNow, ct);
    }

    public async Task RevokeRefreshTokenAsync(string tokenHash, CancellationToken ct)
    {
        await db.RefreshTokens
            .Where(r => r.TokenHash == tokenHash)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.IsRevoked, true), ct);
    }

    public async Task RevokeAllUserTokensAsync(string identityUserId, CancellationToken ct)
    {
        await db.RefreshTokens
            .Where(r => r.IdentityUserId == identityUserId && !r.IsRevoked)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.IsRevoked, true), ct);
    }
}
```

---

### Task 4: Rename UserEntity.EntraOid → IdentityUserId

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Entities/UserEntity.cs`
- Modify: `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Configurations/UserConfiguration.cs`

- [ ] **Step 1: Rename property in UserEntity**

Change `public required string EntraOid { get; set; }` to `public required string IdentityUserId { get; set; }`

- [ ] **Step 2: Update column configuration**

Change `EntraOid` refs to `IdentityUserId`, column name to `identity_user_id`, max length to `450`.

- [ ] **Step 3: Find-and-replace all `EntraOid` references**

Replace `EntraOid` → `IdentityUserId` in every `.cs` file under `packages/api/src/Tungsten.Api/` (excluding `Migrations/`). ~30 files.

- [ ] **Step 4: Verify**

Run: `grep -rn "EntraOid" packages/api/src/Tungsten.Api/ --include="*.cs" | grep -v Migrations`
Expected: No results.

---

### Task 5: Rewrite CurrentUserService for self-issued JWT

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Common/Auth/CurrentUserService.cs`
- Modify: `packages/api/src/Tungsten.Api/Common/Auth/ApiKeyMiddleware.cs`

- [ ] **Step 1: Replace CurrentUserService**

```csharp
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Common.Auth;

public interface ICurrentUserService
{
    string IdentityUserId { get; }
    Task<Guid> GetUserIdAsync(CancellationToken ct);
    Task<Guid> GetTenantIdAsync(CancellationToken ct);
    Task<string> GetTenantStatusAsync(CancellationToken ct);
    Task<string> GetRoleAsync(CancellationToken ct);
}

public class CurrentUserService(IHttpContextAccessor httpContextAccessor, AppDbContext db) : ICurrentUserService
{
    private Guid? _userId;
    private Guid? _tenantId;
    private string? _role;
    private string? _tenantStatus;

    public string IdentityUserId =>
        httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("No authenticated user");

    public async Task<Guid> GetUserIdAsync(CancellationToken ct)
    {
        if (_userId.HasValue) return _userId.Value;
        await ResolveUserAsync(ct);
        return _userId!.Value;
    }

    public async Task<Guid> GetTenantIdAsync(CancellationToken ct)
    {
        if (_tenantId.HasValue) return _tenantId.Value;
        await ResolveUserAsync(ct);
        return _tenantId!.Value;
    }

    public async Task<string> GetTenantStatusAsync(CancellationToken ct)
    {
        if (_tenantStatus is not null) return _tenantStatus;
        await ResolveUserAsync(ct);
        return _tenantStatus!;
    }

    public async Task<string> GetRoleAsync(CancellationToken ct)
    {
        if (_role is not null) return _role;
        await ResolveUserAsync(ct);
        return _role!;
    }

    private async Task ResolveUserAsync(CancellationToken ct)
    {
        var identityUserId = IdentityUserId;
        var user = await db.Users.AsNoTracking()
            .Where(u => u.IdentityUserId == identityUserId && u.IsActive)
            .Join(db.Tenants, u => u.TenantId, t => t.Id,
                (u, t) => new { u.Id, u.TenantId, u.Role, TenantStatus = t.Status })
            .FirstOrDefaultAsync(ct)
            ?? throw new UnauthorizedAccessException("User not found");

        _userId = user.Id;
        _tenantId = user.TenantId;
        _role = user.Role;
        _tenantStatus = user.TenantStatus;
    }
}
```

- [ ] **Step 2: Update ApiKeyMiddleware claim**

Change to: `new Claim(ClaimTypes.NameIdentifier, key.CreatedBy.IdentityUserId),`

---

### Task 6: Wire up Identity + JWT bearer auth in Program.cs

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Program.cs`

- [ ] **Step 1: Update usings**

Remove `using Microsoft.Identity.Web;`. Keep `using Microsoft.AspNetCore.Authentication.JwtBearer;` and `using Microsoft.IdentityModel.Tokens;`. Add `using System.Text;`, `using Microsoft.AspNetCore.Identity;`, `using Tungsten.Api.Infrastructure.Identity;`.

- [ ] **Step 2: Replace Entra auth setup (lines 54-91)**

```csharp
// Identity
builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<AppIdentityUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutEnd = TimeSpan.FromMinutes(15);
})
.AddEntityFrameworkStores<IdentityDbContext>()
.AddDefaultTokenProviders();

// JWT bearer (self-issued tokens)
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.FromSeconds(30),
    };
});

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
```

- [ ] **Step 3: Simplify /api/me endpoint**

Remove Entra OID/Google/idp logic. Replace with simple mediator call.

- [ ] **Step 4: Update Sentry middleware**

Use `ClaimTypes.NameIdentifier` instead of Entra OID claim.

- [ ] **Step 5: Add JWT config to appsettings**

```json
"Jwt": {
  "Key": "CHANGE-ME-USE-64-CHAR-MIN-SECRET-IN-PRODUCTION-ENV-VAR",
  "Issuer": "tungsten-api",
  "Audience": "tungsten-web"
}
```

- [ ] **Step 6: Update CORS to allow credentials (for refresh cookie)**

```csharp
policy.WithOrigins(origins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials();
```

- [ ] **Step 7: Build and verify**

Run: `cd packages/api && dotnet build`

- [ ] **Step 8: Commit**

```bash
git add packages/api/
git commit -m "feat: replace Entra with Identity + self-issued JWT bearer auth"
```

---

## Chunk 2: Backend Auth Endpoints

### Task 7: Login endpoint

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/Auth/Login.cs`

- [ ] **Step 1: Create Login**

```csharp
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Identity;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Auth;

public static class Login
{
    public record Request(string Email, string Password);

    public static async Task<IResult> Handle(
        Request request,
        SignInManager<AppIdentityUser> signInManager,
        UserManager<AppIdentityUser> userManager,
        IJwtTokenService jwtTokenService,
        AppDbContext db,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var identityUser = await userManager.FindByEmailAsync(request.Email);
        if (identityUser is null)
            return TypedResults.Json(new { error = "Invalid email or password." }, statusCode: 401);

        if (!identityUser.EmailConfirmed)
            return TypedResults.Json(new { error = "Please confirm your email before signing in." }, statusCode: 401);

        var result = await signInManager.CheckPasswordSignInAsync(
            identityUser, request.Password, lockoutOnFailure: true);

        if (result.IsLockedOut)
            return TypedResults.Json(new { error = "Account locked. Try again in 15 minutes." }, statusCode: 429);

        if (!result.Succeeded)
            return TypedResults.Json(new { error = "Invalid email or password." }, statusCode: 401);

        var appUser = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.IdentityUserId == identityUser.Id && u.IsActive, ct);

        if (appUser is null)
            return TypedResults.Json(new { error = "No active account found." }, statusCode: 403);

        var accessToken = jwtTokenService.GenerateAccessToken(identityUser.Id, identityUser.Email!);
        var refreshToken = jwtTokenService.GenerateRefreshToken();
        await jwtTokenService.SaveRefreshTokenAsync(identityUser.Id, refreshToken, ct);

        httpContext.Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/api/auth/refresh",
            MaxAge = TimeSpan.FromDays(14),
        });

        return TypedResults.Ok(new { accessToken });
    }
}
```

---

### Task 8: Refresh token endpoint

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/Auth/RefreshToken.cs`

- [ ] **Step 1: Create RefreshToken**

```csharp
using Microsoft.AspNetCore.Identity;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Identity;

namespace Tungsten.Api.Features.Auth;

public static class RefreshToken
{
    public static async Task<IResult> Handle(
        HttpContext httpContext,
        IJwtTokenService jwtTokenService,
        UserManager<AppIdentityUser> userManager,
        CancellationToken ct)
    {
        var oldRefreshToken = httpContext.Request.Cookies["refresh_token"];
        if (string.IsNullOrEmpty(oldRefreshToken))
            return TypedResults.Json(new { error = "No refresh token." }, statusCode: 401);

        var stored = await jwtTokenService.ValidateRefreshTokenAsync(oldRefreshToken, ct);
        if (stored is null)
            return TypedResults.Json(new { error = "Invalid or expired refresh token." }, statusCode: 401);

        await jwtTokenService.RevokeRefreshTokenAsync(stored.TokenHash, ct);

        var identityUser = await userManager.FindByIdAsync(stored.IdentityUserId);
        if (identityUser is null)
            return TypedResults.Json(new { error = "User not found." }, statusCode: 401);

        var accessToken = jwtTokenService.GenerateAccessToken(identityUser.Id, identityUser.Email!);
        var newRefreshToken = jwtTokenService.GenerateRefreshToken();
        await jwtTokenService.SaveRefreshTokenAsync(identityUser.Id, newRefreshToken, ct);

        httpContext.Response.Cookies.Append("refresh_token", newRefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/api/auth/refresh",
            MaxAge = TimeSpan.FromDays(14),
        });

        return TypedResults.Ok(new { accessToken });
    }
}
```

---

### Task 9: Logout endpoint

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/Auth/Logout.cs`

- [ ] **Step 1: Create Logout**

```csharp
using Tungsten.Api.Common.Auth;

namespace Tungsten.Api.Features.Auth;

public static class Logout
{
    public static async Task<IResult> Handle(
        HttpContext httpContext,
        IJwtTokenService jwtTokenService,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        try
        {
            await jwtTokenService.RevokeAllUserTokensAsync(currentUser.IdentityUserId, ct);
        }
        catch (UnauthorizedAccessException) { }

        httpContext.Response.Cookies.Delete("refresh_token", new CookieOptions
        {
            Path = "/api/auth/refresh",
            Secure = true,
            SameSite = SameSiteMode.None,
        });

        return TypedResults.Ok(new { message = "Logged out." });
    }
}
```

---

### Task 10: ForgotPassword + ResetPassword

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/Auth/ForgotPassword.cs`
- Create: `packages/api/src/Tungsten.Api/Features/Auth/ResetPassword.cs`

- [ ] **Step 1: Create ForgotPassword**

```csharp
using Microsoft.AspNetCore.Identity;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Infrastructure.Identity;

namespace Tungsten.Api.Features.Auth;

public static class ForgotPassword
{
    public record Request(string Email);

    public static async Task<IResult> Handle(
        Request request,
        UserManager<AppIdentityUser> userManager,
        IEmailService emailService,
        IConfiguration config)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.EmailConfirmed)
            return TypedResults.Ok(new { message = "If that email exists, a reset link has been sent." });

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var baseUrl = config["BaseUrl"] ?? "https://auditraks.com";
        var resetUrl = $"{baseUrl}/reset-password?email={Uri.EscapeDataString(request.Email)}&token={Uri.EscapeDataString(token)}";

        var html = $"""
            <h2>Reset your password</h2>
            <p>Click the link below to reset your auditraks password. This link expires in 24 hours.</p>
            <p><a href="{resetUrl}">Reset Password</a></p>
            <p>If you didn't request this, ignore this email.</p>
            """;

        await emailService.SendAsync(request.Email, "Reset your auditraks password", html,
            $"Reset your password: {resetUrl}", CancellationToken.None);

        return TypedResults.Ok(new { message = "If that email exists, a reset link has been sent." });
    }
}
```

- [ ] **Step 2: Create ResetPassword**

```csharp
using Microsoft.AspNetCore.Identity;
using Tungsten.Api.Infrastructure.Identity;

namespace Tungsten.Api.Features.Auth;

public static class ResetPassword
{
    public record Request(string Email, string Token, string NewPassword);

    public static async Task<IResult> Handle(Request request, UserManager<AppIdentityUser> userManager)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return TypedResults.Json(new { error = "Invalid reset link." }, statusCode: 400);

        var result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
            return TypedResults.Json(new { error = string.Join(" ", result.Errors.Select(e => e.Description)) }, statusCode: 400);

        return TypedResults.Ok(new { message = "Password reset. You can now sign in." });
    }
}
```

---

### Task 11: ConfirmEmail + ResendConfirmation

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/Auth/ConfirmEmail.cs`
- Create: `packages/api/src/Tungsten.Api/Features/Auth/ResendConfirmation.cs`

- [ ] **Step 1: Create ConfirmEmail**

```csharp
using Microsoft.AspNetCore.Identity;
using Tungsten.Api.Infrastructure.Identity;

namespace Tungsten.Api.Features.Auth;

public static class ConfirmEmail
{
    public static async Task<IResult> Handle(
        string userId, string token,
        UserManager<AppIdentityUser> userManager, IConfiguration config)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return TypedResults.Json(new { error = "Invalid confirmation link." }, statusCode: 400);

        var result = await userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
            return TypedResults.Json(new { error = "Invalid or expired confirmation link." }, statusCode: 400);

        var baseUrl = config["BaseUrl"] ?? "https://auditraks.com";
        return TypedResults.Redirect($"{baseUrl}/login?emailConfirmed=true");
    }
}
```

- [ ] **Step 2: Create ResendConfirmation**

```csharp
using Microsoft.AspNetCore.Identity;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Infrastructure.Identity;

namespace Tungsten.Api.Features.Auth;

public static class ResendConfirmation
{
    public record Request(string Email);

    public static async Task<IResult> Handle(
        Request request, UserManager<AppIdentityUser> userManager,
        IEmailService emailService, IConfiguration config)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || user.EmailConfirmed)
            return TypedResults.Ok(new { message = "If that email exists and is unconfirmed, a new link has been sent." });

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var apiBaseUrl = config["ApiBaseUrl"] ?? "https://accutrac-api.onrender.com";
        var confirmUrl = $"{apiBaseUrl}/api/auth/confirm-email?userId={Uri.EscapeDataString(user.Id)}&token={Uri.EscapeDataString(token)}";

        var html = $"""
            <h2>Confirm your email</h2>
            <p>Click below to confirm your auditraks account.</p>
            <p><a href="{confirmUrl}">Confirm Email</a></p>
            """;

        await emailService.SendAsync(request.Email, "Confirm your auditraks email", html,
            $"Confirm: {confirmUrl}", CancellationToken.None);

        return TypedResults.Ok(new { message = "If that email exists and is unconfirmed, a new link has been sent." });
    }
}
```

---

### Task 12: Register endpoint

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/Auth/Register.cs`

- [ ] **Step 1: Create Register**

```csharp
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Infrastructure.Identity;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Auth;

public static class Register
{
    public record Request(string Email, string Password, string DisplayName);

    public static async Task<IResult> Handle(
        Request request, UserManager<AppIdentityUser> userManager,
        AppDbContext db, IEmailService emailService, IConfiguration config)
    {
        var appUser = await db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IdentityUserId.StartsWith("pending|"));

        if (appUser is null)
            return TypedResults.Json(new { error = "No invitation found for this email." }, statusCode: 400);

        var existing = await userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
            return TypedResults.Json(new { error = "An account with this email already exists." }, statusCode: 409);

        var identityUser = new AppIdentityUser
        {
            UserName = request.Email,
            Email = request.Email,
            AppUserId = appUser.Id,
        };

        var result = await userManager.CreateAsync(identityUser, request.Password);
        if (!result.Succeeded)
            return TypedResults.Json(new { error = string.Join(" ", result.Errors.Select(e => e.Description)) }, statusCode: 400);

        appUser.IdentityUserId = identityUser.Id;
        appUser.DisplayName = request.DisplayName;
        appUser.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var token = await userManager.GenerateEmailConfirmationTokenAsync(identityUser);
        var apiBaseUrl = config["ApiBaseUrl"] ?? "https://accutrac-api.onrender.com";
        var confirmUrl = $"{apiBaseUrl}/api/auth/confirm-email?userId={Uri.EscapeDataString(identityUser.Id)}&token={Uri.EscapeDataString(token)}";

        var html = $"""
            <h2>Confirm your email</h2>
            <p>Welcome to auditraks! Click below to confirm your email.</p>
            <p><a href="{confirmUrl}">Confirm Email</a></p>
            """;

        await emailService.SendAsync(request.Email, "Confirm your auditraks email", html,
            $"Confirm: {confirmUrl}", CancellationToken.None);

        return TypedResults.Ok(new { message = "Account created. Check your email to confirm." });
    }
}
```

---

### Task 13: Map auth endpoints + update StripeWebhookHandler

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Program.cs`
- Modify: `packages/api/src/Tungsten.Api/Features/Signup/StripeWebhookHandler.cs`

- [ ] **Step 1: Map auth endpoints**

```csharp
var auth = app.MapGroup("/api/auth").WithTags("Auth");
auth.MapPost("/login", Login.Handle).AllowAnonymous();
auth.MapPost("/refresh", RefreshToken.Handle).AllowAnonymous();
auth.MapPost("/logout", Logout.Handle).AllowAnonymous();
auth.MapPost("/register", Register.Handle).AllowAnonymous();
auth.MapPost("/forgot-password", ForgotPassword.Handle).AllowAnonymous().RequireRateLimiting("public");
auth.MapPost("/reset-password", ResetPassword.Handle).AllowAnonymous().RequireRateLimiting("public");
auth.MapGet("/confirm-email", ConfirmEmail.Handle).AllowAnonymous();
auth.MapPost("/resend-confirmation", ResendConfirmation.Handle).AllowAnonymous().RequireRateLimiting("public");
```

- [ ] **Step 2: Update StripeWebhookHandler to create Identity user**

Inject `UserManager<AppIdentityUser>`. Create Identity user on checkout instead of `pending|` prefix. Welcome email links to `/reset-password`.

- [ ] **Step 3: Build, commit**

```bash
cd packages/api && dotnet build
git add packages/api/
git commit -m "feat: auth endpoints — login, refresh, logout, register, password reset, email confirm"
```

---

### Task 14: EF Migrations

- [ ] **Step 1: Generate migrations**

```bash
cd packages/api/src/Tungsten.Api
dotnet ef migrations add AddIdentityTables --context IdentityDbContext
dotnet ef migrations add RenameEntraOidAndAddRefreshTokens --context AppDbContext
```

- [ ] **Step 2: Commit**

```bash
git add packages/api/ && git commit -m "feat: EF migrations for Identity tables, column rename, refresh tokens"
```

---

## Chunk 3: Frontend — Remove MSAL, Add JWT Auth

### Task 15: Remove MSAL, simplify config

- [ ] **Step 1:** `cd packages/web && npm uninstall @azure/msal-angular @azure/msal-browser`
- [ ] **Step 2:** Simplify `environment.ts` and `environment.production.ts` (remove `msal` block)
- [ ] **Step 3:** Rewrite `app.config.ts` — remove MSAL providers, use `provideHttpClient(withInterceptors([tokenInterceptor]))`
- [ ] **Step 4:** Simplify `app.ts` — remove MSAL init, just `<router-outlet />`

---

### Task 16: Rewrite AuthService with JWT management

**Files:**
- Modify: `packages/web/src/app/core/auth/auth.service.ts`

- [ ] **Step 1: Replace AuthService**

Key changes from MSAL version:
- `_accessToken` signal stores JWT in memory
- `login()` returns observable, caller stores token via `setAccessToken()`
- `refresh()` calls `/api/auth/refresh` with `withCredentials: true` (sends refresh cookie)
- `logout()` calls API then clears local state
- `loadProfile()` uses the token interceptor automatically
- No MSAL imports

---

### Task 17: Create token interceptor

**Files:**
- Create: `packages/web/src/app/core/auth/token.interceptor.ts`

- [ ] **Step 1: Create interceptor**

Attaches `Authorization: Bearer` header to API requests. On 401, attempts one refresh then retries. Skip URLs: `/api/auth/*`, `/api/signup/*`, `/api/public/*`, `/health`.

---

### Task 18: Rewrite auth guard

- [ ] **Step 1:** Replace MsalGuard with: check `isLoggedIn()` → try `refresh()` → `loadProfile()` → or redirect to `/login`

---

### Task 19: Rewrite login component

- [ ] **Step 1:** Email/password form. On submit: `auth.login()` → `auth.setAccessToken()` → `auth.loadProfile()` → navigate by role. Handle `?emailConfirmed=true` query param.

- [ ] **Step 2: Commit**

```bash
git add packages/web/ && git commit -m "feat: replace MSAL with JWT auth — token interceptor, login, guard"
```

---

## Chunk 4: Frontend — New Auth Pages + Routes

### Task 20: Forgot Password component

- [ ] **Step 1:** Create `forgot-password.component.ts` — email form, calls `auth.forgotPassword()`, shows "check your email"

### Task 21: Reset Password component

- [ ] **Step 1:** Create `reset-password.component.ts` — reads `email`+`token` from query params, password+confirm form

### Task 22: Register component

- [ ] **Step 1:** Create `register.component.ts` — email (prefilled), display name, password+confirm form

### Task 23: Add routes + cleanup

- [ ] **Step 1:** Add `/forgot-password`, `/reset-password`, `/register` routes to `app.routes.ts`
- [ ] **Step 2:** Remove or simplify `error.interceptor.ts` (token interceptor handles 401s now)
- [ ] **Step 3:** Build: `cd packages/web && npm run build`
- [ ] **Step 4:** Commit

```bash
git add packages/web/ && git commit -m "feat: add register, forgot-password, reset-password pages"
```

---

## Chunk 5: Cleanup & Final Verification

### Task 24: Update CLAUDE.md

- [ ] **Step 1:** Replace Auth section with: `ASP.NET Core Identity — self-issued JWT access tokens (15 min) + HttpOnly refresh token cookie. Password reset and email confirmation via Resend. Role resolved via /me endpoint.`

### Task 25: Full build + final commit

- [ ] **Step 1:** `cd packages/api && dotnet build && dotnet test`
- [ ] **Step 2:** `cd packages/web && npm run build`
- [ ] **Step 3:** Final commit

```bash
git add -A && git commit -m "feat: complete Entra → Identity Core migration with JWT, password reset, email confirmation"
```

---

## Configuration Reference

### Render Environment Variables

```
Jwt__Key=<64+ character random secret>
Jwt__Issuer=tungsten-api
Jwt__Audience=tungsten-web
```

### Token Lifecycle

| Token | Lifetime | Storage | Transport |
|-------|----------|---------|-----------|
| Access token | 15 min | Frontend memory (signal) | `Authorization: Bearer` header |
| Refresh token | 14 days | HttpOnly secure cookie | Auto-sent to `/api/auth/refresh` only |

### Why JWT instead of cookies

- Works on every browser, corporate network, mobile webview
- No cross-origin cookie issues (API and frontend on different domains)
- `Authorization: Bearer` header is CSRF-immune — no anti-forgery tokens needed
- Refresh cookie scoped to single path (`/api/auth/refresh`) — minimal surface area

### Data Migration (Production)

1. Run EF migrations (Identity tables + column rename + refresh tokens)
2. For each existing user with a real `EntraOid`: create `AspNetUsers` row, set `EmailConfirmed = true`
3. Send all existing users a "set your password" email via forgot-password flow
4. Update `identity_user_id` to point to `AspNetUsers.Id`
