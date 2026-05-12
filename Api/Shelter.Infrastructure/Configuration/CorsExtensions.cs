using Microsoft.AspNetCore.Builder;
using Shelter.Infrastructure.Settings;

namespace Shelter.Infrastructure.Configuration;

public static class CorsExtensions
{
    public static IApplicationBuilder UseAppCors(this IApplicationBuilder app) =>
        app.UseCors(CorsSettings.PolicyName);
}
