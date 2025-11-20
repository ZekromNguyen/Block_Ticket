using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Event.Application.DTOs;
using Event.Application.Interfaces;
using Event.Domain.Interfaces;
using AutoMapper;
using Event.Domain.Exceptions;

namespace Event.Application.Features.MarketingCampaigns.Queries
{
    public class GetMarketingCampaignQuery : IRequest<MarketingCampaignDto>
    {
        public Guid Id { get; set; }

        public GetMarketingCampaignQuery(Guid id)
        {
            Id = id;
        }
    }

    public class GetMarketingCampaignQueryHandler : IRequestHandler<GetMarketingCampaignQuery, MarketingCampaignDto>
    {
        private readonly IMarketingCampaignRepository _marketingCampaignRepository;
        private readonly IMapper _mapper;

        public GetMarketingCampaignQueryHandler(IMarketingCampaignRepository marketingCampaignRepository, IMapper mapper)
        {
            _marketingCampaignRepository = marketingCampaignRepository;
            _mapper = mapper;
        }

        public async Task<MarketingCampaignDto> Handle(GetMarketingCampaignQuery request, CancellationToken cancellationToken)
        {
            var campaign = await _marketingCampaignRepository.GetByIdAsync(request.Id);

            if (campaign == null)
            {
                throw new EntityNotFoundException("Marketing Campaign", request.Id);
            }

            return _mapper.Map<MarketingCampaignDto>(campaign);
        }
    }
}
