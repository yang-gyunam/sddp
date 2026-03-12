using System.Security.Cryptography;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using Sddp.Abstractions.Constants;
using Sddp.Abstractions.DTOs;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.Exceptions;
using Sddp.Abstractions.Interfaces;
using Sddp.Abstractions.ValueObjects;
using Sddp.Application.Requests;
using Sddp.Domain.Entities;

namespace Sddp.Application.Features.Users.Commands;

/// <summary>
/// Updates the current user's profile
/// </summary>
public sealed record UpdateCurrentUserProfileCommand(
    GlobalUniqueId UserId,
    UpdateProfileDto Dto) : ICommand<UserDto?>, IAuditableRequest<UserDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(UserDto? response) => AuditLog;
}

public sealed class UpdateCurrentUserProfileCommandHandler : IRequestHandler<UpdateCurrentUserProfileCommand, UserDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCurrentUserProfileCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserDto?> Handle(UpdateCurrentUserProfileCommand request, CancellationToken cancellationToken)
    {
        var userRepo = _unitOfWork.Repository<User>();
        var user = await (userRepo.GetByIdAsync(request.UserId, cancellationToken)).ConfigureAwait(false);

        if (user is null)
        {
            return null;
        }

        user.UpdateProfile(request.Dto.DisplayName, request.Dto.Email, request.Dto.AvatarUrl);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            request.UserId,
            "update_profile",
            "user",
            request.UserId,
            new { request.Dto.DisplayName, request.Dto.Email, request.Dto.AvatarUrl },
            null,
            null);

        return UserMapping.MapToDto(user);
    }
}

/// <summary>
/// Updates the current user's preferences
/// </summary>
public sealed record UpdateCurrentUserPreferencesCommand(
    GlobalUniqueId UserId,
    UserPreferencesDto Dto) : ICommand<UserPreferencesDto?>, IAuditableRequest<UserPreferencesDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(UserPreferencesDto? response) => AuditLog;
}

public sealed class UpdateCurrentUserPreferencesCommandHandler : IRequestHandler<UpdateCurrentUserPreferencesCommand, UserPreferencesDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCurrentUserPreferencesCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserPreferencesDto?> Handle(UpdateCurrentUserPreferencesCommand request, CancellationToken cancellationToken)
    {
        var userRepo = _unitOfWork.Repository<User>();
        var user = await (userRepo.GetByIdAsync(request.UserId, cancellationToken)).ConfigureAwait(false);

        if (user is null)
        {
            return null;
        }

        var preferencesJson = request.Dto.Preferences is not null
            ? JsonSerializer.Serialize(request.Dto.Preferences)
            : null;

        user.UpdatePreferences(preferencesJson);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            request.UserId,
            "update_preferences",
            "user",
            request.UserId,
            new { Preferences = request.Dto.Preferences },
            null,
            null);

        return new UserPreferencesDto(
            user.Preferences is not null
                ? JsonSerializer.Deserialize<object>(user.Preferences)
                : null);
    }
}

/// <summary>
/// Updates notification settings
/// </summary>
public sealed record UpdateNotificationSettingsCommand(
    GlobalUniqueId UserId,
    NotificationSettingsDto Dto) : ICommand<NotificationSettingsDto?>, IAuditableRequest<NotificationSettingsDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(NotificationSettingsDto? response) => AuditLog;
}

public sealed class UpdateNotificationSettingsCommandHandler : IRequestHandler<UpdateNotificationSettingsCommand, NotificationSettingsDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateNotificationSettingsCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<NotificationSettingsDto?> Handle(UpdateNotificationSettingsCommand request, CancellationToken cancellationToken)
    {
        var userRepo = _unitOfWork.Repository<User>();
        var user = await (userRepo.GetByIdAsync(request.UserId, cancellationToken)).ConfigureAwait(false);

        if (user is null)
        {
            return null;
        }

        // Parse existing preferences or create empty object
        Dictionary<string, JsonElement> existingPrefs;
        if (user.Preferences is not null)
        {
            try
            {
                existingPrefs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(user.Preferences)
                    ?? new Dictionary<string, JsonElement>();
            }
            catch
            {
                existingPrefs = new Dictionary<string, JsonElement>();
            }
        }
        else
        {
            existingPrefs = new Dictionary<string, JsonElement>();
        }

        // Merge notifications key into existing preferences
        var notificationsJson = JsonSerializer.SerializeToElement(request.Dto);
        existingPrefs["notifications"] = notificationsJson;

        var mergedJson = JsonSerializer.Serialize(existingPrefs);
        user.UpdatePreferences(mergedJson);
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            request.UserId,
            "update_notification_settings",
            "user",
            request.UserId,
            new { Notifications = request.Dto },
            null,
            null);

        return request.Dto;
    }
}

