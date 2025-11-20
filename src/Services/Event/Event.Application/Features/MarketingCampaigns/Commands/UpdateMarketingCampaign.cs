using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Event.Application.DTOs;
using Event.Application.Interfaces;
using Event.Domain.Interfaces;
using Event.Domain.Exceptions;
using AutoMapper;

namespace Event.Application.Features.MarketingCampaigns.Commands
{
    public class UpdateMarketingCampaignCommand : IRequest<MarketingCampaignDto>
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public Guid OrganizationId { get; set; }
    }

    public class UpdateMarketingCampaignCommandHandler : IRequestHandler<UpdateMarketingCampaignCommand, MarketingCampaignDto>
    {
        private readonly IMarketingCampaignRepository _marketingCampaignRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public UpdateMarketingCampaignCommandHandler(IMarketingCampaignRepository marketingCampaignRepository, IUnitOfWork unitOfWork, IMapper mapper)
        {
            _marketingCampaignRepository = marketingCampaignRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<MarketingCampaignDto> Handle(UpdateMarketingCampaignCommand request, CancellationToken cancellationToken)
        {
            var campaign = await _marketingCampaignRepository.GetByIdAsync(request.Id);

            if (campaign == null || campaign.OrganizationId != request.OrganizationId)
            {
                throw new EntityNotFoundException("Marketing Campaign", request.Id);
            }

            campaign.UpdateBasicInfo(request.Name, request.Description);
            campaign.UpdateSchedule(request.StartDate, request.EndDate);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<MarketingCampaignDto>(campaign);
        }
    }
}
