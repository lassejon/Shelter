using Shelter.Api.Configuration;
using Shelter.Api.Features.Auth;
using Shelter.Api.Features.Bookings;
using Shelter.Api.Features.Reviews;
using Shelter.Api.Features.Shelters;
using Shelter.Infrastructure;
using Shelter.Infrastructure.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(o => o.AddDocumentTransformer<BearerSecuritySchemeTransformer>());
builder.Services.ConfigureJsonSerialization();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    await app.Services.MigrateDatabaseAsync();
    await app.Services.SeedRolesAsync();

    app.MapOpenApi();
    app.UseSwaggerUI(o =>
    {
        o.SwaggerEndpoint("/openapi/v1.json", "Shelter API V1");
        o.ConfigObject.AdditionalItems.Add("persistAuthorization", "true");
    });
}

app.UseHttpsRedirection();

// CORS must be applied before Authentication so preflight (OPTIONS) requests
// are answered without authentication.
app.UseAppCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapShelterEndpoints();
app.MapBookingEndpoints();
app.MapReviewEndpoints();

app.Run();

public partial class Program;
