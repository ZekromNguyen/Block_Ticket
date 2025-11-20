using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Event.Application.Interfaces;
using Event.Domain.Interfaces;
using Event.Domain.Exceptions;

namespace Event.Application.Features.MarketingCampaigns.Commands
{
    public class DeleteMarketingCampaignCommand : IRequest<Unit>
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
    }

    public class DeleteMarketingCampaignCommandHandler : IRequestHandler<DeleteMarketingCampaignCommand, Unit>
    {
        private readonly IMarketingCampaignRepository _marketingCampaignRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteMarketingCampaignCommandHandler(IMarketingCampaignRepository marketingCampaignRepository, IUnitOfWork unitOfWork)
        {
            _marketingCampaignRepository = marketingCampaignRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(DeleteMarketingCampaignCommand request, CancellationToken cancellationToken)
        {
            var campaign = await _marketingCampaignRepository.GetByIdAsync(request.Id);

            if (campaign == null || campaign.OrganizationId != request.OrganizationId)
            {
                throw new EntityNotFoundException("Marketing Campaign", request.Id);
            }

            _marketingCampaignRepository.SoftDelete(campaign);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
