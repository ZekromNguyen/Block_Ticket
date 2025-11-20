using MediatR;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Event.Application.DTOs;
using Event.Application.Features.MarketingCampaigns.Commands;
using Event.Application.Features.MarketingCampaigns.Queries;
using Event.Application.Common.Models;

namespace Event.API.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [Produces("application/json")]
    public class MarketingCampaignsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<MarketingCampaignsController> _logger;

        public MarketingCampaignsController(IMediator mediator, ILogger<MarketingCampaignsController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpPost]
        [ProducesResponseType(typeof(MarketingCampaignDto), (int)HttpStatusCode.Created)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<MarketingCampaignDto>> CreateMarketingCampaign([FromBody] CreateMarketingCampaignCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetMarketingCampaign), new { id = result.Id, version = "1.0" }, result);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(MarketingCampaignDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<MarketingCampaignDto>> GetMarketingCampaign(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetMarketingCampaignQuery(id);
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(MarketingCampaignDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<MarketingCampaignDto>> UpdateMarketingCampaign(Guid id, [FromBody] UpdateMarketingCampaignCommand command, CancellationToken cancellationToken)
        {
            if (id != command.Id) return BadRequest();
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> DeleteMarketingCampaign(Guid id, [FromQuery] Guid organizationId, CancellationToken cancellationToken)
        {
            await _mediator.Send(new DeleteMarketingCampaignCommand { Id = id, OrganizationId = organizationId }, cancellationToken);
            return NoContent();
        }

        [HttpGet("search")]
        [ProducesResponseType(typeof(PagedResult<MarketingCampaignDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<PagedResult<MarketingCampaignDto>>> SearchMarketingCampaigns([FromQuery] SearchMarketingCampaignsQuery query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
    }
}
