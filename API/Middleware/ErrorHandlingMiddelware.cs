using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Application.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace API.Middleware
{
    public class ErrorHandlingMiddelware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddelware> _logger;
        public ErrorHandlingMiddelware(RequestDelegate next, ILogger<ErrorHandlingMiddelware> logger)
        {
            _logger = logger;
            _next = next;
        }

        public async Task Invoke(HttpContext context){
            {
                try{
                    await _next(context);
                }catch(Exception e){
                    await HandleExceptionAsync(context, e, _logger);
                }
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception e, ILogger<ErrorHandlingMiddelware> logger)
        {
            object errors = null;

            switch (e){
                case RestException re:
                    logger.LogError(e, "Rest Error");
                    errors= re.Errors;
                    context.Response.StatusCode = (int)re.Code;
                    break;
                case Exception se:
                    logger.LogError(e, "Server Error");
                    errors = string.IsNullOrWhiteSpace(e.Message) ? "Error" : e.Message;
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    break;
            }

            context.Response.ContentType = "application/json";
            if(errors != null)
            {
                var result = JsonSerializer.Serialize(new {
                    errors
                });

                await context.Response.WriteAsync(result);
            }
        }
    }
}