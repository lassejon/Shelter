using App;
using Microsoft.EntityFrameworkCore;
using Shelter.Api.Configuration;
using Shelter.Api.Features.Auth;
using Shelter.Api.Features.Bookings;
using Shelter.Api.Features.Reviews;
using Shelter.Api.Features.Shelters;
using Shelter.Infrastructure;
using Shelter.Infrastructure.Configuration;
using Shelter.Infrastructure.Persistence;
using Shelter.Infrastructure.Settings;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(o => o.AddDocumentTransformer<BearerSecuritySchemeTransformer>());
builder.Services.ConfigureJsonSerialization();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddShelterApplication();
builder.Services.AddAuthApplication();
builder.Services.AddBookingApplication();
builder.Services.AddReviewApplication();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ShelterDbContext>();
        await db.Database.MigrateAsync();
    }

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
app.UseCors(CorsSettings.PolicyName);

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapShelterEndpoints();
app.MapBookingEndpoints();
app.MapReviewEndpoints();

app.Run();
