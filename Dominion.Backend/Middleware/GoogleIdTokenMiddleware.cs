using System;
using Google.Apis.Auth;

namespace Dominion.Backend.Middleware;

public class GoogleIdTokenMiddleware(RequestDelegate next)
{
  private RequestDelegate _next = next;

  public async Task InvokeAsync(HttpContext context)
  {
    // TODO: Set up auth to do this properly and/or use cookies?
    string? idToken;
    // Check query string for access_token first, where it will be for WS and SSE.
    if (context.Request.Query.TryGetValue("access_token", out var values))
    {
      idToken = values.FirstOrDefault();
    }
    else
    {
      // Get Authorization header and remove Bearer to retrieve the idToken.
      idToken = context.Request.Headers.Authorization.FirstOrDefault()?.Substring(startIndex: 7);
    }

    if (idToken is null)
    {
      throw new UnauthorizedAccessException("Id Token missing");
    }

    var playerId = await VerifyGoogleTokenId(idToken);

    if (playerId is null)
    {
      throw new UnauthorizedAccessException("Id Token missing email");
    }

    context.Items["playerId"] = playerId;

    await _next(context);
  }

  private async Task<string?> VerifyGoogleTokenId(string token)
  {
    try
    {
      var validationSettings = new GoogleJsonWebSignature.ValidationSettings
      {
        Audience = ["515082896158-m9qtkkba1tviq45ou8p5r83a86qoltgs.apps.googleusercontent.com"]
      };

      GoogleJsonWebSignature.Payload payload = await GoogleJsonWebSignature.ValidateAsync(token, validationSettings);

      return payload.Email;
    }
    catch (InvalidJwtException)
    {
      throw new UnauthorizedAccessException("Id Token invalid");
    }
  }
}

public static class GoogleIdTokenMiddlewareExtensions
{
  public static IApplicationBuilder UseRequireGoogleIdTokenMiddleware(this IApplicationBuilder builder)
  {
    return builder.UseMiddleware<GoogleIdTokenMiddleware>();
  }
}