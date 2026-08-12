using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;

namespace IslamiJindegiApi.Services;

/// Sends a silent, data-only FCM push to every app instance subscribed to the
/// "content-sync" topic whenever admin-curated offline content changes, so
/// the app can sync the affected domain immediately instead of waiting for
/// its periodic poll. No per-device token storage — broadcast to one topic,
/// the app filters by the "feature" data field.
public class ContentSyncNotifier
{
    const string Topic = "content-sync";

    readonly FirebaseMessaging? _messaging;

    public ContentSyncNotifier(IConfiguration config)
    {
        var serviceAccountJson = Environment.GetEnvironmentVariable("FIREBASE_SERVICE_ACCOUNT_JSON")
            ?? config["Firebase:ServiceAccountJson"];

        if (string.IsNullOrWhiteSpace(serviceAccountJson))
        {
            // Unlike StorageService, push is additive on top of the poll fallback —
            // missing config degrades to poll-only sync instead of failing startup.
            _messaging = null;
            return;
        }

        var app = FirebaseApp.Create(new AppOptions
        {
            Credential = GoogleCredential.FromJson(serviceAccountJson),
        });
        _messaging = FirebaseMessaging.GetMessaging(app);
    }

    public async Task NotifyAsync(string feature)
    {
        if (_messaging is null) return;

        await _messaging.SendAsync(new Message
        {
            Topic = Topic,
            Data = new Dictionary<string, string> { ["feature"] = feature },
            Android = new AndroidConfig { Priority = Priority.High },
            Apns = new ApnsConfig
            {
                Aps = new Aps { ContentAvailable = true },
                Headers = new Dictionary<string, string> { ["apns-priority"] = "5" },
            },
        });
    }
}
