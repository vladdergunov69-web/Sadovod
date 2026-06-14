using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Sadovod.Data;
using Sadovod.Models.Entities;
using WebPush;

namespace Sadovod.Services;

public interface IPushNotificationService
{
    string PublicKey { get; }
    Task SaveSubscriptionAsync(int? userId, string endpoint, string p256dh, string auth);
    Task<int> SendToAllAsync(string title, string body, string? url);
    Task<int> SendToUserAsync(int? userId, string title, string body, string? url);
    Task<int> CountAsync();
}

public class PushNotificationService : IPushNotificationService
{
    private readonly SadovodDbContext _db;
    private readonly IConfiguration _cfg;
    private readonly ILogger<PushNotificationService> _log;

    public PushNotificationService(SadovodDbContext db, IConfiguration cfg, ILogger<PushNotificationService> log)
    {
        _db = db;
        _cfg = cfg;
        _log = log;
    }

    public string PublicKey => _cfg["PushSettings:PublicKey"] ?? string.Empty;

    public async Task SaveSubscriptionAsync(int? userId, string endpoint, string p256dh, string auth)
    {
        var existing = await _db.PushSubscriptions.FirstOrDefaultAsync(s => s.Endpoint == endpoint);
        if (existing is not null)
        {
            existing.P256dh = p256dh;
            existing.Auth = auth;
            existing.UserId = userId;
        }
        else
        {
            _db.PushSubscriptions.Add(new Models.Entities.PushSubscription
            {
                UserId = userId,
                Endpoint = endpoint,
                P256dh = p256dh,
                Auth = auth
            });
        }
        await _db.SaveChangesAsync();
    }

    public async Task<int> CountAsync() => await _db.PushSubscriptions.CountAsync();

    public async Task<int> SendToAllAsync(string title, string body, string? url)
    {
        var subs = await _db.PushSubscriptions.ToListAsync();
        return await SendToSubscriptionsAsync(subs, title, body, url);
    }

    public async Task<int> SendToUserAsync(int? userId, string title, string body, string? url)
    {
        if (userId is null) return 0;
        var subs = await _db.PushSubscriptions.Where(s => s.UserId == userId).ToListAsync();
        return await SendToSubscriptionsAsync(subs, title, body, url);
    }

    private async Task<int> SendToSubscriptionsAsync(
        List<Models.Entities.PushSubscription> subs, string title, string body, string? url)
    {
        var subject = _cfg["PushSettings:Subject"] ?? "mailto:admin@example.com";
        var pub = _cfg["PushSettings:PublicKey"];
        var priv = _cfg["PushSettings:PrivateKey"];
        if (string.IsNullOrWhiteSpace(pub) || string.IsNullOrWhiteSpace(priv))
        {
            _log.LogError("VAPID-ключи не настроены.");
            return 0;
        }
        if (subs.Count == 0) return 0;

        var client = new WebPushClient();
        var vapid = new VapidDetails(subject, pub, priv);

        var payload = JsonSerializer.Serialize(new { title, body, url });
        var sent = 0;
        var dead = new List<Models.Entities.PushSubscription>();

        foreach (var s in subs)
        {
            var wp = new WebPush.PushSubscription(s.Endpoint, s.P256dh, s.Auth);
            try
            {
                await client.SendNotificationAsync(wp, payload, vapid);
                sent++;
            }
            catch (WebPushException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Gone
                                            || ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                dead.Add(s);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Не удалось отправить push на {Endpoint}", s.Endpoint);
            }
        }

        if (dead.Count > 0)
        {
            _db.PushSubscriptions.RemoveRange(dead);
            await _db.SaveChangesAsync();
        }
        return sent;
    }
}
