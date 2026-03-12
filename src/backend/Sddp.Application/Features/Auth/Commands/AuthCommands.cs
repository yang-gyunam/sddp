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
public sealed record LoginCommand(LoginRequest Request) : ICommand<LoginResponse>, IAuditableRequest<LoginResponse>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(LoginResponse response) => AuditLog;
}

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtService _jwtService;
    private readonly IRefreshTokenStore _refreshTokenStore;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IUnitOfWork unitOfWork,
        IJwtService jwtService,
        IRefreshTokenStore refreshTokenStore,
        IPasswordHasher passwordHasher,
        ILogger<LoginCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
        _refreshTokenStore = refreshTokenStore;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var loginRequest = request.Request;
        var userRepo = _unitOfWork.Repository<User>();
        var users = await (userRepo.FindAsync(u => u.Username == loginRequest.Username, cancellationToken)).ConfigureAwait(false);
        var user = users.FirstOrDefault();

        if (user is null)
        {
            _logger.LogWarning("Login failed: User not found - {Username}", loginRequest.Username);
            throw new UnauthorizedException("Invalid username or password");
        }

        if (user.IsAI)
        {
            _logger.LogWarning("Login failed: AI account is not allowed - {Username}", loginRequest.Username);
            throw new UnauthorizedException("Invalid username or password");
        }

        if (!_passwordHasher.Verify(loginRequest.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed: Invalid password - {Username}", loginRequest.Username);
            throw new UnauthorizedException("Invalid username or password");
        }

        var userRoles = await (AuthHelpers.GetUserRolesEntitiesAsync(_unitOfWork, user.Id, cancellationToken)).ConfigureAwait(false);
        var roles = await (AuthHelpers.ResolveRoleNamesAsync(_unitOfWork, userRoles, cancellationToken)).ConfigureAwait(false);
        var permissions = await (AuthHelpers.GetUserPermissionsAsync(_unitOfWork, user.Id, cancellationToken)).ConfigureAwait(false);

        var resolvedTenantId = AuthHelpers.ResolveTenantId(userRoles);

        var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Username, resolvedTenantId, roles, permissions, user.DisplayName);
        var refreshToken = _jwtService.GenerateRefreshToken();
        var refreshTokenTtl = TimeSpan.FromDays(_jwtService.RefreshTokenExpirationDays);

        await (_refreshTokenStore.StoreAsync(user.Id, refreshToken, refreshTokenTtl, cancellationToken)).ConfigureAwait(false);

        user.RecordLogin();
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

/// <summary>
///
/// </summary>
public sealed record RefreshTokenCommand(string RefreshToken) : ICommand<TokenResponse>, IAuditableRequest<TokenResponse>
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

        var userId = await (_refreshTokenStore.GetUserIdAsync(request.RefreshToken, cancellationToken)).ConfigureAwait(false);
        if (userId is null)
        {
            throw new UnauthorizedException("Invalid refresh token");
        }

        var userRepo = _unitOfWork.Repository<User>();
        var user = await (userRepo.GetByIdAsync(userId.Value, cancellationToken)).ConfigureAwait(false);

        if (user is null)
        {
            throw new UnauthorizedException("User not found");
        }

        await (_refreshTokenStore.RemoveAsync(request.RefreshToken, cancellationToken)).ConfigureAwait(false);

        var userRoles = await (AuthHelpers.GetUserRolesEntitiesAsync(_unitOfWork, user.Id, cancellationToken)).ConfigureAwait(false);
        var roles = await (AuthHelpers.ResolveRoleNamesAsync(_unitOfWork, userRoles, cancellationToken)).ConfigureAwait(false);
        var permissions = await (AuthHelpers.GetUserPermissionsAsync(_unitOfWork, user.Id, cancellationToken)).ConfigureAwait(false);

        var resolvedTenantId = AuthHelpers.ResolveTenantId(userRoles);

        var newAccessToken = _jwtService.GenerateAccessToken(user.Id, user.Username, resolvedTenantId, roles, permissions, user.DisplayName);
        var newRefreshToken = _jwtService.GenerateRefreshToken();
        var refreshTokenTtl = TimeSpan.FromDays(_jwtService.RefreshTokenExpirationDays);

        await (_refreshTokenStore.StoreAsync(user.Id, newRefreshToken, refreshTokenTtl, cancellationToken)).ConfigureAwait(false);

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
