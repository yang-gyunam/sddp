using Sddp.Abstractions.Interfaces;

namespace Sddp.Api.Services;

/// <summary>
/// Request context provider based on IHttpContextAccessor.
/// </summary>
public class HttpRequestContextProvider : IRequestContextProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpRequestContextProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? IpAddress =>
        _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent =>
        _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();

    public string? CorrelationId =>
        _httpContextAccessor.HttpContext?.TraceIdentifier;
}
