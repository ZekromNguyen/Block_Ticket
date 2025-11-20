using MediatR;
using Event.Application.DTOs;
using System.Threading;
using System.Threading.Tasks;
using Event.Domain.Interfaces;
using AutoMapper;
using Event.Domain.Exceptions;
using System;

namespace Event.Application.Features.MarketingCampaigns.Queries
{
    public class GetMarketingCampaignByIdQuery : IRequest<MarketingCampaignDto>
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
    }

    public class GetMarketingCampaignByIdQueryHandler : IRequestHandler<GetMarketingCampaignByIdQuery, MarketingCampaignDto>
    {
        private readonly IMarketingCampaignRepository _marketingCampaignRepository;
        private readonly IMapper _mapper;

        public GetMarketingCampaignByIdQueryHandler(IMarketingCampaignRepository marketingCampaignRepository, IMapper mapper)
        {
            _marketingCampaignRepository = marketingCampaignRepository;
            _mapper = mapper;
        }

        public async Task<MarketingCampaignDto> Handle(GetMarketingCampaignByIdQuery request, CancellationToken cancellationToken)
        {
            var campaign = await _marketingCampaignRepository.GetByIdAsync(request.Id);

            if (campaign == null || campaign.OrganizationId != request.OrganizationId)
            {
                throw new EntityNotFoundException("Marketing Campaign", request.Id);
            }

            return _mapper.Map<MarketingCampaignDto>(campaign);
        }
    }
}
