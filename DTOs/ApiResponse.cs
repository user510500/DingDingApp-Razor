namespace DingDingApp.DTOs
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }

        public static ApiResponse<T> SuccessResult(T data, string? message = null)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        public static ApiResponse<T> FailResult(string message)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Data = default
            };
        }
    }

    public class ApiResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }

        public static ApiResponse SuccessResult(string? message = null)
        {
            return new ApiResponse
            {
                Success = true,
                Message = message
            };
        }

        public static ApiResponse FailResult(string message)
        {
            return new ApiResponse
            {
                Success = false,
                Message = message
            };
        }
    }

    public class AuthResponse
    {
        public string? QrCodeUrl { get; set; }
    }

    public class LoginStatusResponse
    {
        public bool IsLoggedIn { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }
    }
}

