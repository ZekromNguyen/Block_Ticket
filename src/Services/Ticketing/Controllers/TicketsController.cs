using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ticketing.Api.Data;
using Ticketing.Api.Models;
using Shared.Common.Models;
using Microsoft.AspNetCore.Authorization;
using MassTransit;
using Shared.Contracts.Commands;
using Shared.Contracts.Events;

namespace Ticketing.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TicketsController : ControllerBase
{
    private readonly TicketingDbContext _context;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<TicketsController> _logger;

    public TicketsController(
        TicketingDbContext context,
        IPublishEndpoint publishEndpoint,
        ILogger<TicketsController> logger)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    [HttpPost("purchase")]
    public async Task<ActionResult<ApiResponse<PurchaseResponse>>> PurchaseTicket(PurchaseTicketRequest request)
    {
        // Generate unique ticket number
        var ticketNumber = GenerateTicketNumber();

        var ticket = new Ticket
        {
            EventId = request.EventId,
            UserId = request.UserId,
            TicketNumber = ticketNumber,
            Price = request.Price,
            Status = TicketStatus.Pending
        };

        var transaction = new TicketTransaction
        {
            TicketId = ticket.Id,
            Type = TransactionType.Purchase,
            Amount = request.Price,
            PaymentMethod = request.PaymentMethod,
            Status = TransactionStatus.Pending
        };

        _context.Tickets.Add(ticket);
        _context.TicketTransactions.Add(transaction);
        await _context.SaveChangesAsync();

        // Simulate payment processing
        var paymentSuccessful = await ProcessPayment(request, transaction);

        if (paymentSuccessful)
        {
            ticket.Status = TicketStatus.Paid;
            transaction.Status = TransactionStatus.Completed;
            transaction.PaymentTransactionId = Guid.NewGuid().ToString();
            
            await _context.SaveChangesAsync();

            // Send command to mint ticket on blockchain
            await _publishEndpoint.Publish(new MintTicketCommand(
                ticket.Id,
                ticket.EventId,
                request.UserWalletAddress ?? "default-wallet",
                ticket.Price,
                $"{{\"ticketNumber\":\"{ticket.TicketNumber}\",\"eventId\":\"{ticket.EventId}\"}}"
            ));

            // Publish ticket purchased event
            await _publishEndpoint.Publish(new TicketPurchased(
                ticket.Id,
                ticket.EventId,
                ticket.UserId,
                ticket.Price,
                DateTime.UtcNow
            ));

            _logger.LogInformation("Ticket {TicketNumber} purchased successfully for user {UserId}", 
                ticket.TicketNumber, ticket.UserId);

            var response = new PurchaseResponse
            {
                TicketId = ticket.Id,
                TicketNumber = ticket.TicketNumber,
                Status = ticket.Status.ToString(),
                TransactionId = transaction.PaymentTransactionId
            };

            return Ok(ApiResponse<PurchaseResponse>.SuccessResult(response, "Ticket purchased successfully"));
        }
        else
        {
            ticket.Status = TicketStatus.Cancelled;
            transaction.Status = TransactionStatus.Failed;
            transaction.FailureReason = "Payment processing failed";
            
            await _context.SaveChangesAsync();

            return BadRequest(ApiResponse<PurchaseResponse>.ErrorResult("Payment processing failed"));
        }
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<Ticket>>>> GetUserTickets(Guid userId)
    {
        var tickets = await _context.Tickets
            .Include(t => t.Transactions)
            .Where(t => t.UserId == userId && !t.IsDeleted)
            .ToListAsync();

        return Ok(ApiResponse<IEnumerable<Ticket>>.SuccessResult(tickets));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<Ticket>>> GetTicket(Guid id)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Transactions)
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);

        if (ticket == null)
        {
            return NotFound(ApiResponse<Ticket>.ErrorResult("Ticket not found"));
        }

        return Ok(ApiResponse<Ticket>.SuccessResult(ticket));
    }

    private async Task<bool> ProcessPayment(PurchaseTicketRequest request, TicketTransaction transaction)
    {
        // Simulate payment processing delay
        await Task.Delay(1000);
        
        // Simulate 95% success rate
        return Random.Shared.Next(1, 101) <= 95;
    }

    private string GenerateTicketNumber()
    {
        return $"TKT-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(100000, 999999)}";
    }
}

public record PurchaseTicketRequest(
    Guid EventId,
    Guid UserId,
    decimal Price,
    string PaymentMethod,
    string? UserWalletAddress
);

public record PurchaseResponse
{
    public Guid TicketId { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? TransactionId { get; set; }
}
