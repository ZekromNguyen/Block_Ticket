using MediatR;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Event.Application.DTOs;
using Event.Application.Features.MarketingAssets.Commands;
using Event.Application.Features.MarketingAssets.Queries;
using Event.Application.Common.Models;

namespace Event.API.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/marketingassets")]
    [ApiVersion("1.0")]
    [Produces("application/json")]
    public class MarketingAssetsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<MarketingAssetsController> _logger;

        public MarketingAssetsController(IMediator mediator, ILogger<MarketingAssetsController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpPost]
        [ProducesResponseType(typeof(MarketingAssetDto), (int)HttpStatusCode.Created)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<MarketingAssetDto>> CreateMarketingAsset([FromForm] CreateMarketingAssetCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetMarketingAsset), new { id = result.Id, version = "1.0" }, result);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(MarketingAssetDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<MarketingAssetDto>> GetMarketingAsset(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetMarketingAssetQuery(id);
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(MarketingAssetDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<MarketingAssetDto>> UpdateMarketingAsset(Guid id, [FromBody] UpdateMarketingAssetCommand command, CancellationToken cancellationToken)
        {
            if (id != command.Id) return BadRequest();
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> DeleteMarketingAsset(Guid id, [FromQuery] Guid organizationId, CancellationToken cancellationToken)
        {
            await _mediator.Send(new DeleteMarketingAssetCommand { Id = id, OrganizationId = organizationId }, cancellationToken);
            return NoContent();
        }

        [HttpGet("search")]
        [ProducesResponseType(typeof(PagedResult<MarketingAssetDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<PagedResult<MarketingAssetDto>>> SearchMarketingAssets([FromQuery] SearchMarketingAssetsQuery query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
    }
}
