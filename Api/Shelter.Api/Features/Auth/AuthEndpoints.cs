using Shelter.Api.Features.Auth.ConfirmEmail;
using Shelter.Api.Features.Auth.Login;
using Shelter.Api.Features.Auth.Logout;
using Shelter.Api.Features.Auth.Me;
using Shelter.Api.Features.Auth.Register;
using Shelter.Api.Features.Auth.ResendConfirmationEmail;
using Shelter.Api.Features.Auth.UpgradeToOwner;

namespace Shelter.Api.Features.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth");

        group.MapLogin();
        group.MapRegister();
        group.MapConfirmEmail();
        group.MapResendConfirmationEmail();
        group.MapLogout();
        group.MapUpgradeToOwner();
        group.MapMe();

        return app;
    }
}
