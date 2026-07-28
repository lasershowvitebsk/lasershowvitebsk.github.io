using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;

namespace LaserShowSite.Services;

public sealed class ChatService : IDisposable
{
    private const string WorkerBaseUrl =
        "https://laser-api.vitalitalipski.workers.dev";

    private readonly HttpClient httpClient;

    private readonly SemaphoreSlim verificationLock = new(1, 1);
    public string? SessionToken { get; private set; }
    public bool HasValidSessionToken =>
        !string.IsNullOrWhiteSpace(SessionToken);

    public ChatService(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public void ClearSessionToken()
    {
        SessionToken = null;
    }

    public async Task<bool> VerifyTurnstileAsync(string turnstileToken)
    {
        if (string.IsNullOrWhiteSpace(turnstileToken))
        {
            Console.Error.WriteLine(
                "[Turnstile] Empty token received in ChatService."
            );

            ClearSessionToken();

            return false;
        }

        await verificationLock.WaitAsync();

        try
        {
 
            ClearSessionToken();

            var payload = new
            {
                turnstileToken
            };

            using var response = await httpClient.PostAsJsonAsync(
                $"{WorkerBaseUrl}/verify",
                payload
            );

            var json = await response.Content.ReadAsStringAsync();

            Console.WriteLine(
                $"[Turnstile] /verify HTTP {(int)response.StatusCode}"
            );

            if (!response.IsSuccessStatusCode)
            {
                Console.Error.WriteLine(
                    $"[Turnstile] /verify rejected. " +
                    $"HTTP {(int)response.StatusCode}. " +
                    $"Body: {json}"
                );

                return false;
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                Console.Error.WriteLine(
                    "[Turnstile] /verify returned empty response."
                );

                return false;
            }

            using var document = JsonDocument.Parse(json);

            var root = document.RootElement;

            var success =
                root.TryGetProperty(
                    "success",
                    out var successProperty
                ) &&
                successProperty.ValueKind == JsonValueKind.True;

            if (!success)
            {
                Console.Error.WriteLine(
                    "[Turnstile] Worker returned success=false."
                );

                return false;
            }

            if (
                !root.TryGetProperty(
                    "sessionToken",
                    out var tokenProperty
                ) ||
                tokenProperty.ValueKind != JsonValueKind.String
            )
            {
                Console.Error.WriteLine(
                    "[Turnstile] Worker returned success=true without sessionToken."
                );

                return false;
            }

            SessionToken = tokenProperty.GetString();

            var hasToken = !string.IsNullOrWhiteSpace(SessionToken);

            Console.WriteLine(
                $"[Turnstile] Session token received: {hasToken}"
            );

            return hasToken;
        }
        catch (Exception ex)
        {
            ClearSessionToken();

            Console.Error.WriteLine(
                $"[Turnstile] VerifyTurnstileAsync exception: {ex.Message}"
            );

            return false;
        }
        finally
        {
            verificationLock.Release();
        }
    }

    public async Task<ChatReplyResult> GetBotReplyAsync(
        string userMessage,
        string language)
    {
        if (!HasValidSessionToken)
        {
            return ChatReplyResult.VerificationRequired(
                GetVerificationRequiredText(language)
            );
        }

        var payload = new
        {
            message = userMessage,
            language,
            sessionToken = SessionToken
        };

        return await SendChatRequestAsync(payload, language);
    }

    public async Task<ChatReplyResult> GetChipReplyAsync(
        string chipId,
        string language)
    {
        if (!HasValidSessionToken)
        {
            return ChatReplyResult.VerificationRequired(
                GetVerificationRequiredText(language)
            );
        }

        var payload = new
        {
            message = chipId,
            chipId,
            language,
            sessionToken = SessionToken
        };

        return await SendChatRequestAsync(payload, language);
    }

    private async Task<ChatReplyResult> SendChatRequestAsync(
        object payload,
        string language)
    {
        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                $"{WorkerBaseUrl}/chat",
                payload
            );

            var json = await response.Content.ReadAsStringAsync();

            Console.WriteLine(
                $"[Chat] /chat HTTP {(int)response.StatusCode}"
            );

            if (
                response.StatusCode == HttpStatusCode.Unauthorized ||
                response.StatusCode == HttpStatusCode.Forbidden
            )
            {
                ClearSessionToken();

                return ChatReplyResult.VerificationRequired(
                    GetVerificationRequiredText(language)
                );
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                return ChatReplyResult.Failure(
                    GetFallbackText(language)
                );
            }

            using var document = JsonDocument.Parse(json);

            var root = document.RootElement;

            var requiresVerification =
                root.TryGetProperty(
                    "requiresVerification",
                    out var requiresVerificationProperty
                ) &&
                requiresVerificationProperty.ValueKind ==
                JsonValueKind.True;

            if (requiresVerification)
            {
                ClearSessionToken();

                var error = GetStringProperty(root, "error");

                return ChatReplyResult.VerificationRequired(
                    string.IsNullOrWhiteSpace(error)
                        ? GetVerificationRequiredText(language)
                        : error
                );
            }

            var success =
                !root.TryGetProperty(
                    "success",
                    out var successProperty
                ) ||
                successProperty.ValueKind != JsonValueKind.False;

            if (!success)
            {
                var error = GetStringProperty(root, "error");

                return ChatReplyResult.Failure(
                    string.IsNullOrWhiteSpace(error)
                        ? GetFallbackText(language)
                        : error
                );
            }

            var reply = GetStringProperty(root, "reply");

            if (string.IsNullOrWhiteSpace(reply))
            {
                return ChatReplyResult.Failure(
                    GetFallbackText(language)
                );
            }

            return ChatReplyResult.Success(reply);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"[Chat] SendChatRequestAsync exception: {ex.Message}"
            );

            return ChatReplyResult.Failure(
                GetFallbackText(language)
            );
        }
    }

    private static string? GetStringProperty(
        JsonElement root,
        string propertyName)
    {
        return root.TryGetProperty(
            propertyName,
            out var property
        ) &&
        property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static string GetVerificationRequiredText(string language)
    {
        return language == "ru"
            ? "Требуется проверка безопасности."
            : "Security verification is required.";
    }

    private static string GetFallbackText(string language)
    {
        return language == "ru"
            ? "Не удалось получить ответ. Попробуйте позже."
            : "Could not get a response. Please try again later.";
    }

    public void Dispose()
    {
        verificationLock.Dispose();
    }
}

public sealed class ChatReplyResult
{
    public string Reply { get; init; } = "";

    public bool RequiresVerification { get; init; }

    public static ChatReplyResult Success(string reply)
    {
        return new ChatReplyResult
        {
            Reply = reply,
            RequiresVerification = false
        };
    }

    public static ChatReplyResult VerificationRequired(string message)
    {
        return new ChatReplyResult
        {
            Reply = message,
            RequiresVerification = true
        };
    }

    public static ChatReplyResult Failure(string message)
    {
        return new ChatReplyResult
        {
            Reply = message,
            RequiresVerification = false
        };
    }
}