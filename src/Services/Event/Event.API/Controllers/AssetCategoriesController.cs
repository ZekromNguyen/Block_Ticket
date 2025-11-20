using MediatR;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Event.Application.DTOs;
using Event.Application.Features.AssetCategories.Commands;
using Event.Application.Features.AssetCategories.Queries;
using Event.Application.Common.Models;

namespace Event.API.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [Produces("application/json")]
    public class AssetCategoriesController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<AssetCategoriesController> _logger;

        public AssetCategoriesController(IMediator mediator, ILogger<AssetCategoriesController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpPost]
        [ProducesResponseType(typeof(AssetCategoryDto), (int)HttpStatusCode.Created)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<AssetCategoryDto>> CreateAssetCategory([FromBody] CreateAssetCategoryCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetAssetCategory), new { id = result.Id, version = "1.0" }, result);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(AssetCategoryDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<AssetCategoryDto>> GetAssetCategory(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetAssetCategoryQuery(id);
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(AssetCategoryDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<AssetCategoryDto>> UpdateAssetCategory(Guid id, [FromBody] UpdateAssetCategoryCommand command, CancellationToken cancellationToken)
        {
            if (id != command.Id) return BadRequest();
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> DeleteAssetCategory(Guid id, [FromQuery] Guid organizationId, CancellationToken cancellationToken)
        {
            await _mediator.Send(new DeleteAssetCategoryCommand { Id = id, OrganizationId = organizationId }, cancellationToken);
            return NoContent();
        }

        [HttpGet("search")]
        [ProducesResponseType(typeof(PagedResult<AssetCategoryDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<PagedResult<AssetCategoryDto>>> SearchAssetCategories([FromQuery] SearchAssetCategoriesQuery query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
    }
}
