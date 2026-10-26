using Common;
using Domain.IRepositories;
using System.Text.Json;

namespace Service.Service
{
    /// <summary>Same error handling and logging behaviour as StockResult2, for the user-management services.</summary>
    public static class UserResult2
    {
        public static BaseResponse<T> Ok<T>(T data, string message = "")
        {
            return BaseResponse<T>.SuccessResponse(data, message);
        }

        public static BaseResponse<T> Fail<T>(string error)
        {
            return BaseResponse<T>.FailureResponse(new List<string> { error }, "Request failed");
        }

        public static BaseResponse<T> Fail<T>(List<string> errors)
        {
            return BaseResponse<T>.FailureResponse(errors, "Request failed");
        }

        public static async Task<BaseResponse<T>> RunAsync<T>(
            Func<Task<BaseResponse<T>>> action,
            ILogRepository logRepo,
            string methodName,
            int? userId = null,
            object? request = null)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                // Never write passwords to the exception log.
                string? requestJson = null;
                try
                {
                    requestJson = request == null ? null : JsonSerializer.Serialize(request);
                    if (requestJson != null && requestJson.Contains("assword", StringComparison.OrdinalIgnoreCase))
                        requestJson = "(request hidden: contains a password)";
                }
                catch
                {
                    // Request could not be serialised; log without it.
                }

                await logRepo.LogExceptionAsync(
                    ex,
                    userId: userId,
                    additionalData: JsonSerializer.Serialize(new { message = $"Error in {methodName}" }),
                    request: requestJson,
                    MethodName: methodName);

                return Fail<T>(ex.InnerException?.Message ?? ex.Message);
            }
        }
    }
}
