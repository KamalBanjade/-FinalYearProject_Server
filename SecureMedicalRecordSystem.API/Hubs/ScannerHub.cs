using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SecureMedicalRecordSystem.Core.Entities;
using SecureMedicalRecordSystem.Infrastructure.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SecureMedicalRecordSystem.API.Hubs;

[Authorize]
public class ScannerHub : Hub<IScannerHubClient>
{
    private readonly ApplicationDbContext _context;

    public ScannerHub(ApplicationDbContext context)
    {
        _context = context;
    }
    public async Task RegisterDesktop(string sessionId, string userIdString)
    {
        if (!Guid.TryParse(userIdString, out var userId))
        {
            await Clients.Caller.ScanError("Invalid user ID");
            return;
        }

        // Get the actual Doctor profile ID
        var doctorId = await _context.Doctors
            .Where(d => d.UserId == userId)
            .Select(d => d.Id)
            .FirstOrDefaultAsync();

        if (doctorId == Guid.Empty)
        {
            await Clients.Caller.ScanError("Doctor profile not found");
            return;
        }

        var existingSession = await _context.DesktopSessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);

        if (existingSession != null)
        {
            existingSession.WebSocketConnectionId = Context.ConnectionId;
            existingSession.DoctorId = doctorId;
            existingSession.LastActivityAt = DateTime.UtcNow;
            existingSession.IsActive = true;
            existingSession.ExpiresAt = DateTime.UtcNow.AddHours(8);
        }
        else
        {
            var session = new DesktopSession
            {
                SessionId = sessionId,
                DoctorId = doctorId,
                WebSocketConnectionId = Context.ConnectionId,
                ExpiresAt = DateTime.UtcNow.AddHours(8),
                LastActivityAt = DateTime.UtcNow,
                IsActive = true
            };
            await _context.DesktopSessions.AddAsync(session);
        }
        
        await _context.SaveChangesAsync();
        
        await Clients.Caller.DesktopRegistered(new {
            sessionId = sessionId,
            status = "ready"
        });
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;
        
        var sessions = await _context.DesktopSessions
            .Where(s => s.WebSocketConnectionId == connectionId && s.IsActive)
            .ToListAsync();

        foreach (var session in sessions)
        {
            session.IsActive = false;
        }

        if (sessions.Any())
        {
            await _context.SaveChangesAsync();
        }

        await base.OnDisconnectedAsync(exception);
    }
}
