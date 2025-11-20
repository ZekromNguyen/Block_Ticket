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

namespace Event.Application.Features.MarketingCampaigns.Queries
{
    public class SearchMarketingCampaignsQuery : IRequest<PagedResult<MarketingCampaignDto>>
    {
        public Guid OrganizationId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchText { get; set; }
        public string? Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class SearchMarketingCampaignsQueryHandler : IRequestHandler<SearchMarketingCampaignsQuery, PagedResult<MarketingCampaignDto>>
    {
        private readonly IMarketingCampaignRepository _marketingCampaignRepository;
        private readonly IMapper _mapper;

        public SearchMarketingCampaignsQueryHandler(IMarketingCampaignRepository marketingCampaignRepository, IMapper mapper)
        {
            _marketingCampaignRepository = marketingCampaignRepository;
            _mapper = mapper;
        }

        public async Task<PagedResult<MarketingCampaignDto>> Handle(SearchMarketingCampaignsQuery request, CancellationToken cancellationToken)
        {
            // In a real implementation, you would build a dynamic query based on the search parameters.
            var campaigns = await _marketingCampaignRepository.GetByOrganizationAsync(request.OrganizationId);
            var campaignDtos = _mapper.Map<List<MarketingCampaignDto>>(campaigns);

            return new PagedResult<MarketingCampaignDto>(campaignDtos, campaignDtos.Count(), request.PageNumber, request.PageSize);
        }
    }
}
