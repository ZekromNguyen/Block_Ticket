using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common.Models;
using Verification.Data;
using Verification.Models;

namespace Verification.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class VerificationController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly VerificationDbContext _context;

    public VerificationController(IHttpClientFactory httpClientFactory, VerificationDbContext context)
    {
        _httpClientFactory = httpClientFactory;
        _context = context;
    }

    [HttpPost("scan")]
    public async Task<ActionResult<ApiResponse<VerifyTicketResponse>>> Scan(VerifyTicketRequest request, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("ticketing");
        var response = await client.PostAsJsonAsync("/api/tickets/internal/verify", request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var failed = new VerifyTicketResponse(request.TicketId, false, "Ticketing verification failed", null);
            await SaveScanAsync(request, failed, cancellationToken);
            return StatusCode((int)response.StatusCode, ApiResponse<VerifyTicketResponse>.ErrorResult(failed.Reason));
        }

        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<VerifyTicketResponse>>(cancellationToken: cancellationToken);
        var result = envelope?.Data ?? new VerifyTicketResponse(request.TicketId, false, "Invalid ticketing response", null);

        await SaveScanAsync(request, result, cancellationToken);
        return Ok(ApiResponse<VerifyTicketResponse>.SuccessResult(result, result.Reason));
    }

    private async Task SaveScanAsync(VerifyTicketRequest request, VerifyTicketResponse result, CancellationToken cancellationToken)
    {
        _context.TicketScans.Add(new TicketScan
        {
            TicketId = request.TicketId,
            VerificationCode = request.VerificationCode,
            CheckedBy = request.CheckedBy,
            Location = request.Location,
            Accepted = result.Accepted,
            Result = result.Accepted ? "Accepted" : "Rejected",
            Reason = result.Reason
        });

        await _context.SaveChangesAsync(cancellationToken);
    }
}
