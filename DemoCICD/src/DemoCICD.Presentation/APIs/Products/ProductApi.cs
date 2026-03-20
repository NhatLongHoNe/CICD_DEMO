using Asp.Versioning.Builder;
using DemoCICD.Contract.Abstractions.Shared;
using DemoCICD.Contract.Extensions;
using DemoCICD.Contract.Services.V1.Product;
using DemoCICD.Infrastructure.Authorization;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DemoCICD.Presentation.APIs.Products;

public static class ProductApi
{
    private const string BaseUrl = "/api/minimal/v{version:apiVersion}/products";

    public static IVersionedEndpointRouteBuilder MapProductApiV1(this IVersionedEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup(BaseUrl).HasApiVersion(1).RequireAuthorization();

        group.MapPost(string.Empty, CreateProducts).RequireAuthorization(PermissionPolicy.Name(ProductPermissions.Create));
        group.MapGet(string.Empty, GetProducts).RequireAuthorization(PermissionPolicy.Name(ProductPermissions.View));
        group.MapGet("{productId}", GetProductsById).RequireAuthorization(PermissionPolicy.Name(ProductPermissions.View));
        group.MapDelete("{productId}", DeleteProducts).RequireAuthorization(PermissionPolicy.Name(ProductPermissions.Delete));
        group.MapPut("{productId}", UpdateProducts).RequireAuthorization(PermissionPolicy.Name(ProductPermissions.Update));

        return builder;
    }

    public static IVersionedEndpointRouteBuilder MapProductApiV2(this IVersionedEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup(BaseUrl).HasApiVersion(2).RequireAuthorization();

        group.MapPost(string.Empty, CreateProducts).RequireAuthorization(PermissionPolicy.Name(ProductPermissions.Create));
        group.MapGet(string.Empty, GetProducts).RequireAuthorization(PermissionPolicy.Name(ProductPermissions.View));

        return builder;
    }

    public static async Task<IResult> CreateProducts(ISender sender, [FromBody] Command.CreateProductCommand CreateProduct)
    {
        var result = await sender.Send(CreateProduct);

        if (result.IsFailure)
            return HandlerFailure(result);

        return Results.Ok(result);
    }

    private static IResult HandlerFailure(Result result) =>
        result switch
        {
            { IsSuccess: true } => throw new InvalidOperationException(),
            IValidationResult validationResult =>
                Results.BadRequest(
                    CreateProblemDetails(
                        "Validation Error", StatusCodes.Status400BadRequest,
                        result.Error,
                        validationResult.Errors)),
            _ =>
                Results.BadRequest(
                    CreateProblemDetails(
                        "Bab Request", StatusCodes.Status400BadRequest,
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

    public static async Task<IResult> GetProducts(ISender sender, string? serchTerm = null,
        string? sortColumn = null,
        string? sortOrder = null,
        string? sortColumnAndOrder = null,
        int pageIndex = 1,
        int pageSize = 10)
    {
        var result = await sender.Send(new Query.GetProductsQuery(serchTerm, sortColumn,
            SortOrderExtension.ConvertStringToSortOrder(sortOrder),
            SortOrderExtension.ConvertStringToSortOrderV2(sortColumnAndOrder),
            pageIndex,
            pageSize));
        return Results.Ok(result);
    }

    public static async Task<IResult> GetProductsById(ISender sender, Guid productId)
    {
        var result = await sender.Send(new Query.GetProductByIdQuery(productId));
        return Results.Ok(result);
    }

    public static async Task<IResult> DeleteProducts(ISender sender, Guid productId)
    {
        var result = await sender.Send(new Command.DeleteProductCommand(productId));
        return Results.Ok(result);
    }

    public static async Task<IResult> UpdateProducts(ISender sender, Guid productId, [FromBody] Command.UpdateProductCommand updateProduct)
    {
        var updateProductCommand = new Command.UpdateProductCommand(productId, updateProduct.Name, updateProduct.Price, updateProduct.Description);
        var result = await sender.Send(updateProductCommand);
        return Results.Ok(result);
    }
}
