using Amazon;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;
using Amazon.S3;
using Amazon.Runtime;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SecureMedicalRecordSystem.API.Authorization;
using SecureMedicalRecordSystem.Infrastructure.Data;
using SecureMedicalRecordSystem.Infrastructure.Services;
using SecureMedicalRecordSystem.Core.Entities;
using SecureMedicalRecordSystem.Core.Interfaces;
using SecureMedicalRecordSystem.Infrastructure.Configuration;
using SecureMedicalRecordSystem.Infrastructure.BackgroundJobs;
using Serilog;
using Serilog.Events;

// Initial bootstrap logger
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("Logs/startup-log-.txt", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Secure Medical Record API...");
    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog correctly for the host
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "SecureMedicalRecordSystem.API")
        .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
        .WriteTo.Console()
        .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day));

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    // DI Registrations - Phase 1
    builder.Services.AddScoped<IAuditLogService, AuditLogService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IEmailService, EmailService>();
    builder.Services.AddScoped<ITotpService, TotpService>();
    builder.Services.AddScoped<ITrustedDeviceService, TrustedDeviceService>();

    // DI Registrations - Phase 2: Encryption & File Storage
    builder.Services.AddScoped<IEncryptionService, EncryptionService>();
    builder.Services.AddScoped<ITigrisStorageService, TigrisStorageService>();
    builder.Services.AddScoped<IMedicalRecordsService, MedicalRecordsService>();
    builder.Services.AddScoped<IKeyManagementService, KeyManagementService>();
    builder.Services.AddScoped<IDigitalSignatureService, DigitalSignatureService>();

    // DI Registrations - Phase 4: QR Code System
    builder.Services.AddScoped<IQRTokenService, QRTokenService>();
    builder.Services.AddScoped<IQRCodeGenerationService, QRCodeGenerationService>();
    builder.Services.AddScoped<IAccessSessionService, AccessSessionService>();
    builder.Services.AddHostedService<AccessSessionCleanupWorker>();
    builder.Services.AddHostedService<EmailReminderService>();
    builder.Services.AddHostedService<SecureMedicalRecordSystem.API.BackgroundServices.TrustedDeviceCleanupService>();



    // Configuration Models
    builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
    builder.Services.Configure<EncryptionSettings>(builder.Configuration.GetSection("EncryptionSettings"));
    builder.Services.Configure<TigrisSettings>(builder.Configuration.GetSection("TigrisSettings"));
    builder.Services.Configure<FileUploadSettings>(builder.Configuration.GetSection("FileUploadSettings"));
    builder.Services.Configure<MasterKeySettings>(builder.Configuration.GetSection("MasterKeySettings"));

    // AWS/Tigris S3 Client Registration
    var tigrisSettings = builder.Configuration.GetSection("TigrisSettings").Get<TigrisSettings>();
    if (tigrisSettings != null)
    {
        builder.Services.AddSingleton<IAmazonS3>(sp =>
        {
            var credentials = new BasicAWSCredentials(tigrisSettings.AccessKeyId, tigrisSettings.SecretAccessKey);
            var config = new AmazonS3Config
            {
                ServiceURL = tigrisSettings.ServiceUrl,
                ForcePathStyle = tigrisSettings.UsePathStyleAddressing,
                AuthenticationRegion = tigrisSettings.Region == "auto" ? "us-east-1" : tigrisSettings.Region,
                UseHttp = !tigrisSettings.ServiceUrl.StartsWith("https")
            };
            var s3Client = new AmazonS3Client(credentials, config);
            
            return s3Client;
        });
    }

    // Enable multipart form (required for file uploads)
    builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
    {
        options.MultipartBodyLengthLimit = 20 * 1024 * 1024; // 20MB
    });

    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "Secure Medical Record API", Version = "v1" });
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "Enter your JWT token below. Swagger will automatically add 'Bearer ' for you.",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT"
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
                Array.Empty<string>()
            }
        });
    });
    Console.WriteLine("ENV: " + builder.Environment.EnvironmentName);

    var conn = builder.Configuration.GetConnectionString("DefaultConnection");

    Console.WriteLine("CONNECTION STRING VALUE:");
    Console.WriteLine(conn == null ? "NULL" : "[" + conn + "]");
    // DbContext
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
            sqlOptions =>
            {
                sqlOptions.CommandTimeout(60);
                // Prevent cartesian-product SQL explosions when multiple Include() chains exist.
                // EF Core will execute separate queries per collection navigation instead of one giant JOIN.
                sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            }));

    // Identity Configuration
    builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 8;
        options.Password.RequiredUniqueChars = 4;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = true; 
        options.SignIn.RequireConfirmedPhoneNumber = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

    // JWT Configuration
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    var secretKeyString = jwtSettings["SecretKey"];
    if (string.IsNullOrEmpty(secretKeyString))
    {
        throw new InvalidOperationException("JWT SecretKey is missing from configuration.");
    }
    var secretKey = Encoding.UTF8.GetBytes(secretKeyString);

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(secretKey),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.NameIdentifier
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                context.Token = context.Request.Cookies["auth_token"];
                return Task.CompletedTask;
            }
        };
    });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
        options.AddPolicy("DoctorPolicy", policy => policy.RequireRole("Doctor"));
        options.AddPolicy("PatientPolicy", policy => policy.RequireRole("Patient"));
        options.AddPolicy("DoctorOrAdminPolicy", policy => policy.RequireRole("Doctor", "Admin"));
    });

    // CORS
    var corsOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.WithOrigins(corsOrigins).AllowAnyMethod().AllowAnyHeader().AllowCredentials();
        });
    });

    // AutoMapper
    builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

    var app = builder.Build();

    // ==========================================
    // DATABASE INITIALIZATION
    // ==========================================
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            // Skip migrations if running from EF tooling to avoid HostAbortedException
            var isEfTooling = AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name?.Contains("EntityFrameworkCore.Design") == true);
            
            if (!isEfTooling)
            {
                Log.Information("Applying Database Migrations...");
                var dbContext = services.GetRequiredService<ApplicationDbContext>();
                await dbContext.Database.MigrateAsync();
                Log.Information("Database Migrations Applied.");
            }
            Log.Information("Seeding Identity Data...");
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            await IdentitySeeder.SeedAsync(userManager, roleManager);
            Log.Information("Identity Data Seeded.");
            
            // Backfill missing SecurityStamps to fix TOTP validation crash
            Log.Information("Backfilling missing SecurityStamps...");
            var allUsers = userManager.Users.ToList();
            var usersMissingStamps = allUsers.Where(u => u.SecurityStamp == null).ToList();
                
            foreach (var u in usersMissingStamps)
            {
                await userManager.UpdateSecurityStampAsync(u);
                Log.Information("Assigned new SecurityStamp to user: {Email}", u.Email);
            }
            Log.Information("SecurityStamp backfill complete. ({Count} fixed)", usersMissingStamps.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred during database initialization.");
        }
    }

    // ==========================================
    // TIGRIS BUCKET INITIALIZATION
    // ==========================================
    try
    {
        Log.Information("Initializing Tigris storage bucket...");
        using var tigrisScope = app.Services.CreateScope();
        var tigrisService = tigrisScope.ServiceProvider.GetRequiredService<ITigrisStorageService>();
        await tigrisService.InitializeBucketAsync();
        Log.Information("Tigris bucket initialization complete.");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Tigris bucket initialization failed. File uploads may not work until bucket is created manually.");
    }


    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // Middleware
    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection(); // Only redirect to HTTPS in production
    }

    // IMPORTANT: UseCors must go BEFORE UseAuthentication and UseAuthorization!
    app.UseCors("AllowFrontend");

    app.UseMiddleware<SecureMedicalRecordSystem.API.Middleware.GlobalExceptionHandler>();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    // Ensure Storage directories exist
    Log.Information("Ensuring storage directories exist...");
    var storagePath = builder.Configuration["FileUploadSettings:StoragePath"];
    if (!string.IsNullOrEmpty(storagePath))
    {
        Directory.CreateDirectory(storagePath);
        var baseDir = Path.GetDirectoryName(storagePath);
        if (baseDir != null)
        {
            // Base storage is usually 'Storage' folder
            var storageBase = Path.GetFullPath(Path.Combine(storagePath, ".."));
            Directory.CreateDirectory(Path.Combine(storageBase, "QRCodes"));
            Directory.CreateDirectory(Path.Combine(storageBase, "Temp"));
        }
    }

    Log.Information("Application startup complete. Running...");
    app.Run();
}
catch (Exception ex) when (ex.GetType().Name != "HostAbortedException")
{
    Log.Fatal(ex, "Application terminated unexpectedly during startup.");
}
catch (Exception)
{
    throw;
}
finally
{
    Log.CloseAndFlush();
}
