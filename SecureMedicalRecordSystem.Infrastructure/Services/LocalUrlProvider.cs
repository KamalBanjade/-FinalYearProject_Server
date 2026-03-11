using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SecureMedicalRecordSystem.Core.Interfaces;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace SecureMedicalRecordSystem.Infrastructure.Services;

public class LocalUrlProvider : ILocalUrlProvider
{
    private readonly Lazy<string> _frontendBaseUrl;
    private readonly Lazy<string> _backendBaseUrl;
    private readonly Lazy<string> _localIpAddress;
    private readonly Lazy<string> _emailConfirmationLinkTemplate;
    private readonly Lazy<string> _passwordResetLinkTemplate;

    public string FrontendBaseUrl => _frontendBaseUrl.Value;
    public string BackendBaseUrl => _backendBaseUrl.Value;
    public string LocalIpAddress => _localIpAddress.Value;
    public string EmailConfirmationLinkTemplate => _emailConfirmationLinkTemplate.Value;
    public string PasswordResetLinkTemplate => _passwordResetLinkTemplate.Value;

    public LocalUrlProvider(IConfiguration configuration, ILogger<LocalUrlProvider> logger)
    {
        _localIpAddress = new Lazy<string>(() => DetectLocalIpAddress(logger));

        _frontendBaseUrl = new Lazy<string>(() =>
        {
            var configured = configuration["ApplicationUrls:FrontendUrl"];
            if (!string.IsNullOrWhiteSpace(configured))
            {
                return configured.TrimEnd('/');
            }
            logger.LogInformation("FrontendUrl not explicitly configured. Auto-detecting via local IP: {Ip}", _localIpAddress.Value);
            return $"http://{_localIpAddress.Value}:3000";
        });

        _backendBaseUrl = new Lazy<string>(() =>
        {
            var configured = configuration["ApplicationUrls:BackendUrl"];
            if (!string.IsNullOrWhiteSpace(configured))
            {
                return configured.TrimEnd('/');
            }
            logger.LogInformation("BackendUrl not explicitly configured. Auto-detecting via local IP: {Ip}", _localIpAddress.Value);
            return $"http://{_localIpAddress.Value}:5004";
        });

        _emailConfirmationLinkTemplate = new Lazy<string>(() =>
        {
            var configured = configuration["EmailTemplates:EmailConfirmationLinkTemplate"];
            if (!string.IsNullOrWhiteSpace(configured))
            {
                return configured;
            }
            return $"{FrontendBaseUrl}/confirm-email?token=[TOKEN]&userId=[USERID]";
        });

        _passwordResetLinkTemplate = new Lazy<string>(() =>
        {
            var configured = configuration["EmailTemplates:PasswordResetLinkTemplate"];
            if (!string.IsNullOrWhiteSpace(configured))
            {
                return configured;
            }
            return $"{FrontendBaseUrl}/reset-password?token=[TOKEN]&userId=[USERID]";
        });
    }

    private static string DetectLocalIpAddress(ILogger logger)
    {
        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                             ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                             !ni.Description.Contains("Virtual", StringComparison.OrdinalIgnoreCase) &&
                             !ni.Description.Contains("Pseudo", StringComparison.OrdinalIgnoreCase) &&
                             !ni.Description.Contains("vEthernet", StringComparison.OrdinalIgnoreCase) &&
                             !ni.Description.Contains("Multiplexor", StringComparison.OrdinalIgnoreCase) &&
                             !ni.Description.Contains("Bluetooth", StringComparison.OrdinalIgnoreCase) &&
                             !ni.Description.Contains("ZeroTier", StringComparison.OrdinalIgnoreCase) &&
                             !ni.Description.Contains("Hyper-V", StringComparison.OrdinalIgnoreCase) &&
                             !ni.Description.Contains("VMware", StringComparison.OrdinalIgnoreCase) &&
                             !ni.Description.Contains("VirtualBox", StringComparison.OrdinalIgnoreCase) &&
                             !ni.Description.Contains("TAP", StringComparison.OrdinalIgnoreCase) &&
                             !ni.Name.Contains("vbox", StringComparison.OrdinalIgnoreCase))
                .ToList();

            logger.LogInformation("Scanning {Count} valid network interfaces for Local IP...", interfaces.Count);

            foreach (var ni in interfaces)
            {
                var ipProps = ni.GetIPProperties();
                var ipv4Addresses = ipProps.UnicastAddresses
                    .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
                    .Select(a => a.Address)
                    .ToList();

                foreach (var ip in ipv4Addresses)
                {
                    var ipString = ip.ToString();
                    
                    // Prefer true local network ranges (usually Wi-Fi or Ethernet on home routers)
                    if (ipString.StartsWith("192.168.") || 
                        ipString.StartsWith("10.") ||
                        IsClassBPrivate(ipString))
                    {
                        logger.LogInformation("✅ AUTO-DETECTED LOCAL IP: {Ip} (on {Interface}: {Desc})", ipString, ni.Name, ni.Description);
                        return ipString;
                    }
                }
            }

            // Fallback: Just get the first valid non-loopback IPv4 if no standard private range found
            foreach (var ni in interfaces)
            {
                var ipv4 = ni.GetIPProperties().UnicastAddresses
                    .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork)?.Address;
                
                if (ipv4 != null)
                {
                    logger.LogInformation("Fallback - detected IPv4 address: {Ip} on interface: {Interface}", ipv4, ni.Name);
                    return ipv4.ToString();
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to auto-detect local IP address.");
        }

        logger.LogWarning("No local IPv4 address detected. Defaulting to 127.0.0.1.");
        return "127.0.0.1";
    }

    private static bool IsClassBPrivate(string ip)
    {
        var parts = ip.Split('.');
        if (parts.Length != 4) return false;
        
        if (parts[0] == "172" && int.TryParse(parts[1], out int secondOctet))
        {
            return secondOctet >= 16 && secondOctet <= 31;
        }
        return false;
    }
}
