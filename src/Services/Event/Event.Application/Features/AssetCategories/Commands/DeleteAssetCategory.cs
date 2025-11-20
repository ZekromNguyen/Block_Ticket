using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Event.Application.Interfaces;
using Event.Domain.Interfaces;
using Event.Domain.Exceptions;

namespace Event.Application.Features.AssetCategories.Commands
{
    public class DeleteAssetCategoryCommand : IRequest<Unit>
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
    }

    public class DeleteAssetCategoryCommandHandler : IRequestHandler<DeleteAssetCategoryCommand, Unit>
    {
        private readonly IAssetCategoryRepository _assetCategoryRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteAssetCategoryCommandHandler(IAssetCategoryRepository assetCategoryRepository, IUnitOfWork unitOfWork)
        {
            _assetCategoryRepository = assetCategoryRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(DeleteAssetCategoryCommand request, CancellationToken cancellationToken)
        {
            var category = await _assetCategoryRepository.GetByIdAsync(request.Id);

            if (category == null || category.OrganizationId != request.OrganizationId)
            {
                throw new EntityNotFoundException("Asset Category", request.Id);
            }

            // In a real implementation, you would check for cascading deletes or child entities.
            _assetCategoryRepository.SoftDelete(category);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
