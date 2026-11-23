using Sddp.Abstractions.DTOs;

namespace Sddp.Abstractions.Interfaces;

public interface IServiceHealthReader
{
    Task<HealthCheckDto> ReadAsync(CancellationToken cancellationToken);
}
