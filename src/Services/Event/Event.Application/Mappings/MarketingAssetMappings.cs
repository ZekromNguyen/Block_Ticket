using AutoMapper;
using Event.Application.DTOs;
using Event.Domain.Entities;

namespace Event.Application.Mappings
{
    public class MarketingAssetMappings : Profile
    {
        public MarketingAssetMappings()
        {
            CreateMap<MarketingAsset, MarketingAssetDto>();
            CreateMap<AssetVersion, AssetVersionDto>();
            CreateMap<AssetCategory, AssetCategoryDto>()
                .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => src.Slug.Value));
            CreateMap<MarketingCampaign, MarketingCampaignDto>();
            CreateMap<CampaignVariant, CampaignVariantDto>();
        }
    }
}
