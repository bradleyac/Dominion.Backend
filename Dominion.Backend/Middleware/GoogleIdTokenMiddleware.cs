using System;
using Google.Apis.Auth;

namespace Dominion.Backend.Middleware;

public class GoogleIdTokenMiddleware(RequestDelegate next)
{
  private RequestDelegate _next = next;

  public async Task InvokeAsync(HttpContext context)
  {
    string? idToken = context.Request.Headers["google-id-token"].FirstOrDefault();

    if (idToken is null)
    {
      throw new UnauthorizedAccessException("google-id-token header missing");
    }

    var playerId = await VerifyGoogleTokenId(idToken);

    if (playerId is null)
    {
      throw new UnauthorizedAccessException("google-id-token header missing email");
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
      throw new UnauthorizedAccessException("google-id-token header invalid");
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