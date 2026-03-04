using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SecureMedicalRecordSystem.Core.Entities;
using SecureMedicalRecordSystem.Core.Enums;
using SecureMedicalRecordSystem.Infrastructure.Data;
using SecureMedicalRecordSystem.Infrastructure.Services;
using Xunit;

namespace SecureMedicalRecordSystem.Tests.Services;

public class QRTokenServiceTests
{
    private readonly ApplicationDbContext _context;
    private readonly QRTokenService _service;

    public QRTokenServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _service = new QRTokenService(_context, new NullLogger<QRTokenService>());
    }

    private async Task<Patient> CreateTestPatientAsync()
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            UserName = "test@example.com",
            FirstName = "Test",
            LastName = "Patient",
            Role = "Patient",
            IsActive = true
        };

        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            DateOfBirth = DateTime.UtcNow.AddYears(-30),
            Gender = "Male"
        };

        await _context.Users.AddAsync(user);
        await _context.Patients.AddAsync(patient);
        await _context.SaveChangesAsync();

        return patient;
    }

    [Fact]
    public async Task GenerateNormalAccessTokenAsync_ShouldCreateActiveToken()
    {
        // Arrange
        var patient = await CreateTestPatientAsync();

        // Act
        var result = await _service.GenerateNormalAccessTokenAsync(patient.Id);

        // Assert
        Assert.NotNull(result.Token);
        Assert.True(result.Token.Length > 40);
        Assert.True(result.ExpiresAt > DateTime.UtcNow);

        var dbToken = await _context.QRTokens.FirstOrDefaultAsync(t => t.Token == result.Token);
        Assert.NotNull(dbToken);
        Assert.Equal(patient.Id, dbToken.PatientId);
        Assert.Equal(QRTokenType.Normal, dbToken.TokenType);
        Assert.True(dbToken.IsActive);
    }

    [Fact]
    public async Task GenerateEmergencyAccessTokenAsync_ShouldCreateActiveTokenWithLongExpiry()
    {
        // Arrange
        var patient = await CreateTestPatientAsync();

        // Act
        var result = await _service.GenerateEmergencyAccessTokenAsync(patient.Id);

        // Assert
        var dbToken = await _context.QRTokens.FirstAsync(t => t.Token == result.Token);
        Assert.Equal(QRTokenType.Emergency, dbToken.TokenType);
        Assert.True(result.ExpiresAt > DateTime.UtcNow.AddDays(360));
    }

    [Fact]
    public async Task ValidateTokenAsync_ShouldReturnSuccessForValidToken()
    {
        // Arrange
        var patient = await CreateTestPatientAsync();
        var (token, _) = await _service.GenerateNormalAccessTokenAsync(patient.Id);

        // Act
        var (isValid, tokenData) = await _service.ValidateTokenAsync(token);

        // Assert
        Assert.True(isValid);
        Assert.NotNull(tokenData);
        Assert.Equal(1, tokenData.AccessCount);
        Assert.NotNull(tokenData.LastAccessedAt);
        Assert.NotNull(tokenData.Patient);
        Assert.NotNull(tokenData.Patient.User);
    }

    [Fact]
    public async Task ValidateTokenAsync_ShouldReturnInvalidForExpiredToken()
    {
        // Arrange
        var patient = await CreateTestPatientAsync();
        var qrToken = new QRToken
        {
            PatientId = patient.Id,
            Token = "expired-token",
            TokenType = QRTokenType.Normal,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-5),
            IsActive = true
        };
        await _context.QRTokens.AddAsync(qrToken);
        await _context.SaveChangesAsync();

        // Act
        var (isValid, _) = await _service.ValidateTokenAsync("expired-token");

        // Assert
        Assert.False(isValid);
        var dbToken = await _context.QRTokens.FirstOrDefaultAsync(t => t.Token == "expired-token");
        Assert.False(dbToken!.IsActive);
    }

    [Fact]
    public async Task RevokeTokenAsync_ShouldDeactivateToken()
    {
        // Arrange
        var patient = await CreateTestPatientAsync();
        var (token, _) = await _service.GenerateNormalAccessTokenAsync(patient.Id);

        // Act
        var result = await _service.RevokeTokenAsync(token);

        // Assert
        Assert.True(result);
        var dbToken = await _context.QRTokens.FirstOrDefaultAsync(t => t.Token == token);
        Assert.False(dbToken!.IsActive);
    }
}
