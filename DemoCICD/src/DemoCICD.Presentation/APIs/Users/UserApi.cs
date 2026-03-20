using Asp.Versioning.Builder;
using Carter;
using DemoCICD.Contract.Services.V1.User;
using DemoCICD.Infrastructure.Authorization;
using DemoCICD.Presentation.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace DemoCICD.Presentation.APIs.Users;

public class UserApi : ApiEndpoint, ICarterModule
{
    private const string BaseUrl = "/api/v{version:apiVersion}/users";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.NewVersionedApi("users").MapGroup(BaseUrl).HasApiVersion(1).RequireAuthorization();

        group.MapGet(string.Empty, GetUsers).RequireAuthorization(PermissionPolicy.Name(UserPermissions.View));
        group.MapGet("roles", GetRoles).RequireAuthorization(PermissionPolicy.Name(UserPermissions.View));
        group.MapGet("functions-actions", GetFunctionsActions).RequireAuthorization(PermissionPolicy.Name(UserPermissions.View));
        group.MapGet("{id:guid}", GetUserById).RequireAuthorization(PermissionPolicy.Name(UserPermissions.View));
        group.MapPost(string.Empty, CreateUser).RequireAuthorization(PermissionPolicy.Name(UserPermissions.Create));
        group.MapPut("{id:guid}", UpdateUser).RequireAuthorization(PermissionPolicy.Name(UserPermissions.Update));
        group.MapDelete("{id:guid}", DeleteUser).RequireAuthorization(PermissionPolicy.Name(UserPermissions.Delete));
    }

    public static async Task<IResult> GetUsers(ISender sender, [FromQuery] string? searchTerm, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
    {
        var result = await sender.Send(new Query.GetUsersQuery(searchTerm, pageIndex, pageSize));
        return result.IsFailure ? HandlerFailure(result) : Results.Ok(result.Value);
    }

    public static async Task<IResult> GetUserById(ISender sender, [FromRoute] Guid id)
    {
        var result = await sender.Send(new Query.GetUserByIdQuery(id));
        if (result.IsFailure)
            return HandlerFailure(result);
        return result.Value == null ? Results.NotFound() : Results.Ok(result.Value);
    }

    public static async Task<IResult> GetRoles(ISender sender)
    {
        var result = await sender.Send(new Query.GetRolesQuery());
        return result.IsFailure ? HandlerFailure(result) : Results.Ok(result.Value);
    }

    public static async Task<IResult> GetFunctionsActions(ISender sender)
    {
        var result = await sender.Send(new Query.GetFunctionsActionsQuery());
        return result.IsFailure ? HandlerFailure(result) : Results.Ok(result.Value);
    }

    public static async Task<IResult> CreateUser(ISender sender, [FromBody] Command.CreateUserCommand command)
    {
        var result = await sender.Send(command);
        if (result.IsFailure)
            return HandlerFailure(result);
        return Results.Created($"{BaseUrl}/{result.Value}", new { id = result.Value });
    }

    public static async Task<IResult> UpdateUser(ISender sender, [FromRoute] Guid id, [FromBody] Command.UpdateUserCommand command)
    {
        var cmd = command with { Id = id };
        var result = await sender.Send(cmd);
        return result.IsFailure ? HandlerFailure(result) : Results.NoContent();
    }

    public static async Task<IResult> DeleteUser(ISender sender, [FromRoute] Guid id)
    {
        var result = await sender.Send(new Command.DeleteUserCommand(id));
        return result.IsFailure ? HandlerFailure(result) : Results.NoContent();
    }
}
