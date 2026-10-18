using Common;
using Domain.IRepositories;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Service.Service
{
    /// <summary>Small helpers that give every Stock2 service the same error handling and logging behaviour.</summary>
    public static class StockResult2
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

        /// <summary>Runs an operation, converting exceptions into failure responses and writing them to ExceptionLog.</summary>
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
            catch (DbUpdateConcurrencyException)
            {
                return Fail<T>("Another user changed this record while you were working. Reload and try again.");
            }
            catch (Exception ex)
            {
                string? requestJson = null;
                try
                {
                    requestJson = request == null ? null : JsonSerializer.Serialize(request);
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
