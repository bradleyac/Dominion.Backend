using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dominion.Backend.Middleware;

public class AppServiceClaimsPrincipalMiddleware(RequestDelegate next)
{
  private RequestDelegate _next = next;

  public async Task InvokeAsync(HttpContext context)
  {
    if (false)
    {
      context.Items["authType"] = "google";
      context.Items["authPrincipalName"] = "andrew.charles.bradley@gmail.com";
      context.Items["firstName"] = "Andrew";
      context.Items["lastName"] = "Bradley";

      await _next(context);
      return;
    }

    var clientPrincipalHeaderJson = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(context.Request.Headers["X-MS-CLIENT-PRINCIPAL"].SingleOrDefault() ?? throw new UnauthorizedAccessException()));
    var user = ParseClientPrincipalHeader(clientPrincipalHeaderJson);

    if (user?.Identity?.IsAuthenticated ?? false)
    {
      context.Items["claimsPrincipal"] = user;
      context.Items["authType"] = user.Identity.AuthenticationType;
      context.Items["authPrincipalName"] = user.Identity.Name;
      context.Items["firstName"] = user.FindFirstValue(ClaimTypes.GivenName) ?? "Unknown";
      context.Items["lastName"] = user.FindFirstValue(ClaimTypes.Surname) ?? "Unknown";

      await _next(context);
    }
    else
    {
      throw new UnauthorizedAccessException();
    }
  }

  private class ClientPrincipalClaim
  {
    [JsonPropertyName("typ")]
    public string Type { get; set; }
    [JsonPropertyName("val")]
    public string Value { get; set; }
  }

  private class ClientPrincipal
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

  private static ClaimsPrincipal ParseClientPrincipalHeader(string clientPrincipalHeaderJson)
  {
    var principal = JsonSerializer.Deserialize<ClientPrincipal>(clientPrincipalHeaderJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    var identity = new ClaimsIdentity(principal!.IdentityProvider, principal.NameClaimType, principal.RoleClaimType);
    identity.AddClaims(principal.Claims.Select(c => new Claim(c.Type, c.Value)));

    return new ClaimsPrincipal(identity);
  }
}

public static class ClaimsPrincipalMiddlewareExtensions
{
  public static IApplicationBuilder UseAppServiceClaimsPrincipalMiddleware(this IApplicationBuilder builder)
    => builder.UseMiddleware<AppServiceClaimsPrincipalMiddleware>();
}