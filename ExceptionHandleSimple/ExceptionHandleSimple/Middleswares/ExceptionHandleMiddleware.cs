using System.Text;

namespace ExceptionHandleSimple.Middleswares
{
    public class ExceptionHandleMiddleware
    {
        //ref: https://blog.poychang.net/asp-net-core-web-api-global-exception-handler/
        //ref: https://blog.poychang.net/logging-http-request-in-asp-net-core/
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandleMiddleware> _logger;

        public ExceptionHandleMiddleware(RequestDelegate next, ILogger<ExceptionHandleMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// 任務調用
        /// </summary>
        /// <param name="context">HTTP 的上下文</param>
        /// <returns></returns>
        public async Task Invoke(HttpContext context)
        {
            // 確保 HTTP Request 可以多次讀取
            context.Request.EnableBuffering();
            try
            {
                await _next(context);
            }
            catch (Exception exception)
            {
                await HandleExceptionAsync(context, exception);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var path = $"{context.Request.Path}";
            var method = $"{context.Request.Method}";
            var controllerName = $"{context.Request.RouteValues["controller"]}";
            var actionName = $"{context.Request.RouteValues["action"]}";
            var env = $"{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}";
            var body = ReadRequestBody(context);

            _logger.LogError("{now} | [{env}]({method}):{path} | {controllerName}-{actionName} | {message} | Data:{body}",
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}", env, method, path, controllerName, actionName, exception.StackTrace, body);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return context.Response.WriteAsync($"Catch Exception from Middleware : {exception.Message}");
        }

        private static string ReadRequestBody(HttpContext context)
        {
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
            // 將 HTTP Request 的 Stream 起始位置歸零
            context.Request.Body.Seek(0, SeekOrigin.Begin);
            var body = reader.ReadToEndAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            // 將 HTTP Request 的 Stream 起始位置歸零
            context.Request.Body.Seek(0, SeekOrigin.Begin);
            return body;
        }
    }

    public static class ExceptionHandleMiddlewareExtensions
    {
        /// <summary>在中介程序中全域處理例外</summary>
        /// <param name="builder">中介程序建構器</param>
        /// <returns></returns>
        public static IApplicationBuilder UseExceptionHandleMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExceptionHandleMiddleware>();
        }
    }

}
