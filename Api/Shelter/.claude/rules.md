# Development Rules

## Auto-Enable Context7
Always use Context7 MCP tools automatically when:
- Generating code
- Using any library or framework
- Needing API documentation
- Working with .NET, EF Core, ASP.NET, or C#

Do this automatically without me having to say "use context7".

## Tech Stack Versions
Always use these specific versions:
- .NET 10.0
- C# 14
- Entity Framework Core 10.0
- ASP.NET Core 10.0
- PostgreSQL 18 with PostGIS 3.6

## Code Style
- Use primary constructors for all services and controllers
- Use collection expressions `[]` instead of `new List<T>()`
- Use file-scoped namespaces
- Use target-typed new where appropriate
- Use record types for DTOs

## For api guidelines, use Zalando. If you cannot find it in context7, use this url:
- https://opensource.zalando.com/restful-api-guidelines/