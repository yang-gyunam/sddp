using System.Text.RegularExpressions;
using MediatR;
using Microsoft.Extensions.Logging;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Auth.Commands;

/// <summary>
/// user log
/// </summary>
public sealed record LoginCommand(LoginRequest Request) :
    ICommand<LoginResponse>,
    INonTransactionalCommand,
    IAuditableRequest<LoginResponse>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(LoginResponse response) => AuditLog;
}

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private const string DummyPasswordHash =
        "$2a$12$dgwRnMdTgpQMi9gX66rktOBFcocw0oBwBdxE7jxwlUK1qiiDvaWci";

    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtService _jwtService;
    private readonly IRefreshTokenStore _refreshTokenStore;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<LoginCommandHandler> _logger;
    private readonly LoginSecurityPolicy _securityPolicy;

    public LoginCommandHandler(
        IUnitOfWork unitOfWork,
        IJwtService jwtService,
        IRefreshTokenStore refreshTokenStore,
        IPasswordHasher passwordHasher,
        ILogger<LoginCommandHandler> logger,
        LoginSecurityPolicy securityPolicy)
    {
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
        _refreshTokenStore = refreshTokenStore;
        _passwordHasher = passwordHasher;
        _logger = logger;
        _securityPolicy = securityPolicy;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var loginRequest = request.Request;
        var userRepo = _unitOfWork.Repository<User>();
        var users = await (userRepo.FindAsync(u => u.Username == loginRequest.Username, cancellationToken)).ConfigureAwait(false);
        var user = users.FirstOrDefault();

        if (user is null)
        {
            _passwordHasher.Verify(loginRequest.Password, DummyPasswordHash);
            _logger.LogWarning("Login failed: User not found - {Username}", loginRequest.Username);
            throw new UnauthorizedException("Invalid username or password");
        }

        if (!user.IsActive || user.IsAI)
        {
            _passwordHasher.Verify(loginRequest.Password, DummyPasswordHash);
            _logger.LogWarning("Login failed: Account is not eligible - {Username}", loginRequest.Username);
            throw new UnauthorizedException("Invalid username or password");
        }

        var attemptedAt = Timestamp.Now;
        user.ResetExpiredLoginLockout(attemptedAt);

        if (user.IsLoginLocked(attemptedAt))
        {
            _passwordHasher.Verify(loginRequest.Password, DummyPasswordHash);
            _logger.LogWarning("Login failed: Account is temporarily locked - {Username}", loginRequest.Username);
            throw new UnauthorizedException("Invalid username or password");
        }

        if (!_passwordHasher.Verify(loginRequest.Password, user.PasswordHash))
        {
            user.RecordFailedLogin(
                attemptedAt,
                _securityPolicy.MaxFailedAttempts,
                _securityPolicy.LockoutDuration);
            await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);
            _logger.LogWarning("Login failed: Invalid password - {Username}", loginRequest.Username);
            throw new UnauthorizedException("Invalid username or password");
        }

        var allUserRoles = await (AuthHelpers.GetUserRolesEntitiesAsync(
            _unitOfWork, user.Id, cancellationToken)).ConfigureAwait(false);
        var resolvedTenantId = AuthHelpers.ResolveTenantId(allUserRoles, loginRequest.TenantId);
        var tenantRoles = allUserRoles
            .Where(role => role.TenantId.HasValue && role.TenantId.Value == resolvedTenantId)
            .ToList();
        var roles = await (AuthHelpers.ResolveRoleNamesAsync(
            _unitOfWork, tenantRoles, cancellationToken)).ConfigureAwait(false);
        var permissions = await (AuthHelpers.GetUserPermissionsAsync(
            _unitOfWork, tenantRoles, cancellationToken)).ConfigureAwait(false);

        var accessToken = _jwtService.GenerateAccessToken(
            user.Id, user.Username, resolvedTenantId, user.DisplayName);
        var refreshToken = _jwtService.GenerateRefreshToken();
        var refreshTokenTtl = TimeSpan.FromDays(_jwtService.RefreshTokenExpirationDays);

        await (_refreshTokenStore.StoreAsync(user.Id, refreshToken, refreshTokenTtl, cancellationToken, resolvedTenantId)).ConfigureAwait(false);

        user.RecordLogin(attemptedAt);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        _logger.LogInformation("User logged in successfully: {Username}", loginRequest.Username);

        var userInfo = AuthHelpers.BuildUserInfo(user, resolvedTenantId, roles, permissions);

        request.AuditLog = new AuditLogRequest(
            ActorId: user.Id,
            Action: "login",
            ResourceType: "auth",
            ResourceId: user.Id,
            Payload: new { Username = request.Request.Username },
            TenantId: null,
            ProjectId: null);

        return new LoginResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            ExpiresIn: _jwtService.AccessTokenExpirationMinutes * 60,
            User: userInfo,
            RequirePasswordChange: user.RequirePasswordChange);
    }
}