/// <summary>
/// Deactivates a user
/// </summary>
public sealed record DeactivateUserCommand(
    GlobalUniqueId UserId,
    GlobalUniqueId RequestUserId) : ICommand<bool>, IAuditableRequest<bool>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(bool response) => AuditLog;
}

public sealed class DeactivateUserCommandHandler : IRequestHandler<DeactivateUserCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateUserCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        var userRepo = _unitOfWork.Repository<User>();
        var user = await (userRepo.GetByIdAsync(request.UserId, cancellationToken)).ConfigureAwait(false);

        if (user is null)
        {
            throw new NotFoundException("User", (Guid)request.UserId);
        }

        user.Deactivate();
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            request.RequestUserId,
            "deactivate",
            "user",
            request.UserId,
            new { Username = user.Username, DisplayName = user.DisplayName },
            null,
            null);

        return true;
    }
}

/// <summary>
/// Activates a user
/// </summary>
public sealed record ActivateUserCommand(
    GlobalUniqueId UserId,
    GlobalUniqueId RequestUserId) : ICommand<bool>, IAuditableRequest<bool>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(bool response) => AuditLog;
}

public sealed class ActivateUserCommandHandler : IRequestHandler<ActivateUserCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public ActivateUserCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(ActivateUserCommand request, CancellationToken cancellationToken)
    {
        var userRepo = _unitOfWork.Repository<User>();
        var user = await (userRepo.GetByIdIncludingInactiveAsync(request.UserId, cancellationToken)).ConfigureAwait(false);

        if (user is null)
        {
            throw new NotFoundException("User", (Guid)request.UserId);
        }

        if (user.IsActive)
        {
            throw new ValidationException("User is already active");
        }

        user.Activate();
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            request.RequestUserId,
            "activate",
            "user",
            request.UserId,
            new { Username = user.Username, DisplayName = user.DisplayName },
            null,
            null);

        return true;
    }
}

/// <summary>
/// Creates a user
/// </summary>
public sealed record CreateUserCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId RequestUserId,
    CreateUserDto Dto) : ICommand<SystemUserDto?>, IAuditableRequest<SystemUserDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(SystemUserDto? response) => AuditLog;
}

public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, SystemUserDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public CreateUserCommandHandler(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<SystemUserDto?> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var userRepo = _unitOfWork.Repository<User>();
        var roleRepo = _unitOfWork.Repository<Role>();
        var userRoleRepo = _unitOfWork.Repository<UserRole>();

        var dto = request.Dto;

        // 1. Check username uniqueness
        var existingByUsername = await (userRepo.FindAsync(
            u => u.Username == dto.Username, cancellationToken)).ConfigureAwait(false);
        if (existingByUsername.Any())
        {
            throw new ConflictException($"Username '{dto.Username}' already exists");
        }

        // 2. Check email uniqueness
        var existingByEmail = await (userRepo.FindAsync(
            u => u.Email == dto.Email, cancellationToken)).ConfigureAwait(false);
        if (existingByEmail.Any())
        {
            throw new ConflictException($"Email '{dto.Email}' already exists");
        }

        // 3. Generate the password hash
        var passwordHash = _passwordHasher.Hash(dto.Password);

        // 4. Create the Person
        var personRepo = _unitOfWork.Repository<Person>();
        var person = new Person(dto.DisplayName, dto.Email);
        await (personRepo.AddAsync(person, cancellationToken)).ConfigureAwait(false);

        // 5. Create the User
        var user = new User(dto.Username, dto.Email, passwordHash, dto.DisplayName, person.Id);
        await (userRepo.AddAsync(user, cancellationToken)).ConfigureAwait(false);

        // 6. Assign the default role (DEVELOPER)
        var roles = await (roleRepo.GetAllAsync(cancellationToken)).ConfigureAwait(false);
        var developerRole = roles.FirstOrDefault(r => r.Type == RoleType.Developer);

        if (developerRole is not null)
        {
            var userRole = new UserRole(
                user.Id, developerRole.Id, request.RequestUserId, request.TenantId);
            await (userRoleRepo.AddAsync(userRole, cancellationToken)).ConfigureAwait(false);
        }

        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        request.AuditLog = new AuditLogRequest(
            request.RequestUserId,
            "create",
            "user",
            user.Id,
            new { dto.Username, dto.DisplayName },
            request.TenantId,
            null);

        // 7. Return the mapped SystemUserDto
        var userRoles = await (userRoleRepo.FindAsync(ur => ur.UserId == user.Id, cancellationToken)).ConfigureAwait(false);
        var roleMap = roles.ToDictionary(r => r.Id);

        return UserMapping.MapToSystemUserDto(user, userRoles, roleMap, []);
    }
}

