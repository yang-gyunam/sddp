using System.Diagnostics;

namespace Sddp.Application.Telemetry;

public static class MediatRActivitySource
{
    public static readonly ActivitySource Source = new("Sddp.MediatR");
}