public sealed record RefreshTokenCommand(string RefreshToken)
    : ICommand<TokenResponse>, INonTransactionalCommand, IAuditableRequest<TokenResponse>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(TokenResponse response) => AuditLog;
}

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, TokenResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtService _jwtService;
    private readonly IRefreshTokenStore _refreshTokenStore;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IUnitOfWork unitOfWork,
        IJwtService jwtService,
        IRefreshTokenStore refreshTokenStore,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
        _refreshTokenStore = refreshTokenStore;
        _logger = logger;
    }

    public async Task<TokenResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new UnauthorizedException("Refresh token is required");
        }

        var session = await (_refreshTokenStore.GetSessionAsync(request.RefreshToken, cancellationToken)).ConfigureAwait(false);
        if (session is null)
        {
            throw new UnauthorizedException("Invalid refresh token");
        }

        var userRepo = _unitOfWork.Repository<User>();
        var user = await (userRepo.GetByIdAsync(session.UserId, cancellationToken)).ConfigureAwait(false);

        // A rejection is a decision, not a failure, so the token is destroyed here on purpose —
        // unlike a transient error, retrying will never make an ineligible account eligible. The
        // reordering above is about not destroying it when the answer is "try again later".
        if (user is null)
        {
            await (_refreshTokenStore.RemoveAsync(request.RefreshToken, cancellationToken)).ConfigureAwait(false);
            throw new UnauthorizedException("Invalid refresh token");
        }

        if (!user.IsActive || user.IsAI)
        {
            _logger.LogWarning(
                "Refresh rejected: Account is not eligible - {Username}",
                user.Username);
            await (_refreshTokenStore.RemoveAsync(request.RefreshToken, cancellationToken)).ConfigureAwait(false);
            throw new UnauthorizedException("Invalid refresh token");
        }

        var allUserRoles = await (AuthHelpers.GetUserRolesEntitiesAsync(
            _unitOfWork, user.Id, cancellationToken)).ConfigureAwait(false);
        var resolvedTenantId = session.TenantId;
        if (!allUserRoles.Any(role => role.TenantId == resolvedTenantId))
        {
            await _refreshTokenStore.RemoveAsync(request.RefreshToken, cancellationToken).ConfigureAwait(false);
            throw new UnauthorizedException("Session tenant membership is no longer active");
        }
        var tenantRoles = allUserRoles
            .Where(role => role.TenantId.HasValue && role.TenantId.Value == resolvedTenantId)
            .ToList();
        var roles = await (AuthHelpers.ResolveRoleNamesAsync(
            _unitOfWork, tenantRoles, cancellationToken)).ConfigureAwait(false);
        var permissions = await (AuthHelpers.GetUserPermissionsAsync(
            _unitOfWork, tenantRoles, cancellationToken)).ConfigureAwait(false);

        var newAccessToken = _jwtService.GenerateAccessToken(
            user.Id, user.Username, resolvedTenantId, user.DisplayName);
        var newRefreshToken = _jwtService.GenerateRefreshToken();
        var refreshTokenTtl = TimeSpan.FromDays(_jwtService.RefreshTokenExpirationDays);

        // Validate generation and replace in one Redis operation. A revoke cannot fall between
        // consumption and storage and accidentally attach the replacement to a new generation.
        var rotated = await (_refreshTokenStore.RotateAsync(user.Id, request.RefreshToken,
            newRefreshToken, refreshTokenTtl, cancellationToken, resolvedTenantId)).ConfigureAwait(false);
        if (!rotated)
        {
            throw new UnauthorizedException("Invalid refresh token");
        }

        _logger.LogInformation("Token refreshed for user: {Username}", user.Username);

        request.AuditLog = new AuditLogRequest(
            ActorId: user.Id,
            Action: "refresh_token",
            ResourceType: "auth",
            ResourceId: user.Id,
            Payload: new { Username = user.Username },
            TenantId: null,
            ProjectId: null);

        var userInfo = AuthHelpers.BuildUserInfo(user, resolvedTenantId, roles, permissions);

        return new TokenResponse(
            AccessToken: newAccessToken,
            RefreshToken: newRefreshToken,
            ExpiresIn: _jwtService.AccessTokenExpirationMinutes * 60,
            User: userInfo,
            RequirePasswordChange: user.RequirePasswordChange);
    }
}

