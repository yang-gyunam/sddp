using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sddp.Abstractions.Enums;
using Sddp.Abstractions.ValueObjects;
using Sddp.Domain.Entities;
using Sddp.Infrastructure.Persistence.Contexts;

namespace Sddp.Infrastructure.Persistence.Seeding;

/// <summary>
/// Database seed data generator
/// </summary>
public class DatabaseSeeder
{
    private readonly SddpDbContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(SddpDbContext context, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Executes seed-data creation
    /// </summary>
    public async Task SeedAsync()
    {
        _logger.LogInformation("Starting database seeding...");

        try
        {
            // 1. Seed roles
            await (SeedRolesAsync()).ConfigureAwait(false);

            // 2. Seed permissions
            await (SeedPermissionsAsync()).ConfigureAwait(false);

            // 3. Seed role-permission mappings
            await (SeedRolePermissionsAsync()).ConfigureAwait(false);

            // 4. Seed the default administrator account
            await (SeedAdminUserAsync()).ConfigureAwait(false);

            // 5. Seed SDDP domain glossary terms
            await (SeedGlossaryTermsAsync()).ConfigureAwait(false);

            _logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during database seeding");
            throw;
        }
    }

    /// <summary>
    /// Seeds the 7 default roles
    /// </summary>
    private async Task SeedRolesAsync()
    {
        if (await (_context.Roles.AnyAsync()).ConfigureAwait(false))
        {
            _logger.LogInformation("Roles already exist, skipping role seeding");
            return;
        }

        var roles = Role.CreateDefaultRoles().ToList();
        await (_context.Roles.AddRangeAsync(roles)).ConfigureAwait(false);
        await (_context.SaveChangesAsync()).ConfigureAwait(false);

        _logger.LogInformation("Seeded {Count} default roles", roles.Count);
    }

    /// <summary>
    /// Seeds the 31 default permissions
    /// </summary>
    private async Task SeedPermissionsAsync()
    {
        if (await (_context.Permissions.AnyAsync()).ConfigureAwait(false))
        {
            _logger.LogInformation("Permissions already exist, skipping permission seeding");
            return;
        }

        var permissions = Permission.CreateDefaultPermissions().ToList();
        await (_context.Permissions.AddRangeAsync(permissions)).ConfigureAwait(false);
        await (_context.SaveChangesAsync()).ConfigureAwait(false);

        _logger.LogInformation("Seeded {Count} default permissions", permissions.Count);
    }

    /// <summary>
    /// Seeds role-permission mappings
    /// </summary>
    private async Task SeedRolePermissionsAsync()
    {
        if (await (_context.RolePermissions.AnyAsync()).ConfigureAwait(false))
        {
            _logger.LogInformation("Role-Permission mappings already exist, skipping");
            return;
        }

        var roles = await (_context.Roles.ToListAsync()).ConfigureAwait(false);
        var permissions = await (_context.Permissions.ToListAsync()).ConfigureAwait(false);

        var rolePermissions = new List<RolePermission>();

        foreach (var role in roles)
        {
            var permissionsForRole = GetPermissionsForRole(role.Type, permissions);
            foreach (var permission in permissionsForRole)
            {
                rolePermissions.Add(new RolePermission(role.Id, permission.Id));
            }
        }

        await (_context.RolePermissions.AddRangeAsync(rolePermissions)).ConfigureAwait(false);
        await (_context.SaveChangesAsync()).ConfigureAwait(false);

        _logger.LogInformation("Seeded {Count} role-permission mappings", rolePermissions.Count);
    }

    /// <summary>
    /// Maps permissions by role
    /// </summary>
    private static IEnumerable<Permission> GetPermissionsForRole(RoleType roleType, List<Permission> allPermissions)
    {
        // Define permission mappings per role
        var permissionCodes = roleType switch
        {
            // Admin: all permissions
            RoleType.Admin => allPermissions.Select(p => p.Code),

            // ProductOwner: all permissions except user:manage and audit:read
            RoleType.ProductOwner => allPermissions
                .Where(p => p.Code != "user:manage" && p.Code != "audit:read")
                .Select(p => p.Code),

            // DomainExpert: full Spec/Conversation/Requirement/Glossary + Project/Task read
            // SQL: resource_type IN ('spec','conversation','requirement','glossary') OR code IN ('project:read','task:read')
            RoleType.DomainExpert => allPermissions
                .Where(p =>
                    new[] { "spec", "conversation", "requirement", "glossary" }.Contains(p.Resource)
                    || p.Code is "project:read" or "task:read")
                .Select(p => p.Code),

            // Developer: full Spec/Conversation/Requirement/Generation/Task + Glossary/Project read (excluding approve/lock)
            // SQL: resource_type IN ('spec','conversation','requirement','generation','task')
            //      OR code IN ('glossary:read','project:read')
            //      AND code NOT IN ('spec:approve','spec:lock')
            RoleType.Developer => allPermissions
                .Where(p =>
                    new[] { "spec", "conversation", "requirement", "generation", "task" }.Contains(p.Resource)
                    || p.Code is "glossary:read" or "project:read")
                .Where(p => p.Code is not "spec:approve" and not "spec:lock")
                .Select(p => p.Code),

            // Reviewer: read + approval permissions + Project/Task read
            RoleType.Reviewer =>
            [
                "spec:read", "spec:approve",
                "conversation:read", "conversation:post", "conversation:close",
                "requirement:read",
                "glossary:read",
                "project:read", "task:read"
            ],

            // QATester: primarily read permissions (excluding audit:read)
            RoleType.QATester => allPermissions
                .Where(p => p.Action == "read" && p.Code != "audit:read")
                .Select(p => p.Code),

            _ => []
        };

        return allPermissions.Where(p => permissionCodes.Contains(p.Code));
    }

    /// <summary>
    /// Seeds the default administrator account
    /// </summary>
    private async Task SeedAdminUserAsync()
    {
        const string adminUsername = "admin";

        if (await (_context.Users.AnyAsync(u => u.Username == adminUsername)).ConfigureAwait(false))
        {
            _logger.LogInformation("Admin user already exists, skipping");
            return;
        }

        // Generate a BCrypt hash (WorkFactor=12)
        // Password: Admin@123!
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123!", 12);

        var adminUser = new User(
            username: adminUsername,
            email: "admin@sddp.local",
            passwordHash: passwordHash,
            displayName: "System Administrator");

        await (_context.Users.AddAsync(adminUser)).ConfigureAwait(false);
        await (_context.SaveChangesAsync()).ConfigureAwait(false);

        // Assign the Admin role
        var adminRole = await (_context.Roles
            .FirstOrDefaultAsync(r => r.Type == RoleType.Admin)).ConfigureAwait(false);

        if (adminRole != null)
        {
            var userRole = new UserRole(adminUser.Id, adminRole.Id, adminUser.Id);
            await (_context.UserRoles.AddAsync(userRole)).ConfigureAwait(false);
            await (_context.SaveChangesAsync()).ConfigureAwait(false);

            _logger.LogInformation(
                "Seeded admin user '{Username}' with Admin role",
                adminUsername);
        }
        else
        {
            _logger.LogWarning("Admin role not found, admin user created without role");
        }
    }
    /// <summary>
    /// Seeds SDDP domain glossary terms
    /// </summary>
    private async Task SeedGlossaryTermsAsync()
    {
        // Default tenant/project IDs (system-wide terms)
        GlobalUniqueId systemTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        GlobalUniqueId systemProjectId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        if (await (_context.GlossaryTerms.AnyAsync(t => t.TenantId == systemTenantId)).ConfigureAwait(false))
        {
            _logger.LogInformation("Glossary terms already exist, skipping");
            return;
        }

        // Look up the admin user ID
        var adminUser = await (_context.Users.FirstOrDefaultAsync(u => u.Username == "admin")).ConfigureAwait(false);
        GlobalUniqueId definedBy = adminUser?.Id ?? Guid.Empty;

        var terms = CreateSddpDomainTerms(systemTenantId, systemProjectId, definedBy);
        await (_context.GlossaryTerms.AddRangeAsync(terms)).ConfigureAwait(false);
        await (_context.SaveChangesAsync()).ConfigureAwait(false);

        _logger.LogInformation("Seeded {Count} SDDP domain glossary terms", terms.Count);
    }

    /// <summary>
    /// Creates the SDDP domain glossary-term list
    /// </summary>
    private static List<GlossaryTerm> CreateSddpDomainTerms(GlobalUniqueId tenantId, GlobalUniqueId projectId, GlobalUniqueId definedBy)
    {
        var terms = new List<GlossaryTerm>();
        var now = DateTimeOffset.UtcNow;

        // Core Concepts
        terms.Add(CreateTerm(tenantId, projectId, definedBy, now,
            term: "Spec",
            definition: "A structured document that captures design decisions, requirements, and technical specifications. Specs are versioned, immutable after approval, and serve as the source of truth for code generation.",
            category: TermCategory.Technical,
            abbreviation: null,
            synonyms: "Specification, Design Document, ADR",
            source: "SDDP Core Concepts",
            usageExamples: ["Create a new Spec for the authentication module", "This Spec defines the API contract"]));

        terms.Add(CreateTerm(tenantId, projectId, definedBy, now,
            term: "Requirement",
            definition: "A formal statement of a capability, constraint, or quality attribute that the system must satisfy. Requirements are hierarchical (Epic > Story > Task) and traceable to Specs.",
            category: TermCategory.Business,
            abbreviation: "REQ",
            synonyms: "User Requirement, Functional Requirement",
            source: "SDDP Core Concepts",
            usageExamples: ["Link this Spec to REQ-001", "The requirement specifies response time under 200ms"]));

        terms.Add(CreateTerm(tenantId, projectId, definedBy, now,
            term: "Conversation",
            definition: "A collaborative communication space (Channel, Forum, or DirectMessage) for discussing design decisions before they become formal Specs. Conversations can be promoted to Specs when consensus is reached.",
            category: TermCategory.Business,
            abbreviation: null,
            synonyms: "Channel, Forum, DirectMessage",
            source: "SDDP Core Concepts",
            usageExamples: ["Start a conversation about the caching strategy", "Promote this conversation to a Spec"]));

        terms.Add(CreateTerm(tenantId, projectId, definedBy, now,
            term: "SignOff",
            definition: "A formal approval record for a Spec, indicating that a designated approver has reviewed and accepted the design. SignOffs are immutable and tracked for audit purposes.",
            category: TermCategory.Business,
            abbreviation: null,
            synonyms: "Approval, Sign-off",
            source: "SDDP Core Concepts",
            usageExamples: ["Request SignOff from the technical lead", "The Spec requires two SignOffs"]));

        terms.Add(CreateTerm(tenantId, projectId, definedBy, now,
            term: "Alternative",
            definition: "A rejected design option that was considered but not chosen. Alternatives are preserved for historical context and to prevent revisiting previously rejected approaches.",
            category: TermCategory.Technical,
            abbreviation: null,
            synonyms: "Rejected Option, Considered Option",
            source: "SDDP Core Concepts",
            usageExamples: ["Document the rejected alternatives", "Why was this alternative not chosen?"]));

        terms.Add(CreateTerm(tenantId, projectId, definedBy, now,
            term: "Glossary Term",
            definition: "A domain-specific term with a precise definition. Glossary Terms ensure consistent terminology across the project and are auto-highlighted in Specs.",
            category: TermCategory.Business,
            abbreviation: null,
            synonyms: "Domain Term, Vocabulary",
            source: "SDDP Core Concepts",
            usageExamples: ["Add this term to the glossary", "The glossary term is auto-linked in documents"]));

        // Abbreviations
        terms.Add(CreateTerm(tenantId, projectId, definedBy, now,
            term: "SDDP",
            definition: "Spec-Driven Design Platform - A development platform that transforms conversations and consensus into structured specifications, and uses those specifications as the source of truth for code generation.",
            category: TermCategory.Abbreviation,
            abbreviation: "SDDP",
            synonyms: null,
            source: "SDDP Project",
            usageExamples: ["SDDP enforces spec-first development", "Deploy the SDDP platform"]));

        terms.Add(CreateTerm(tenantId, projectId, definedBy, now,
            term: "ADR",
            definition: "Architecture Decision Record - A document that captures an important architectural decision made along with its context and consequences. ADRs are a form of Spec in SDDP.",
            category: TermCategory.Abbreviation,
            abbreviation: "ADR",
            synonyms: "Architecture Decision",
            source: "Michael Nygard - Documenting Architecture Decisions",
            usageExamples: ["Create an ADR for the database choice", "ADR-001 defines our REST API conventions"]));

        terms.Add(CreateTerm(tenantId, projectId, definedBy, now,
            term: "SCD",
            definition: "Slowly Changing Dimension - A data warehousing concept for managing historical changes. SDDP uses SCD Type 6 (hybrid) for temporal versioning of entities.",
            category: TermCategory.Abbreviation,
            abbreviation: "SCD",
            synonyms: "Slowly Changing Dimension",
            source: "Ralph Kimball - The Data Warehouse Toolkit",
            usageExamples: ["Specs use SCD Type 6 for version history", "The SCD pattern tracks valid_from/valid_to"]));

        terms.Add(CreateTerm(tenantId, projectId, definedBy, now,
            term: "DTO",
            definition: "Data Transfer Object - An object that carries data between processes. DTOs are used for API request/response payloads and are generated from Specs.",
            category: TermCategory.Abbreviation,
            abbreviation: "DTO",
            synonyms: "Data Transfer Object, View Model",
            source: "Martin Fowler - Patterns of Enterprise Application Architecture",
            usageExamples: ["Define the CreateSpecDto", "The DTO maps to the API response"]));

        terms.Add(CreateTerm(tenantId, projectId, definedBy, now,
            term: "API",
            definition: "Application Programming Interface - A set of protocols and tools for building software. In SDDP, APIs are defined by Specs and follow REST conventions.",
            category: TermCategory.Abbreviation,
            abbreviation: "API",
            synonyms: "Application Programming Interface, Endpoint",
            source: "Industry Standard",
            usageExamples: ["The API endpoint is /api/specs", "Document the API contract in a Spec"]));

        terms.Add(CreateTerm(tenantId, projectId, definedBy, now,
            term: "UUID",
            definition: "Universally Unique Identifier - A 128-bit identifier used for unique identification. SDDP uses UUIDs (v4) for all entity IDs.",
            category: TermCategory.Abbreviation,
            abbreviation: "UUID",
            synonyms: "GUID, Unique ID",
            source: "RFC 4122",
            usageExamples: ["Generate a new UUID for the entity", "The spec ID is a UUID v4"]));

        // Technical Terms
        terms.Add(CreateTerm(tenantId, projectId, definedBy, now,
            term: "Code Generation",
            definition: "The process of automatically generating source code from Specs. SDDP uses Roslyn Incremental Generators for compile-time code generation.",
            category: TermCategory.Technical,
            abbreviation: null,
            synonyms: "Codegen, Source Generation",
            source: "SDDP Architecture",
            usageExamples: ["Run code generation from the Spec", "The generator creates DTOs automatically"]));

        terms.Add(CreateTerm(tenantId, projectId, definedBy, now,
            term: "Version",
            definition: "A specific revision of a Spec or entity. Versions in SDDP follow semantic versioning and are immutable once created.",
            category: TermCategory.Technical,
            abbreviation: null,
            synonyms: "Revision, Release",
            source: "Semantic Versioning 2.0.0",
            usageExamples: ["Increment the version to 2.0.0", "Compare version 1.0.0 with 1.1.0"]));

        terms.Add(CreateTerm(tenantId, projectId, definedBy, now,
            term: "Tenant",
            definition: "An organizational unit in a multi-tenant architecture. Each tenant has isolated data and configuration in SDDP.",
            category: TermCategory.Technical,
            abbreviation: null,
            synonyms: "Organization, Account",
            source: "Multi-tenancy Architecture",
            usageExamples: ["Configure tenant-specific settings", "Data is isolated by tenant ID"]));

        terms.Add(CreateTerm(tenantId, projectId, definedBy, now,
            term: "Project",
            definition: "A logical grouping of Specs, Requirements, and Conversations within a Tenant. Projects help organize related work items.",
            category: TermCategory.Business,
            abbreviation: null,
            synonyms: "Workspace, Repository",
            source: "SDDP Architecture",
            usageExamples: ["Create a new project for the API", "Switch to the authentication project"]));

        // Domain Terms
        terms.Add(CreateTerm(tenantId, projectId, definedBy, now,
            term: "Relationship",
            definition: "A semantic connection between entities (Specs, Requirements). SDDP supports 8 relationship types: supersedes, evolves_from, extends, conflicts_with, depends_on, implements, replaces, affects.",
            category: TermCategory.Domain,
            abbreviation: null,
            synonyms: "Link, Connection, Association",
            source: "SDDP Entity Model",
            usageExamples: ["This Spec supersedes the previous version", "Create a depends_on relationship"]));

        terms.Add(CreateTerm(tenantId, projectId, definedBy, now,
            term: "Entity",
            definition: "A domain object with a unique identity that persists over time. In SDDP, entities include Spec, Requirement, Conversation, GlossaryTerm, User, etc.",
            category: TermCategory.Domain,
            abbreviation: null,
            synonyms: "Domain Object, Business Object",
            source: "Domain-Driven Design",
            usageExamples: ["Each entity has a unique ID", "The entity lifecycle is managed by the service"]));

        return terms;
    }

    /// <summary>
    /// Helper for creating GlossaryTerm instances
    /// </summary>
    private static GlossaryTerm CreateTerm(
        GlobalUniqueId tenantId,
        GlobalUniqueId projectId,
        GlobalUniqueId definedBy,
        DateTimeOffset now,
        string term,
        string definition,
        TermCategory category,
        string? abbreviation,
        string? synonyms,
        string? source,
        List<string>? usageExamples)
    {
        var glossaryTerm = new GlossaryTerm(
            tenantId: tenantId,
            projectId: projectId,
            term: term,
            definition: definition,
            category: category,
            definedBy: definedBy,
            source: source,
            synonyms: synonyms,
            abbreviation: abbreviation);

        // Set usage examples if provided
        if (usageExamples is { Count: > 0 })
        {
            glossaryTerm.SetUsageExamples(usageExamples);
        }

        // Approve the term (make it Active)
        glossaryTerm.Approve(definedBy);

        return glossaryTerm;
    }
}
