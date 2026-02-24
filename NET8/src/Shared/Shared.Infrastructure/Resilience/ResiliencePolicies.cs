using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using Polly.Retry;

namespace Shared.Infrastructure.Resilience;

/// <summary>
/// Políticas de resiliencia centralizadas usando Polly
/// </summary>
public static class ResiliencePolicies
{
    /// <summary>
    /// Política de reintentos para llamadas HTTP
    /// 3 reintentos con espera exponencial: 2s, 4s, 8s
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    var logger = context.GetLogger();
                    logger?.LogWarning(
                        "Reintento {RetryAttempt} después de {Timespan}s. Razón: {Reason}",
                        retryAttempt,
                        timespan.TotalSeconds,
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                });
    }

    /// <summary>
    /// Política de Circuit Breaker para llamadas HTTP
    /// Se abre después de 5 fallos consecutivos, permanece abierto 30 segundos
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (outcome, breakDelay) =>
                {
                    Console.WriteLine($"[CircuitBreaker] ABIERTO por {breakDelay.TotalSeconds}s. Razón: {outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()}");
                },
                onReset: () =>
                {
                    Console.WriteLine("[CircuitBreaker] CERRADO - Operaciones normales restauradas");
                },
                onHalfOpen: () =>
                {
                    Console.WriteLine("[CircuitBreaker] SEMI-ABIERTO - Probando conexión...");
                });
    }

    /// <summary>
    /// Política combinada: Retry envuelto en Circuit Breaker
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetCombinedPolicy()
    {
        return Policy.WrapAsync(GetCircuitBreakerPolicy(), GetRetryPolicy());
    }

    /// <summary>
    /// Política de reintentos genérica para operaciones async
    /// </summary>
    public static AsyncRetryPolicy GetGenericRetryPolicy(ILogger? logger = null)
    {
        return Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timeSpan, retryAttempt, context) =>
                {
                    logger?.LogWarning(exception,
                        "Reintento {RetryAttempt} después de {Timespan}s",
                        retryAttempt,
                        timeSpan.TotalSeconds);
                });
    }

    /// <summary>
    /// Política de Circuit Breaker genérica para operaciones async
    /// </summary>
    public static AsyncCircuitBreakerPolicy GetGenericCircuitBreakerPolicy(ILogger? logger = null)
    {
        return Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (exception, duration) =>
                {
                    logger?.LogError(exception,
                        "[CircuitBreaker] ABIERTO por {Duration}s",
                        duration.TotalSeconds);
                },
                onReset: () =>
                {
                    logger?.LogInformation("[CircuitBreaker] CERRADO - Operaciones normales");
                },
                onHalfOpen: () =>
                {
                    logger?.LogInformation("[CircuitBreaker] SEMI-ABIERTO - Probando...");
                });
    }

    private static ILogger? GetLogger(this Context context)
    {
        if (context.TryGetValue("Logger", out var logger))
        {
            return logger as ILogger;
        }
        return null;
    }
}

/// <summary>
/// Extensiones para configurar HttpClient con políticas de resiliencia
/// </summary>
public static class HttpClientBuilderExtensions
{
    /// <summary>
    /// Agrega políticas de resiliencia (Retry + Circuit Breaker) al HttpClient
    /// </summary>
    public static IHttpClientBuilder AddResiliencePolicies(this IHttpClientBuilder builder)
    {
        return builder
            .AddPolicyHandler(ResiliencePolicies.GetRetryPolicy())
            .AddPolicyHandler(ResiliencePolicies.GetCircuitBreakerPolicy());
    }
}
