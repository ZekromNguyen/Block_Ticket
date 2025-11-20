using MediatR;
using Event.Application.DTOs;
using System.Threading;
using System.Threading.Tasks;
using Event.Domain.Interfaces;
using Event.Domain.Entities;
using AutoMapper;
using System;
using System.Collections.Generic;
using Event.Domain.Enums;

namespace Event.Application.Features.MarketingCampaigns.Commands
{
    public class CreateMarketingCampaignCommand : IRequest<MarketingCampaignDto>
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public Guid OrganizationId { get; set; }
    }

    public class CreateMarketingCampaignCommandHandler : IRequestHandler<CreateMarketingCampaignCommand, MarketingCampaignDto>
    {
        private readonly IMarketingCampaignRepository _marketingCampaignRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CreateMarketingCampaignCommandHandler(
            IMarketingCampaignRepository marketingCampaignRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _marketingCampaignRepository = marketingCampaignRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<MarketingCampaignDto> Handle(CreateMarketingCampaignCommand request, CancellationToken cancellationToken)
        {
            var newCampaign = new MarketingCampaign(
                request.Name,
                request.Description,
                request.OrganizationId,
                request.StartDate,
                request.EndDate,
                AssetUsageContext.Event // Default context
            );

            await _marketingCampaignRepository.AddAsync(newCampaign);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<MarketingCampaignDto>(newCampaign);
        }
    }
}
