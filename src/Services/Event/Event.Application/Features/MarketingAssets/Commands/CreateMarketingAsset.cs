using MediatR;
using Event.Application.DTOs;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;
using Event.Domain.Interfaces;
using Event.Domain.Entities;
using AutoMapper;
using Event.Application.Interfaces;
using Event.Domain.ValueObjects;
using System.Collections.Generic;
using System;
using Event.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Event.Application.Features.MarketingAssets.Commands
{
    public class CreateMarketingAssetCommand : IRequest<MarketingAssetDto>
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public Guid? CategoryId { get; set; }
        public IFormFile File { get; set; } = null!;
        public List<string> Tags { get; set; } = new();
        public Guid OrganizationId { get; set; }
    }

    public class CreateMarketingAssetCommandHandler : IRequestHandler<CreateMarketingAssetCommand, MarketingAssetDto>
    {
        private readonly IMarketingAssetRepository _marketingAssetRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        // private readonly IFileStorageService _fileStorageService; // To be implemented

        public CreateMarketingAssetCommandHandler(
            IMarketingAssetRepository marketingAssetRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper
            /* IFileStorageService fileStorageService */)
        {
            _marketingAssetRepository = marketingAssetRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            // _fileStorageService = fileStorageService;
        }

        public async Task<MarketingAssetDto> Handle(CreateMarketingAssetCommand request, CancellationToken cancellationToken)
        {
            // In a real implementation, you would upload the file to a storage provider.
            // For now, we'll just create the entity with placeholder file info.

            var newAsset = new MarketingAsset(
                request.Name,
                request.Description,
                (AssetType)Enum.Parse(typeof(AssetType), request.Type, true),
                request.OrganizationId,
                new AssetFileInfo(request.File.FileName, request.File.ContentType, request.File.Length, ""),
                new AssetStorageInfo("Local", $"assets/{request.File.FileName}", $"http://localhost/assets/{request.File.FileName}"),
                request.CategoryId
            );

            newAsset.SetMetadata(new AssetMetadata(new Dictionary<string, object>(), request.Tags));

            await _marketingAssetRepository.AddAsync(newAsset);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<MarketingAssetDto>(newAsset);
        }
    }
}
