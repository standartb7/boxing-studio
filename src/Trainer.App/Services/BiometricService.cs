#if IOS || MACCATALYST
using LocalAuthentication;
#endif

namespace Trainer.App.Services;

/// <summary>
/// Биометрия (Face ID / Touch ID) через нативные API. Чтобы не тянуть NuGet-плагин:
/// поверхность API маленькая, iOS LocalAuthentication покрывает 100% наших нужд.
/// Android реализация — TODO (BiometricPrompt), пока возвращает Unavailable.
/// </summary>
public class BiometricService
{
    public enum BiometryKind { None, FaceId, TouchId, Generic }

    public BiometryKind GetKind()
    {
#if IOS || MACCATALYST
        using var ctx = new LAContext();
        if (!ctx.CanEvaluatePolicy(LAPolicy.DeviceOwnerAuthenticationWithBiometrics, out _))
            return BiometryKind.None;

        return ctx.BiometryType switch
        {
            LABiometryType.FaceId => BiometryKind.FaceId,
            LABiometryType.TouchId => BiometryKind.TouchId,
            _ => BiometryKind.Generic,
        };
#else
        return BiometryKind.None;
#endif
    }

    public bool IsAvailable() => GetKind() != BiometryKind.None;

    public string DisplayName() => GetKind() switch
    {
        BiometryKind.FaceId => "Face ID",
        BiometryKind.TouchId => "Touch ID",
        BiometryKind.Generic => "Биометрия",
        _ => "Биометрия",
    };

    public async Task<bool> AuthenticateAsync(string reason)
    {
#if IOS || MACCATALYST
        using var ctx = new LAContext();
        if (!ctx.CanEvaluatePolicy(LAPolicy.DeviceOwnerAuthenticationWithBiometrics, out _))
            return false;

        try
        {
            var (success, _) = await ctx.EvaluatePolicyAsync(
                LAPolicy.DeviceOwnerAuthenticationWithBiometrics, reason);
            return success;
        }
        catch
        {
            // Юзер отменил, fallback на iPhone сам показал, и т.п. — для нас это просто «не вышло».
            return false;
        }
#else
        await Task.CompletedTask;
        return false;
#endif
    }
}
