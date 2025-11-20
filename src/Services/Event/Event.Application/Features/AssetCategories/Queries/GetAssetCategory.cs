using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Event.Application.DTOs;
using Event.Application.Interfaces;
using Event.Domain.Interfaces;
using AutoMapper;
using Event.Domain.Exceptions;

namespace Event.Application.Features.AssetCategories.Queries
{
    public class GetAssetCategoryQuery : IRequest<AssetCategoryDto>
    {
        public Guid Id { get; set; }

        public GetAssetCategoryQuery(Guid id)
        {
            Id = id;
        }
    }

    public class GetAssetCategoryQueryHandler : IRequestHandler<GetAssetCategoryQuery, AssetCategoryDto>
    {
        private readonly IAssetCategoryRepository _assetCategoryRepository;
        private readonly IMapper _mapper;

        public GetAssetCategoryQueryHandler(IAssetCategoryRepository assetCategoryRepository, IMapper mapper)
        {
            _assetCategoryRepository = assetCategoryRepository;
            _mapper = mapper;
        }

        public async Task<AssetCategoryDto> Handle(GetAssetCategoryQuery request, CancellationToken cancellationToken)
        {
            var category = await _assetCategoryRepository.GetByIdAsync(request.Id);

            if (category == null)
            {
                throw new EntityNotFoundException("Asset Category", request.Id);
            }

            return _mapper.Map<AssetCategoryDto>(category);
        }
    }
}
