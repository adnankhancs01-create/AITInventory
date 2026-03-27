using System;
using System.Collections.Generic;
using System.Text;

namespace Common
{
    public class BaseResponse<T>
    {
        public bool Success { get; set; }          // Indicates if the request was successful
        public string Message { get; set; }        // Optional message
        public T Data { get; set; }                // Generic data payload
        public List<string> Errors { get; set; }   // Optional list of error messages

        // Constructor for success response
        public BaseResponse(T data, string message = "")
        {
            Success = true;
            Message = message;
            Data = data;
            Errors = null;
        }

        // Constructor for failure response
        public BaseResponse(List<string> errors, string message = "")
        {
            Success = false;
            Message = message;
            Data = default;
            Errors = errors;
        }

        // Static helper for success
        public static BaseResponse<T> SuccessResponse(T data, string message = "")
        {
            return new BaseResponse<T>(data, message);
        }

        // Static helper for failure
        public static BaseResponse<T> FailureResponse(List<string> errors, string message = "")
        {
            return new BaseResponse<T>(errors, message);
        }
    }
}
