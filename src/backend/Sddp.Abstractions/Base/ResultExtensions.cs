using Sddp.Abstractions.Exceptions;

namespace Sddp.Abstractions.Base;

public static class ResultExtensions
{
    public static void EnsureSuccess(this Result result, string? errorCode = null)
    {
        if (result.IsFailure)
        {
            var error = result.Error;
            throw new SddpException(errorCode ?? error.Code, error.Message);
        }
    }

    public static T EnsureSuccess<T>(this Result<T> result, string? errorCode = null)
    {
        if (result.IsFailure)
        {
            var error = result.Error;
            throw new SddpException(errorCode ?? error.Code, error.Message);
        }

        return result.Value;
    }
}
