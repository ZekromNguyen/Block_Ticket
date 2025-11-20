using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Event.Application.DTOs;
using Event.Application.Interfaces;
using Event.Domain.Interfaces;
using AutoMapper;
using Event.Domain.Exceptions;

namespace Event.Application.Features.MarketingAssets.Queries
{
    public class GetMarketingAssetQuery : IRequest<MarketingAssetDto>
    {
        public Guid Id { get; set; }

        public GetMarketingAssetQuery(Guid id)
        {
            Id = id;
        }
    }

    public class GetMarketingAssetQueryHandler : IRequestHandler<GetMarketingAssetQuery, MarketingAssetDto>
    {
        private readonly IMarketingAssetRepository _marketingAssetRepository;
        private readonly IMapper _mapper;

        public GetMarketingAssetQueryHandler(IMarketingAssetRepository marketingAssetRepository, IMapper mapper)
        {
            _marketingAssetRepository = marketingAssetRepository;
            _mapper = mapper;
        }

        public async Task<MarketingAssetDto> Handle(GetMarketingAssetQuery request, CancellationToken cancellationToken)
        {
            var asset = await _marketingAssetRepository.GetByIdAsync(request.Id);

            if (asset == null)
            {
                throw new EntityNotFoundException("Marketing Asset", request.Id);
            }

            return _mapper.Map<MarketingAssetDto>(asset);
        }
    }
}
