using MediatR;
using Event.Application.DTOs;
using System.Threading;
using System.Threading.Tasks;
using Event.Domain.Interfaces;
using Event.Domain.Entities;
using AutoMapper;
using System;

namespace Event.Application.Features.AssetCategories.Commands
{
    public class CreateAssetCategoryCommand : IRequest<AssetCategoryDto>
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid? ParentCategoryId { get; set; }
        public Guid OrganizationId { get; set; }
    }

    public class CreateAssetCategoryCommandHandler : IRequestHandler<CreateAssetCategoryCommand, AssetCategoryDto>
    {
        private readonly IAssetCategoryRepository _assetCategoryRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CreateAssetCategoryCommandHandler(
            IAssetCategoryRepository assetCategoryRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _assetCategoryRepository = assetCategoryRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<AssetCategoryDto> Handle(CreateAssetCategoryCommand request, CancellationToken cancellationToken)
        {
            var newCategory = new AssetCategory(
                request.Name,
                request.Description,
                request.OrganizationId,
                request.ParentCategoryId
            );

            await _assetCategoryRepository.AddAsync(newCategory);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<AssetCategoryDto>(newCategory);
        }
    }
}
