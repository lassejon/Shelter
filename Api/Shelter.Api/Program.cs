using App;
using Shelter.Api.Configuration;
using Shelter.Api.Features.Shelters;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(o => o.AddDocumentTransformer<BearerSecuritySchemeTransformer>());
builder.Services.ConfigureJsonSerialization();
builder.Services.AddShelterApplication();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapShelterEndpoints();

app.Run();
