using App;
using Shelter.Api.Configuration;
using Shelter.Api.Features.Shelters;
using Shelter.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(o => o.AddDocumentTransformer<BearerSecuritySchemeTransformer>());
builder.Services.ConfigureJsonSerialization();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddShelterApplication();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
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

app.MapShelterEndpoints();

app.Run();
