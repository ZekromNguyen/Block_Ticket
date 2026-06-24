using Microsoft.EntityFrameworkCore;
using Ticketing.Domain.Entities;

namespace Ticketing.Infrastructure.Persistence;

/// <summary>
/// Provides seed data for local development: sample users, events, ticket types,
/// and a sold-out scenario for exercising purchase, mint, verification, resale,
/// and refund flows without external dependencies.
/// </summary>
public static class DevSeedData
{
    public static async Task SeedAsync(TicketingDbContext db, CancellationToken ct = default)
    {
        if (await db.Reservations.AnyAsync(ct))
        {
            return;
        }

        var eventId1 = Guid.NewGuid();
        var eventId2 = Guid.NewGuid();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var ticketTypeId1 = Guid.NewGuid();
        var ticketTypeId2 = Guid.NewGuid();
        var ticketTypeId3 = Guid.NewGuid();

        // ── Scenario 1: Active event with available tickets ──
        var reservation1 = new Reservation(userId1, eventId1, "USD", "seed-idem-001", "seed-lock-001", DateTime.UtcNow.AddMinutes(30));
        reservation1.AddItem(ticketTypeId1, "General Admission", 49.99m, 2);
        reservation1.AddItem(ticketTypeId2, "VIP", 149.99m, 1);
        db.Reservations.Add(reservation1);

        // ── Scenario 2: Sold-out event — expired reservation to exercise waitlist ──
        var reservation2 = new Reservation(userId1, eventId2, "USD", "seed-idem-002", "seed-lock-002", DateTime.UtcNow.AddMinutes(30));
        reservation2.AddItem(ticketTypeId3, "Standing Room", 29.99m, 4);
        reservation2.MarkExpired();
        db.Reservations.Add(reservation2);

        // ── Scenario 3: Confirmed reservation with tickets ──
        var reservation3 = new Reservation(userId2, eventId1, "USD", "seed-idem-003", "seed-lock-003", DateTime.UtcNow.AddMinutes(30));
        reservation3.AddItem(ticketTypeId1, "General Admission", 49.99m, 1);
        reservation3.SetPaymentIntent("pi_seed_confirmed");
        db.Reservations.Add(reservation3);
        var confirmedTickets = reservation3.Confirm("txn_seed_001", "{}");

        // ── Scenario 4: Refunded ticket ──
        var reservation4 = new Reservation(userId2, eventId2, "USD", "seed-idem-004", "seed-lock-004", DateTime.UtcNow.AddMinutes(30));
        reservation4.AddItem(ticketTypeId3, "Standing Room", 29.99m, 1);
        reservation4.SetPaymentIntent("pi_seed_refund");
        db.Reservations.Add(reservation4);
        var refundedTickets = reservation4.Confirm("txn_seed_002", "{}");
        foreach (var t in refundedTickets)
        {
            t.MarkRefunded(29.99m, "Seed scenario refund");
        }

        // ── Scenario 5: Ticket on resale ──
        var reservation5 = new Reservation(userId1, eventId1, "USD", "seed-idem-005", "seed-lock-005", DateTime.UtcNow.AddMinutes(30));
        reservation5.AddItem(ticketTypeId2, "VIP", 149.99m, 1);
        reservation5.SetPaymentIntent("pi_seed_resale");
        db.Reservations.Add(reservation5);
        var resaleTickets = reservation5.Confirm("txn_seed_003", "{}");
        foreach (var t in resaleTickets)
        {
            t.ListForResale(120.00m);
        }

        // ── Waiting list entry for sold-out scenario ──
        var waitingEntry = new WaitingListEntry(userId1, eventId2, ticketTypeId3);
        db.WaitingListEntries.Add(waitingEntry);

        await db.SaveChangesAsync(ct);
    }
}
