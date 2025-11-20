using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Event.Application.DTOs;
using Event.Application.Interfaces;
using Event.Domain.Interfaces;
using Event.Domain.Exceptions;
using AutoMapper;

namespace Event.Application.Features.AssetCategories.Commands
{
    public class UpdateAssetCategoryCommand : IRequest<AssetCategoryDto>
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid? ParentCategoryId { get; set; }
        public Guid OrganizationId { get; set; }
    }

    public class UpdateAssetCategoryCommandHandler : IRequestHandler<UpdateAssetCategoryCommand, AssetCategoryDto>
    {
        private readonly IAssetCategoryRepository _assetCategoryRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public UpdateAssetCategoryCommandHandler(IAssetCategoryRepository assetCategoryRepository, IUnitOfWork unitOfWork, IMapper mapper)
        {
            _assetCategoryRepository = assetCategoryRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<AssetCategoryDto> Handle(UpdateAssetCategoryCommand request, CancellationToken cancellationToken)
        {
            var category = await _assetCategoryRepository.GetByIdAsync(request.Id);

            if (category == null || category.OrganizationId != request.OrganizationId)
            {
                throw new EntityNotFoundException("Asset Category", request.Id);
            }

            // In a real implementation, you would have a method on the entity to update its properties.
            // category.UpdateDetails(request.Name, request.Description);
            // In a real implementation, you would handle reparenting logic carefully.
            // category.SetParent(request.ParentCategoryId);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<AssetCategoryDto>(category);
        }
    }
}
