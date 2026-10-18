using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sddp.Abstractions.DTOs;
using Sddp.Api.Authorization;
using Sddp.Api.Configuration;

namespace Sddp.Api.Controllers;

/// <summary>Distribution metadata available in both editions without loading an AI endpoint.</summary>
[Route("api/runtime/capabilities")]
[Authorize]
public sealed class RuntimeCapabilitiesController(DistributionInfo distribution, IAuthorizationService authorization)
    : BaseApiController
{
    [HttpGet]
    [ResourceScopeExempt("Build edition with settings permission evaluated in middleware-validated tenant/project scope.")]
    [ProducesResponseType(typeof(ApiResponse<RuntimeCapabilitiesResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get()
    {
        var canConfigure = distribution.AiInstalled
            && (await authorization.AuthorizeAsync(User, null, "CanManageUsers")).Succeeded;
        return Ok(ApiResponse<RuntimeCapabilitiesResponse>.Ok(new(1, distribution.Edition,
            distribution.AiInstalled, distribution.DeploymentEnabled, canConfigure)));
    }
}

/// <summary>Non-secret installed capabilities and current actor's settings permission.</summary>
public sealed record RuntimeCapabilitiesResponse(int ContractVersion, string Edition, bool AiInstalled,
    bool DeploymentEnabled, bool CanConfigureAi);
