using MediatR;
using Event.Application.DTOs;
using System.Threading;
using System.Threading.Tasks;
using Event.Domain.Interfaces;
using AutoMapper;
using Event.Domain.Exceptions;
using System;

namespace Event.Application.Features.AssetCategories.Queries
{
    public class GetAssetCategoryByIdQuery : IRequest<AssetCategoryDto>
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
    }

    public class GetAssetCategoryByIdQueryHandler : IRequestHandler<GetAssetCategoryByIdQuery, AssetCategoryDto>
    {
        private readonly IAssetCategoryRepository _assetCategoryRepository;
        private readonly IMapper _mapper;

        public GetAssetCategoryByIdQueryHandler(IAssetCategoryRepository assetCategoryRepository, IMapper mapper)
        {
            _assetCategoryRepository = assetCategoryRepository;
            _mapper = mapper;
        }

        public async Task<AssetCategoryDto> Handle(GetAssetCategoryByIdQuery request, CancellationToken cancellationToken)
        {
            var category = await _assetCategoryRepository.GetByIdAsync(request.Id);

            if (category == null || category.OrganizationId != request.OrganizationId)
            {
                throw new EntityNotFoundException("Asset Category", request.Id);
            }

            return _mapper.Map<AssetCategoryDto>(category);
        }
    }
}
