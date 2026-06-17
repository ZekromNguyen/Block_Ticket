using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common.Models;
using Ticketing.Application.DTOs;

namespace Resale.Controllers;

[ApiController]
[Route("api/resale")]
[Authorize]
public sealed class ResaleController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ResaleController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("listings")]
    [AllowAnonymous]
    public Task<ActionResult<ApiResponse<IReadOnlyCollection<TicketDto>>>> GetListings([FromQuery] Guid? eventId, CancellationToken cancellationToken)
    {
        var path = eventId.HasValue
            ? $"/api/resale/listings?eventId={eventId.Value}"
            : "/api/resale/listings";

        return ForwardGetAsync<IReadOnlyCollection<TicketDto>>(path, cancellationToken);
    }

    [HttpPost("list")]
    public Task<ActionResult<ApiResponse<TicketDto>>> ListTicket(ResaleListTicketRequest request, CancellationToken cancellationToken)
    {
        return ForwardPostAsync<TicketDto>("/api/resale/list", request, cancellationToken);
    }

    [HttpPost("{ticketId:guid}/purchase")]
    public Task<ActionResult<ApiResponse<TicketDto>>> Purchase(Guid ticketId, ResalePurchaseTicketRequest request, CancellationToken cancellationToken)
    {
        return ForwardPostAsync<TicketDto>($"/api/resale/{ticketId}/purchase", request with { TicketId = ticketId }, cancellationToken);
    }

    [HttpPost("{ticketId:guid}/cancel")]
    public Task<ActionResult<ApiResponse<TicketDto>>> Cancel(Guid ticketId, CancelResaleRequest request, CancellationToken cancellationToken)
    {
        return ForwardPostAsync<TicketDto>($"/api/resale/{ticketId}/cancel", request with { TicketId = ticketId }, cancellationToken);
    }

    [HttpPost("waiting-list/join")]
    public Task<ActionResult<ApiResponse<WaitingListEntryDto>>> JoinWaitingList(WaitingListJoinRequest request, CancellationToken cancellationToken)
    {
        return ForwardPostAsync<WaitingListEntryDto>("/api/waiting-list/join", request, cancellationToken);
    }

    [HttpDelete("waiting-list/events/{eventId:guid}/ticket-types/{ticketTypeId:guid}/users/{userId:guid}")]
    public Task<ActionResult<ApiResponse<WaitingListEntryDto>>> LeaveWaitingList(Guid eventId, Guid ticketTypeId, Guid userId, CancellationToken cancellationToken)
    {
        return ForwardDeleteAsync<WaitingListEntryDto>($"/api/waiting-list/events/{eventId}/ticket-types/{ticketTypeId}/users/{userId}", cancellationToken);
    }

    private async Task<ActionResult<ApiResponse<T>>> ForwardGetAsync<T>(string path, CancellationToken cancellationToken)
    {
        var client = CreateTicketingClient();
        var response = await client.GetAsync(path, cancellationToken);
        return await ReadResponseAsync<T>(response, cancellationToken);
    }

    private async Task<ActionResult<ApiResponse<T>>> ForwardPostAsync<T>(string path, object request, CancellationToken cancellationToken)
    {
        var client = CreateTicketingClient();
        var response = await client.PostAsJsonAsync(path, request, cancellationToken);
        return await ReadResponseAsync<T>(response, cancellationToken);
    }

    private async Task<ActionResult<ApiResponse<T>>> ForwardDeleteAsync<T>(string path, CancellationToken cancellationToken)
    {
        var client = CreateTicketingClient();
        var response = await client.DeleteAsync(path, cancellationToken);
        return await ReadResponseAsync<T>(response, cancellationToken);
    }

    private HttpClient CreateTicketingClient()
    {
        var client = _httpClientFactory.CreateClient("ticketing");
        if (Request.Headers.TryGetValue("Authorization", out var authorization))
        {
            client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(authorization.ToString());
        }

        return client;
    }

    private async Task<ActionResult<ApiResponse<T>>> ReadResponseAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        ApiResponse<T>? envelope = null;

        try
        {
            envelope = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(cancellationToken: cancellationToken);
        }
        catch (InvalidOperationException)
        {
        }
        catch (System.Text.Json.JsonException)
        {
        }

        envelope ??= ApiResponse<T>.ErrorResult(response.IsSuccessStatusCode ? "Invalid ticketing response" : "Ticketing request failed");

        return StatusCode((int)response.StatusCode, envelope);
    }
}
