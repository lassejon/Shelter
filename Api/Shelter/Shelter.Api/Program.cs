using System.Text.Json.Serialization;
using Microsoft.OpenApi;
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

builder.Services.AddSwaggerGen(c =>
{
    const string schemeName = "Bearer";

    // 1. Define the scheme
    c.AddSecurityDefinition(schemeName, new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,   // <- Http + bearer is the canonical setup
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'."
    });

    // 2. Require that scheme for all operations
    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        // This constructor looks up the scheme named `schemeName` in the document
        [new OpenApiSecuritySchemeReference(schemeName, document)] = []
    });
});

// Authentication
builder.Services.ConfigureIdentity();
builder.Services.AddJwtAuthentication(builder.Configuration["JWT:ValidAudience"], builder.Configuration["JWT:ValidIssuer"], builder.Configuration["JWT:Secret"] ?? "Default secret");

builder.Services
    .AddInfrastructure();
    // .AddApplication();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

builder.Services.AddAuthorization();


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();