/// <summary>
/// Updates a system user (admin only)
/// </summary>
public sealed record UpdateSystemUserCommand(
    GlobalUniqueId TenantId,
    GlobalUniqueId UserId,
    GlobalUniqueId RequestUserId,
    UpdateSystemUserDto Dto) : ICommand<SystemUserDto?>, IAuditableRequest<SystemUserDto?>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(SystemUserDto? response) => AuditLog;
}

public sealed class UpdateSystemUserCommandHandler : IRequestHandler<UpdateSystemUserCommand, SystemUserDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSystemUserCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SystemUserDto?> Handle(UpdateSystemUserCommand request, CancellationToken cancellationToken)
    {
        var userRepo = _unitOfWork.Repository<User>();
        var roleRepo = _unitOfWork.Repository<Role>();
        var userRoleRepo = _unitOfWork.Repository<UserRole>();

        // 1. Load the user (inactive users can also be updated)
        var user = await (userRepo.GetByIdIncludingInactiveAsync(request.UserId, cancellationToken)).ConfigureAwait(false);
        if (user is null)
        {
            throw new NotFoundException("User", (Guid)request.UserId);
        }

        // 2. Built-in users cannot be modified
        var isBuiltIn = (Guid)user.Id == WellKnownUsers.AdminUserId
                     || (Guid)user.Id == WellKnownUsers.AiAgentUserId;
        if (isBuiltIn)
        {
            throw new ValidationException("Built-in system users cannot be modified");
        }

        var dto = request.Dto;

        // 3. Check email uniqueness against other users
        if (!string.Equals(user.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existingByEmail = await (userRepo.FindAsync(
                u => u.Email == dto.Email, cancellationToken)).ConfigureAwait(false);
            if (existingByEmail.Any(u => u.Id != request.UserId))
            {
                throw new ConflictException($"Email '{dto.Email}' already exists");
            }
        }

        // 4. Update the profile
        user.UpdateProfile(dto.DisplayName, dto.Email);

        // 5. Change the GlobalRole
        if (dto.GlobalRole is not null)
        {
            var roles = await (roleRepo.GetAllAsync(cancellationToken)).ConfigureAwait(false);
            var existingUserRoles = await (userRoleRepo.FindAsync(
                ur => ur.UserId == request.UserId, cancellationToken)).ConfigureAwait(false);

            // Remove existing roles
            foreach (var existingRole in existingUserRoles)
            {
                existingRole.Deactivate();
            }

            // Assign the new role
            if (!Enum.TryParse<RoleType>(dto.GlobalRole, ignoreCase: true, out var targetRoleType))
                targetRoleType = RoleType.Developer;

            var targetRole = roles.FirstOrDefault(r => r.Type == targetRoleType);
            if (targetRole is not null)
            {
                var newUserRole = new UserRole(
                    request.UserId, targetRole.Id, request.RequestUserId, request.TenantId);
                await (userRoleRepo.AddAsync(newUserRole, cancellationToken)).ConfigureAwait(false);
            }
        }

        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        // 6. Audit log
        request.AuditLog = new AuditLogRequest(
            request.RequestUserId,
            "update",
            "user",
            request.UserId,
            new { dto.DisplayName, dto.Email, dto.GlobalRole },
            request.TenantId,
            null);

        // 7. Return the SystemUserDto
        var allRoles = await (roleRepo.GetAllAsync(cancellationToken)).ConfigureAwait(false);
        var updatedUserRoles = await (userRoleRepo.FindAsync(
            ur => ur.UserId == request.UserId, cancellationToken)).ConfigureAwait(false);
        var roleMap = allRoles.ToDictionary(r => r.Id);

        return UserMapping.MapToSystemUserDto(user, updatedUserRoles, roleMap, []);
    }
}

