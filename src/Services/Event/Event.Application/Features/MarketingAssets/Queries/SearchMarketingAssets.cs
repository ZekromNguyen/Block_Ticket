using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Event.Application.DTOs;
using Event.Application.Interfaces;
using Event.Domain.Interfaces;
using AutoMapper;
using Event.Application.Common.Models;
using System.Linq;

namespace Event.Application.Features.MarketingAssets.Queries
{
    public class SearchMarketingAssetsQuery : IRequest<PagedResult<MarketingAssetDto>>
    {
        public Guid OrganizationId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchText { get; set; }
        public Guid? CategoryId { get; set; }
        public string? AssetType { get; set; }
    }

    public class SearchMarketingAssetsQueryHandler : IRequestHandler<SearchMarketingAssetsQuery, PagedResult<MarketingAssetDto>>
    {
        private readonly IMarketingAssetRepository _marketingAssetRepository;
        private readonly IMapper _mapper;

        public SearchMarketingAssetsQueryHandler(IMarketingAssetRepository marketingAssetRepository, IMapper mapper)
        {
            _marketingAssetRepository = marketingAssetRepository;
            _mapper = mapper;
        }

        public async Task<PagedResult<MarketingAssetDto>> Handle(SearchMarketingAssetsQuery request, CancellationToken cancellationToken)
        {
            // In a real implementation, you would build a dynamic query based on the search parameters.
            // For now, we'll just return all assets for the organization.
            var assets = await _marketingAssetRepository.GetByOrganizationAsync(request.OrganizationId);
            var assetDtos = _mapper.Map<List<MarketingAssetDto>>(assets);

            return new PagedResult<MarketingAssetDto>(assetDtos, assetDtos.Count(), request.PageNumber, request.PageSize);
        }
    }
}
