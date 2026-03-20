using System.Linq.Expressions;
using AutoMapper;
using DemoCICD.Contract.Abstractions.Shared;
using DemoCICD.Contract.Enumerations;
using DemoCICD.Contract.Services.V1.Product;
using DemoCICD.Domain.Abstractions.Repositories;
using DemoCICD.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DemoCICD.Application.UserCases.V1.Queries.Product;

public sealed class GetProductsQueryHandler : IQueryHandler<Query.GetProductsQuery, PagedResult<Response.ProductResponse>>
{
    private static readonly HashSet<string> AllowedSortColumns = new(StringComparer.OrdinalIgnoreCase)
        { "id", "name", "price", "description" };

    private readonly IRepositoryBase<Domain.Entities.Product, Guid> _productRepository;
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;

    public GetProductsQueryHandler(IRepositoryBase<Domain.Entities.Product, Guid> productRepository,
        ApplicationDbContext context,
        IMapper mapper)
    {
        _productRepository = productRepository;
        _mapper = mapper;
        _context = context;
    }

    public async Task<Result<PagedResult<Response.ProductResponse>>> Handle(Query.GetProductsQuery request, CancellationToken cancellationToken)
    {
        // Dùng EF cho mọi trường hợp để tránh SQL injection và đảm bảo totalCount đúng bộ lọc
        var productsQuery = string.IsNullOrWhiteSpace(request.SearchTerm)
            ? _productRepository.FindAll()
            : _productRepository.FindAll(x => x.Name.Contains(request.SearchTerm) || x.Description.Contains(request.SearchTerm));

        if (request.SortColumnAndOrder is { } sortDict && sortDict.Count > 0)
        {
            var first = true;
            foreach (var (column, order) in sortDict)
            {
                var col = column?.Trim();
                if (string.IsNullOrEmpty(col) || !AllowedSortColumns.Contains(col))
                    continue;
                var expr = GetSortPropertyByColumn(col);
                if (first)
                {
                    productsQuery = order == SortOrder.Descending
                        ? productsQuery.OrderByDescending(expr)
                        : productsQuery.OrderBy(expr);
                    first = false;
                }
                else
                {
                    var ordered = (IOrderedQueryable<Domain.Entities.Product>)productsQuery;
                    productsQuery = order == SortOrder.Descending
                        ? ordered.ThenByDescending(expr)
                        : ordered.ThenBy(expr);
                }
            }
            if (first)
                productsQuery = productsQuery.OrderBy(p => p.Id);
        }
        else
        {
            productsQuery = request.SortOrder == SortOrder.Descending
                ? productsQuery.OrderByDescending(GetSortProperty(request.SortColumn))
                : productsQuery.OrderBy(GetSortProperty(request.SortColumn));
        }

        var products = await PagedResult<Domain.Entities.Product>.CreateAsync(productsQuery, request.PageIndex, request.PageSize);

        var result = _mapper.Map<PagedResult<Response.ProductResponse>>(products);
        return Result.Success(result);
    }

    private static Expression<Func<Domain.Entities.Product, object>> GetSortProperty(string? sortColumn)
        => GetSortPropertyByColumn(sortColumn ?? "id");

    private static Expression<Func<Domain.Entities.Product, object>> GetSortPropertyByColumn(string column)
        => (column?.ToLowerInvariant()) switch
        {
            "name" => product => product.Name,
            "price" => product => product.Price,
            "description" => product => product.Description,
            _ => product => product.Id
        };
}
