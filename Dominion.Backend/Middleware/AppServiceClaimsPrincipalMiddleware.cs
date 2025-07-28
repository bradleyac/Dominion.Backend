using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR;

namespace Dominion.Backend.Middleware;

public class AppServiceClaimsPrincipalHubFilter(ILogger<AppServiceClaimsPrincipalHubFilter> logger) : IHubFilter
{
  private readonly ILogger<AppServiceClaimsPrincipalHubFilter> _logger = logger;
  ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
  {
    _logger.LogWarning(invocationContext?.Context?.User?.Identity?.Name ?? "invocationContext.Context.User.Identity.Name null");
    var httpContext = invocationContext.Context.GetHttpContext()!;

    var clientPrincipalHeaderJson = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(httpContext.Request.Headers["X-MS-CLIENT-PRINCIPAL"].SingleOrDefault() ?? throw new HubException("Unauthorized")));
    var user = ParseClientPrincipalHeader(clientPrincipalHeaderJson);

    _logger.LogWarning(user?.Identity?.Name ?? "user.Identity.Name null");

    if (user?.Identity?.IsAuthenticated ?? false)
    {
      httpContext.Items["claimsPrincipal"] = user;
      httpContext.Items["authType"] = user.Identity.AuthenticationType;
      httpContext.Items["authPrincipalName"] = user.Identity.Name;
      httpContext.Items["firstName"] = user.FindFirstValue(ClaimTypes.GivenName) ?? "Unknown";
      httpContext.Items["lastName"] = user.FindFirstValue(ClaimTypes.Surname) ?? "Unknown";

      return next(invocationContext);
    }
    else
    {
      throw new HubException("Unauthorized");
    }
  }

  private static ClaimsPrincipal ParseClientPrincipalHeader(string clientPrincipalHeaderJson)
  {
    var principal = JsonSerializer.Deserialize<ClientPrincipal>(clientPrincipalHeaderJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    var identity = new ClaimsIdentity(principal!.IdentityProvider, principal.NameClaimType, principal.RoleClaimType);
    identity.AddClaims(principal.Claims.Select(c => new Claim(c.Type, c.Value)));

    return new ClaimsPrincipal(identity);
  }
}

internal class ClientPrincipalClaim
{
  [JsonPropertyName("typ")]
  public string Type { get; set; }
  [JsonPropertyName("val")]
  public string Value { get; set; }
}

internal class ClientPrincipal
{
  [JsonPropertyName("auth_typ")]
  public string IdentityProvider { get; set; }
  [JsonPropertyName("name_typ")]
  public string NameClaimType { get; set; }
  [JsonPropertyName("role_typ")]
  public string RoleClaimType { get; set; }
  [JsonPropertyName("claims")]
  public IEnumerable<ClientPrincipalClaim> Claims { get; set; }
}
public static class ClaimsPrincipalMiddlewareExtensions
{
  public static void UseAppServiceClaimsPrincipalHubFilter(this HubOptions hubOptions) => hubOptions.AddFilter<AppServiceClaimsPrincipalHubFilter>();
}