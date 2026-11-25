using BlotterSync.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlotterSync.Agent;

public static class DemoData
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlotterSyncContext>();
        if (await db.Officers.AnyAsync(o => o.Username == "demo.officer")) return;
        var officer = new Officer { Username = "demo.officer", FirstName = "Demo", LastName = "Officer",
            BadgeNumber = "DEMO-001", Role = "Officer", ActiveStatus = true };
        officer.PasswordHash = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Officer>>()
            .HashPassword(officer, Guid.NewGuid().ToString("N"));
        db.Officers.Add(officer);
        await db.SaveChangesAsync();
        var categories = await db.Categories.ToDictionaryAsync(c => c.Name, c => c.CategoryId);
        var entries = new[] {
            ("Disturbance of Peace / Noise", "M.H. del Pilar Street", "Pending", "The reporting resident stated that loud music continued from a neighboring property after 11:00 p.m. on the incident date. The source of the music and the precise house number have not yet been verified. No injury or property damage was reported."),
            ("Boundary Dispute", "Purok 3, Malanday", "Ongoing", "Two residents requested assistance regarding the position of a fence between their properties. Both parties agreed to present their available documents at a follow-up meeting. The property boundaries have not been verified."),
            ("Lost and Found", "Malanday barangay hall", "Resolved", "A resident turned in a black wallet found near the barangay hall entrance. The owner later presented matching identification and acknowledged receipt of the wallet. The handover was recorded by the desk officer."),
            ("Theft / Robbery", "Purok 1, market area", "Pending", "The complainant reported that a bicycle was missing from outside a market stall. They last saw it at approximately 8:30 a.m. The serial number, value and any available CCTV footage remain to be confirmed. No suspect was identified."),
            ("Vandalism / Property Damage", "Riverbank access road", "Ongoing", "A resident reported damage to a gate beside the riverbank access road. The time of the damage is unknown. The resident offered photographs for documentation. No person has been identified as responsible."),
            ("Disturbance of Peace / Noise", "Purok 5, Malanday", "Resolved", "A complaint about late-night construction noise was received. After a barangay-facilitated discussion, the parties agreed on daytime working hours. The complainant confirmed that the immediate concern had been addressed."),
            ("Lost and Found", "M.H. del Pilar Street", "Pending", "A blue school bag was found beside a waiting shed. It was brought to the desk officer for safekeeping. Ownership has not been established. An inventory of the contents is awaiting verification."),
            ("Other / Miscellaneous", "Purok 2, Malanday", "Dismissed", "A resident requested that a previously reported concern be withdrawn after clarifying that the item had been moved by a household member. The request and clarification were recorded by the desk officer.")
        };
        var today = DateTime.UtcNow.Date;
        for (var i = 0; i < entries.Length; i++)
        {
            var e = entries[i];
            db.BlotterRecords.Add(new BlotterRecord { TrackingNumber = $"DEMO-{today:yyyyMMdd}-{i + 1:D3}",
                CategoryId = categories[e.Item1], DeskOfficerId = officer.OfficerId,
                IncidentDate = today.AddDays(-i / 2).AddHours(8 + i), ReportedDate = today.AddDays(-i / 2).AddHours(9 + i),
                Location = e.Item2, Status = e.Item3, Narrative = e.Item4,
                ResolutionDate = e.Item3 == "Resolved" ? today : null });
        }
        await db.SaveChangesAsync();
    }
}
