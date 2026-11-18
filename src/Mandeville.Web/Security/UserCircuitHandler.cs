using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Mandeville.Web.Security;

/// <summary>Makes the signed-in person's name available (via <see cref="AppUser"/>) to every UI event of a circuit.</summary>
public sealed class UserCircuitHandler(AuthenticationStateProvider authenticationStateProvider) : CircuitHandler
{
    private readonly AuthenticationStateProvider __authenticationStateProvider = authenticationStateProvider;

    public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(Func<CircuitInboundActivityContext, Task> next)
    {
        return async context =>
        {
            try
            {
                var state = await __authenticationStateProvider.GetAuthenticationStateAsync();
                AppUser.Current = state.User.Identity?.IsAuthenticated == true ? state.User.Identity.Name : null;
            }
            catch
            {
                AppUser.Current = null;
            }

            await next(context);
        };
    }
}
