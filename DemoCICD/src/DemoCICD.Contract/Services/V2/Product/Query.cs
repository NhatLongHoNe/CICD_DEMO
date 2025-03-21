using DemoCICD.Contract.Abstractions.Shared;
using static DemoCICD.Contract.Services.V2.Product.Response;

namespace DemoCICD.Contract.Services.V2.Product;

public static class Query
{
    public record GetProductsQuery() : IQuery<Result<List<ProductResponse>>>;
    public record GetProductByIdQuery(Guid Id) : IQuery<ProductResponse>;
}
