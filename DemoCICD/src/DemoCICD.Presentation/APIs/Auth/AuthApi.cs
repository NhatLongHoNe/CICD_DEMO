using Asp.Versioning.Builder;
using DemoCICD.Contract.Abstractions.Shared;
using DemoCICD.Contract.Services.V1.Auth;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DemoCICD.Presentation.APIs.Auth;

public static class AuthApi
{
    private const string BaseUrl = "/api/v{version:apiVersion}/auth";

    public static IVersionedEndpointRouteBuilder MapAuthApi(this IVersionedEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup(BaseUrl).HasApiVersion(1);

        group.MapPost("login", Login).WithName("Login");
        group.MapPost("refresh", RefreshToken).WithName("RefreshToken");

        return builder;
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

    private static IResult HandlerFailure(Result result) =>
        result switch
        {
            { IsSuccess: true } => throw new InvalidOperationException(),
            IValidationResult validationResult =>
                Results.BadRequest(CreateProblemDetails(
                    "Validation Error",
                    StatusCodes.Status400BadRequest,
                    result.Error,
                    validationResult.Errors)),
            { Error.Code: "Auth.InvalidCredentials" or "Auth.InvalidRefreshToken" or "Auth.UserNotFound" } =>
                Results.Json(
                    CreateProblemDetails("Unauthorized", StatusCodes.Status401Unauthorized, result.Error),
                    statusCode: StatusCodes.Status401Unauthorized),
            _ => Results.BadRequest(CreateProblemDetails(
                "Bad Request",
                StatusCodes.Status400BadRequest,
                result.Error))
        };

    private static ProblemDetails CreateProblemDetails(
        string title,
        int status,
        Error error,
        Error[]? errors = null) =>
        new()
        {
            Title = title,
            Type = error.Code,
            Detail = error.Message,
            Status = status,
            Extensions = { { nameof(errors), errors } }
        };
}
