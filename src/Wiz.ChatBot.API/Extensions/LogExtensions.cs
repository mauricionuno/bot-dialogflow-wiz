using Microsoft.AspNetCore.Builder;
using Wiz.CRM.API.Middlewares;

namespace Wiz.ChatBot.API.Extensions
{
    public static class LogExtensions
    {
        public static IApplicationBuilder UseLogMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<LogMiddleware>();
        }
    }
}
