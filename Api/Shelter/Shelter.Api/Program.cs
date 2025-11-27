using System.Text.Json.Serialization;
using Shelter.Api.Configuration;
using Shelter.Infrastructure.Configuration;
using Shelter.Infrastructure.Configuration.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddRouting(options => options.LowercaseUrls = true);

builder.Services.AddEndpointsApiExplorer();

// Authentication
builder.Services.ConfigureIdentity();
builder.Services
    .AddInfrastructure(builder.Configuration);
    // .AddApplication();

builder.Services.AddOpenApi(o => o.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

var app = builder.Build();

// Configure the HTTP request pipeline.
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

app.UseRouting();

app.UseAuthorization();
app.MapControllers();

app.UseCors("ClientPermission");

app.Run();