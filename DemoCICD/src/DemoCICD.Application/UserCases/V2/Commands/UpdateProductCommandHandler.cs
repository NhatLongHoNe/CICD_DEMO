using DemoCICD.Contract.Abstractions.Shared;
using DemoCICD.Domain.Abstractions.Dappers;
using DemoCICD.Domain.Exceptions;
using DemoCICD.Persistence;
using DemoCICD.Contract.Services.V2.Product;
using DemoCICD.Domain.Entities;

namespace DemoCICD.Application.UserCases.V2.Commands.Product;

public sealed class UpdateProductCommandHandler : ICommandHandler<Command.UpdateProductCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(Command.UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(request.Id)
            ?? throw new ProductException.ProductNotFoundException(request.Id);

        product.Update(request.Name, request.Price, request.Description);

        var result = await _unitOfWork.Products.UpdateAsync(product);

        return Result.Success(result);
    }
}
