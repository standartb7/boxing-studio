namespace Trainer.App.Services.Api;

public static class ApiSettings
{
    /// <summary>
    /// Базовый URL API. Меняй здесь под текущий сценарий:
    /// - Mac Catalyst / iOS simulator: http://localhost:5080
    /// - Android emulator: http://10.0.2.2:5080 (специальный alias для host-машины)
    /// - Физическое устройство в той же LAN: http://&lt;твой-IP&gt;:5080
    /// - После деплоя на Fly.io: https://&lt;app&gt;.fly.dev
    /// </summary>
    public const string BaseUrl =
#if ANDROID
        "http://10.0.2.2:5080";
#else
        "http://localhost:5080";
#endif
}
