using System.Net;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.ObjectPool;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Requests;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types;
using TelegramBots.MessageSender.TelegramBotClientExtensions;

namespace TelegramBots.MessageSender.Services.BotClients;

public abstract class BaseTelegramBotClient<TOptions> : ITelegramBotClient, IResettable where TOptions : TelegramBotClientOptions
{
    private readonly TOptions _options;

    private HttpClient _httpClient;

    /// <inheritdoc/>
    public long? BotId => _options.BotId;

    /// <inheritdoc />
    public bool LocalBotServer => _options.LocalBotServer;

    /// <summary>
    /// Timeout for requests
    /// </summary>
    public TimeSpan Timeout
    {
        get => _httpClient.Timeout;
        set => _httpClient.Timeout = value;
    }

    /// <inheritdoc />
    public IExceptionParser ExceptionsParser { get; set; } = new DefaultExceptionParser();

    public HttpClient Client
    {
        get => _httpClient;
        set => _httpClient = value;
    }

    /// <summary>
    /// Occurs before sending a request to API
    /// </summary>
    public event AsyncEventHandler<ApiRequestEventArgs>? OnMakingApiRequest;

    /// <summary>
    /// Occurs after receiving the response to an API request
    /// </summary>
    public event AsyncEventHandler<ApiResponseEventArgs>? OnApiResponseReceived;

    /// <summary>
    /// Create a new <see cref="TelegramBotClient"/> instance.
    /// </summary>
    /// <param name="options">Configuration for <see cref="TelegramBotClient" /></param>
    /// <param name="httpClient">A custom <see cref="HttpClient"/></param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="options"/> is <c>null</c>
    /// </exception>
    protected BaseTelegramBotClient(
        TOptions options,
        HttpClient? httpClient = default)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _httpClient = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public virtual async Task<TResponse> MakeRequestAsync<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var url = $"{_options.BaseRequestUrl}/{request.MethodName}";

#pragma warning disable CA2000
        var httpRequest = new HttpRequestMessage(method: request.Method, requestUri: url)
        {
            Content = request.ToHttpContent()
        };
#pragma warning restore CA2000

        if (OnMakingApiRequest is not null)
        {
            var requestEventArgs = new ApiRequestEventArgs(
                request: request,
                httpRequestMessage: httpRequest
            );
            await OnMakingApiRequest.Invoke(
                botClient: this,
                args: requestEventArgs,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);
        }

        using var httpResponse = await SendRequestAsync(
            httpClient: _httpClient,
            httpRequest: httpRequest,
            cancellationToken: cancellationToken
        ).ConfigureAwait(false);

        if (OnApiResponseReceived is not null)
        {
            var requestEventArgs = new ApiRequestEventArgs(
                request: request,
                httpRequestMessage: httpRequest
            );
            var responseEventArgs = new ApiResponseEventArgs(
                responseMessage: httpResponse,
                apiRequestEventArgs: requestEventArgs
            );
            await OnApiResponseReceived.Invoke(
                botClient: this,
                args: responseEventArgs,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);
        }

        if (httpResponse.StatusCode != HttpStatusCode.OK)
        {
            var failedApiResponse = await httpResponse
                .DeserializeContentAsync<ApiResponse>(
                    guard: response =>
                        response.ErrorCode == default ||
                        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                        response.Description is null
                )
                .ConfigureAwait(false);

            throw ExceptionsParser.Parse(failedApiResponse);
        }

        var apiResponse = await httpResponse
            .DeserializeContentAsync<ApiResponse<TResponse>>(
                guard: response => response.Ok == false ||
                                   response.Result is null
            )
            .ConfigureAwait(false);

        return apiResponse.Result!;

        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        static async Task<HttpResponseMessage> SendRequestAsync(
            HttpClient httpClient,
            HttpRequestMessage httpRequest,
            CancellationToken cancellationToken)
        {
            HttpResponseMessage? httpResponse;
            try
            {
                httpResponse = await httpClient
                    .SendAsync(request: httpRequest, cancellationToken: cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false);
            }
            catch (TaskCanceledException exception)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }

                throw new RequestException(message: "Request timed out", innerException: exception);
            }
            catch (Exception exception)
            {
                throw new RequestException(
                    message: "Exception during making request",
                    innerException: exception
                );
            }

            return httpResponse;
        }
    }

    /// <summary>
    /// Test the API token
    /// </summary>
    /// <returns><see langword="true"/> if token is valid</returns>
    public async Task<bool> TestApiAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await MakeRequestAsync(request: new GetMeRequest(), cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return true;
        }
        catch (ApiRequestException e)
            when (e.ErrorCode == 401)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task DownloadFileAsync(
        string filePath,
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath) || filePath.Length < 2)
        {
            throw new ArgumentException(message: "Invalid file path", paramName: nameof(filePath));
        }

        if (destination is null)
        {
            throw new ArgumentNullException(nameof(destination));
        }

        var fileUri = $"{_options.BaseFileUrl}/{filePath}";
        using var httpResponse = await GetResponseAsync(
            httpClient: _httpClient,
            fileUri: fileUri,
            cancellationToken: cancellationToken
        ).ConfigureAwait(false);

        if (!httpResponse.IsSuccessStatusCode)
        {
            var failedApiResponse = await httpResponse
                .DeserializeContentAsync<ApiResponse>(
                    guard: response =>
                        response.ErrorCode == default ||
                        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                        response.Description is null
                )
                .ConfigureAwait(false);

            throw ExceptionsParser.Parse(failedApiResponse);
        }

        if (httpResponse.Content is null)
        {
            throw new RequestException(
                message: "Response doesn't contain any content",
                httpResponse.StatusCode
            );
        }

        try
        {
            await httpResponse.Content.CopyToAsync(destination, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            throw new RequestException(
                message: "Exception during file download",
                httpResponse.StatusCode,
                exception
            );
        }

        [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
        static async Task<HttpResponseMessage> GetResponseAsync(
            HttpClient httpClient,
            string fileUri,
            CancellationToken cancellationToken)
        {
            HttpResponseMessage? httpResponse;
            try
            {
                httpResponse = await httpClient
                    .GetAsync(
                        requestUri: fileUri,
                        completionOption: HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken: cancellationToken
                    )
                    .ConfigureAwait(continueOnCapturedContext: false);
            }
            catch (TaskCanceledException exception)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }

                throw new RequestException(
                    message: "Request timed out",
                    innerException: exception
                );
            }
            catch (Exception exception)
            {
                throw new RequestException(
                    message: "Exception during file download",
                    innerException: exception
                );
            }

            return httpResponse;
        }
    }

    public bool TryReset()
    {
        _httpClient = null;
        return true;
    }
}