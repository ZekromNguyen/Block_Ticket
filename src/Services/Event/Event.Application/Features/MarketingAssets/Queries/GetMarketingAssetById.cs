using MediatR;
using Event.Application.DTOs;
using System.Threading;
using System.Threading.Tasks;
using Event.Domain.Interfaces;
using AutoMapper;
using Event.Domain.Exceptions;
using System;

namespace Event.Application.Features.MarketingAssets.Queries
{
    public class GetMarketingAssetByIdQuery : IRequest<MarketingAssetDto>
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
    }

    public class GetMarketingAssetByIdQueryHandler : IRequestHandler<GetMarketingAssetByIdQuery, MarketingAssetDto>
    {
        private readonly IMarketingAssetRepository _marketingAssetRepository;
        private readonly IMapper _mapper;

        public GetMarketingAssetByIdQueryHandler(IMarketingAssetRepository marketingAssetRepository, IMapper mapper)
        {
            _marketingAssetRepository = marketingAssetRepository;
            _mapper = mapper;
        }

        public async Task<MarketingAssetDto> Handle(GetMarketingAssetByIdQuery request, CancellationToken cancellationToken)
        {
            var asset = await _marketingAssetRepository.GetByIdAsync(request.Id);

            if (asset == null || asset.OrganizationId != request.OrganizationId)
            {
                throw new EntityNotFoundException("Marketing Asset", request.Id);
            }

            return _mapper.Map<MarketingAssetDto>(asset);
        }
    }
}
