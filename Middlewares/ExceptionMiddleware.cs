using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (Exception ex)
        {
            LogException(ex);
            await HandleExceptionAsync(httpContext, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        // You can customize the response here, e.g., by providing more details about the error
        var result = $"{{ \"message\": \"{exception.Message}\", \"stackTrace\": \"{exception.StackTrace}\" }}";
        return context.Response.WriteAsync(result);
    }

    private void LogException(Exception ex)
    {
        // Log exception message and stack trace
        Console.WriteLine("Exception Message: " + ex.Message);
        Console.WriteLine("Stack Trace: " + ex.StackTrace);

        // You can also write the exception message and stack trace to a file or any other logging system
        File.WriteAllText("error_log.txt", "Exception Message: " + ex.Message + "\nStack Trace: " + ex.StackTrace);
    }
}
