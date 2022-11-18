using Serilog;
using Serilog.Context;
using V.Common.Extensions;

namespace V.Talog.Server.Middlewares
{
    public class LogMiddleware
    {
        private readonly RequestDelegate next;

        public LogMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            using (LogContext.PushProperty("TraceId", context.TraceIdentifier))
            {
                string body = string.Empty;
                try
                {
                    context.Request.EnableBuffering();
                    if (context.Request.Method == "POST")
                    {
                        body = await context.Request.ReadBody();
                    }
                    await this.next(context);
                }
                catch (Exception e)
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    context.Response.ContentType = "application/json";
                    Log.Error(e, $"an error occure, url: {context.Request.GetAbsoluteUrl()} body: {body} remote ip: {context.Connection.RemoteIpAddress} headers: {context.Request.Headers.ToJson()}");
                    await context.Response.WriteAsync(e.Message);
                }
            }
        }
    }
}