/// <summary>
/// user log ()
/// </summary>
public sealed record LogoutCommand(string? RefreshToken) : ICommand<bool>, IAuditableRequest<bool>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(bool response) => AuditLog;
}

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand, bool>
{
    private readonly IRefreshTokenStore _refreshTokenStore;

    public LogoutCommandHandler(IRefreshTokenStore refreshTokenStore)
    {
        _refreshTokenStore = refreshTokenStore;
    }

    public async Task<bool> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        GlobalUniqueId? logoutUserId = null;

        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            logoutUserId = await (_refreshTokenStore.GetUserIdAsync(request.RefreshToken, cancellationToken)).ConfigureAwait(false);
            await (_refreshTokenStore.RemoveAsync(request.RefreshToken, cancellationToken)).ConfigureAwait(false);
        }

        request.AuditLog = new AuditLogRequest(
            ActorId: logoutUserId,
            Action: "logout",
            ResourceType: "auth",
            ResourceId: logoutUserId ?? GlobalUniqueId.NewId(),
            Payload: null,
            TenantId: null,
            ProjectId: null);

        return true;
    }
}

// ============================================================================
// ChangePassword (self-service)
// ============================================================================

/// <summary>
/// change ()
/// </summary>
public sealed record ChangePasswordCommand(
    GlobalUniqueId UserId,
    ChangePasswordRequest Request) : ICommand<bool>, IAuditableRequest<bool>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(bool response) => AuditLog;
}

public sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<ChangePasswordCommandHandler> _logger;

    public ChangePasswordCommandHandler(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ILogger<ChangePasswordCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<bool> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var userRepo = _unitOfWork.Repository<User>();
        var user = await (userRepo.GetByIdAsync(request.UserId, cancellationToken)).ConfigureAwait(false);

        if (user is null)
        {
            throw new NotFoundException("User", (Guid)request.UserId);
        }

        if (!_passwordHasher.Verify(request.Request.CurrentPassword, user.PasswordHash))
        {
            throw new UnauthorizedException("Current password is incorrect");
        }

        ValidatePasswordStrength(request.Request.NewPassword);

        user.UpdatePassword(_passwordHasher.Hash(request.Request.NewPassword));
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        _logger.LogInformation("Password changed for user: {Username}", user.Username);

        request.AuditLog = new AuditLogRequest(
            ActorId: request.UserId,
            Action: "change_password",
            ResourceType: "auth",
            ResourceId: request.UserId,
            Payload: new { Username = user.Username },
            TenantId: null,
            ProjectId: null);

        return true;
    }

    private static void ValidatePasswordStrength(string password)
    {
        var errors = new List<string>();

        if (password.Length < 8)
            errors.Add("Password must be at least 8 characters");
        if (!Regex.IsMatch(password, "[A-Z]"))
            errors.Add("Password must contain at least one uppercase letter");
        if (!Regex.IsMatch(password, "[a-z]"))
            errors.Add("Password must contain at least one lowercase letter");
        if (!Regex.IsMatch(password, "[0-9]"))
            errors.Add("Password must contain at least one digit");
        if (!Regex.IsMatch(password, @"[^a-zA-Z0-9]"))
            errors.Add("Password must contain at least one special character");

        if (errors.Count > 0)
        {
            throw new ValidationException(string.Join("; ", errors));
        }
    }
}
