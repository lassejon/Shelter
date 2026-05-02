using App;
using Microsoft.EntityFrameworkCore;
using Shelter.Api.Configuration;
using Shelter.Api.Features.Auth;
using Shelter.Api.Features.Shelters;
using Shelter.Infrastructure;
using Shelter.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(o => o.AddDocumentTransformer<BearerSecuritySchemeTransformer>());
builder.Services.ConfigureJsonSerialization();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddShelterApplication();
builder.Services.AddAuthApplication();

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

    app.MapOpenApi();
    app.UseSwaggerUI(o =>
    {
        o.SwaggerEndpoint("/openapi/v1.json", "Shelter API V1");
        o.ConfigObject.AdditionalItems.Add("persistAuthorization", "true");
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapShelterEndpoints();

app.Run();
