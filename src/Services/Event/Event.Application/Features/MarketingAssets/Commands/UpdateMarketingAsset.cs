using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Event.Application.DTOs;
using Event.Application.Interfaces;
using Event.Domain.Interfaces;
using Event.Domain.Exceptions;
using AutoMapper;

namespace Event.Application.Features.MarketingAssets.Commands
{
    public class UpdateMarketingAssetCommand : IRequest<MarketingAssetDto>
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid? CategoryId { get; set; }
        public Guid OrganizationId { get; set; }
    }

    public class UpdateMarketingAssetCommandHandler : IRequestHandler<UpdateMarketingAssetCommand, MarketingAssetDto>
    {
        private readonly IMarketingAssetRepository _marketingAssetRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public UpdateMarketingAssetCommandHandler(IMarketingAssetRepository marketingAssetRepository, IUnitOfWork unitOfWork, IMapper mapper)
        {
            _marketingAssetRepository = marketingAssetRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<MarketingAssetDto> Handle(UpdateMarketingAssetCommand request, CancellationToken cancellationToken)
        {
            var asset = await _marketingAssetRepository.GetByIdAsync(request.Id);

            if (asset == null || asset.OrganizationId != request.OrganizationId)
            {
                throw new EntityNotFoundException("Marketing Asset", request.Id);
            }

            asset.UpdateBasicInfo(request.Name, request.Description);
            // asset.SetCategory(request.CategoryId); // Assuming a method to set category exists

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<MarketingAssetDto>(asset);
        }
    }
}
