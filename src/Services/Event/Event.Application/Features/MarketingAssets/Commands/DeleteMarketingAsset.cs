using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Event.Application.Interfaces;
using Event.Domain.Interfaces;
using Event.Domain.Exceptions;

namespace Event.Application.Features.MarketingAssets.Commands
{
    public class DeleteMarketingAssetCommand : IRequest<Unit>
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
    }

    public class DeleteMarketingAssetCommandHandler : IRequestHandler<DeleteMarketingAssetCommand, Unit>
    {
        private readonly IMarketingAssetRepository _marketingAssetRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteMarketingAssetCommandHandler(IMarketingAssetRepository marketingAssetRepository, IUnitOfWork unitOfWork)
        {
            _marketingAssetRepository = marketingAssetRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(DeleteMarketingAssetCommand request, CancellationToken cancellationToken)
        {
            var asset = await _marketingAssetRepository.GetByIdAsync(request.Id);

            if (asset == null || asset.OrganizationId != request.OrganizationId)
            {
                throw new EntityNotFoundException("Marketing Asset", request.Id);
            }

            _marketingAssetRepository.SoftDelete(asset);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
