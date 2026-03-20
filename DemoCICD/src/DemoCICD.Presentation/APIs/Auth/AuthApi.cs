using System.Security.Claims;
using Asp.Versioning.Builder;
using Carter;
using DemoCICD.Contract.Abstractions.Shared;
using DemoCICD.Contract.Services.V1.Auth;
using DemoCICD.Infrastructure.Authorization;
using DemoCICD.Presentation.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace DemoCICD.Presentation.APIs.Auth;

public class AuthApi : ApiEndpoint, ICarterModule
{
    private const string BaseUrl = "/api/v{version:apiVersion}/auth";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.NewVersionedApi("auth").MapGroup(BaseUrl).HasApiVersion(1);

        group.MapPost("login", Login).WithName("Login");
        group.MapPost("refresh-token", RefreshToken).WithName("RefreshToken");
        group.MapPost("logout", Logout).WithName("Logout");
        group.MapGet("me", Me).RequireAuthorization().WithName("Me");
    }

    public static async Task<IResult> Login(ISender sender, [FromBody] Command.LoginCommand command)
    {
        var result = await sender.Send(command);

        if (result.IsFailure)
            return HandlerFailure(result);

        return Results.Ok(result.Value);
    }

    public static async Task<IResult> RefreshToken(ISender sender, [FromBody] Command.RefreshTokenCommand command)
    {
        var result = await sender.Send(command);

        if (result.IsFailure)
            return HandlerFailure(result);

        return Results.Ok(result.Value);
    }

    public static async Task<IResult> Logout(ISender sender, [FromBody] Command.LogoutCommand command)
    {
        var result = await sender.Send(command);

        if (result.IsFailure)
            return HandlerFailure(result);

        return Results.Ok(result);
    }

    public static IResult Me(HttpContext httpContext)
    {
        var user = httpContext.User;
        if (user?.Identity?.IsAuthenticated != true)
            return Results.Unauthorized();

        var userName = user.FindFirstValue(ClaimTypes.Name) ?? user.FindFirstValue("unique_name");
        var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var permissions = user.FindAll(PermissionClaimTypes.Permission).Select(c => c.Value).ToList();

        return Results.Ok(new { userName, roles, permissions });
    }
}