/// <summary>
/// Resets a user's password as an administrator
/// </summary>
public sealed record AdminResetPasswordCommand(
    GlobalUniqueId UserId,
    GlobalUniqueId RequestUserId) : ICommand<AdminResetPasswordResponse>, IAuditableRequest<AdminResetPasswordResponse>
{
    public AuditLogRequest? AuditLog { get; set; }
    public AuditLogRequest? GetAuditLogRequest(AdminResetPasswordResponse response) => AuditLog;
}

public sealed class AdminResetPasswordCommandHandler : IRequestHandler<AdminResetPasswordCommand, AdminResetPasswordResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<AdminResetPasswordCommandHandler> _logger;

    public AdminResetPasswordCommandHandler(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ILogger<AdminResetPasswordCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<AdminResetPasswordResponse> Handle(AdminResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var userRepo = _unitOfWork.Repository<User>();
        var user = await (userRepo.GetByIdAsync(request.UserId, cancellationToken)).ConfigureAwait(false);

        if (user is null)
        {
            throw new NotFoundException("User", (Guid)request.UserId);
        }

        // Block resets for built-in users
        var isBuiltIn = (Guid)user.Id == WellKnownUsers.AdminUserId
                     || (Guid)user.Id == WellKnownUsers.AiAgentUserId;
        if (isBuiltIn)
        {
            throw new ValidationException("Cannot reset password for built-in system users");
        }

        // Generate a temporary password (12 chars, upper/lowercase, digits, special chars)
        var tempPassword = GenerateTemporaryPassword();
        var hash = _passwordHasher.Hash(tempPassword);

        user.UpdatePassword(hash);
        user.ForcePasswordChange();
        await (_unitOfWork.SaveChangesAsync(cancellationToken)).ConfigureAwait(false);

        _logger.LogInformation("Admin reset password for user: {Username} by {RequestUserId}",
            user.Username, (Guid)request.RequestUserId);

        request.AuditLog = new AuditLogRequest(
            ActorId: request.RequestUserId,
            Action: "admin_reset_password",
            ResourceType: "user",
            ResourceId: request.UserId,
            Payload: new { Username = user.Username },
            TenantId: null,
            ProjectId: null);

        return new AdminResetPasswordResponse(tempPassword);
    }

    private static string GenerateTemporaryPassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghjkmnpqrstuvwxyz";
        const string digits = "23456789";
        const string special = "!@#$%&*";
        const string all = upper + lower + digits + special;

        var bytes = new byte[12];
        RandomNumberGenerator.Fill(bytes);

        var chars = new char[12];
        // Ensure at least one of each type
        chars[0] = upper[bytes[0] % upper.Length];
        chars[1] = lower[bytes[1] % lower.Length];
        chars[2] = digits[bytes[2] % digits.Length];
        chars[3] = special[bytes[3] % special.Length];

        // Fill remaining with random from all chars
        for (var i = 4; i < 12; i++)
        {
            chars[i] = all[bytes[i] % all.Length];
        }

        // Shuffle using Fisher-Yates
        var shuffleBytes = new byte[12];
        RandomNumberGenerator.Fill(shuffleBytes);
        for (var i = chars.Length - 1; i > 0; i--)
        {
            var j = shuffleBytes[i] % (i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }
}
