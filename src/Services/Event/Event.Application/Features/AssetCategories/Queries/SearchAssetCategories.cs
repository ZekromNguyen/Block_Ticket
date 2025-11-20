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

namespace Event.Application.Features.AssetCategories.Queries
{
    public class SearchAssetCategoriesQuery : IRequest<PagedResult<AssetCategoryDto>>
    {
        public Guid OrganizationId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchText { get; set; }
        public Guid? ParentCategoryId { get; set; }
    }

    public class SearchAssetCategoriesQueryHandler : IRequestHandler<SearchAssetCategoriesQuery, PagedResult<AssetCategoryDto>>
    {
        private readonly IAssetCategoryRepository _assetCategoryRepository;
        private readonly IMapper _mapper;

        public SearchAssetCategoriesQueryHandler(IAssetCategoryRepository assetCategoryRepository, IMapper mapper)
        {
            _assetCategoryRepository = assetCategoryRepository;
            _mapper = mapper;
        }

        public async Task<PagedResult<AssetCategoryDto>> Handle(SearchAssetCategoriesQuery request, CancellationToken cancellationToken)
        {
            // In a real implementation, you would build a dynamic query based on the search parameters.
            var categories = await _assetCategoryRepository.GetByOrganizationAsync(request.OrganizationId);
            var categoryDtos = _mapper.Map<List<AssetCategoryDto>>(categories);

            return new PagedResult<AssetCategoryDto>(categoryDtos, categoryDtos.Count(), request.PageNumber, request.PageSize);
        }
    }
}
