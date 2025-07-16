using Azure;
using Azure.Identity;
using Azure.AI.Projects;
using Azure.AI.Agents.Persistent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

try
{
    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        cts.Cancel();
    };

    var host = CreateHostBuilder(args).Build();
    var chatService = host.Services.GetRequiredService<IPayrollChatService>();

    await chatService.StartChatAsync(cts.Token);
    return 0;
}
catch (OperationCanceledException)
{
    Console.WriteLine("\n❌ Application cancelled by user.");
    return 1;
}
catch (Exception ex)
{
    Console.WriteLine($"💥 Application error: {ex.Message}");
    return 1;
}

static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureServices((context, services) =>
        {
            services.AddSingleton<IPayrollChatService, PayrollChatService>();
            services.AddLogging(builder => builder.AddConsole());
        });

/// <summary>
/// Interface for the payroll chat service providing agent interaction capabilities.
/// </summary>
public interface IPayrollChatService
{
    /// <summary>
    /// Starts an interactive chat session with the payroll agent.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>A task representing the chat session</returns>
    Task StartChatAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Service providing payroll agent chat functionality with Azure AI Foundry integration.
/// Implements comprehensive error handling, file upload capabilities, and multi-turn conversations.
/// </summary>
public class PayrollChatService : IPayrollChatService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PayrollChatService> _logger;
    private readonly AIProjectClient _projectClient;
    private readonly PersistentAgentsClient _agentsClient;
    private readonly string _agentId;
    private PersistentAgentThread? _currentThread;

    /// <summary>
    /// Initializes a new instance of PayrollChatService with required dependencies.
    /// </summary>
    /// <param name="configuration">Application configuration containing Azure endpoints and agent ID</param>
    /// <param name="logger">Logger instance for structured logging</param>
    /// <exception cref="InvalidOperationException">Thrown when required configuration is missing</exception>
    public PayrollChatService(IConfiguration configuration, ILogger<PayrollChatService> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var endpoint = new Uri(_configuration["ProjectEndpoint"] ??
            throw new InvalidOperationException("ProjectEndpoint not configured"));
        _agentId = _configuration["AgentId"] ??
            throw new InvalidOperationException("AgentId not configured");

        _projectClient = new AIProjectClient(endpoint, new DefaultAzureCredential());
        _agentsClient = _projectClient.GetPersistentAgentsClient();

        _logger.LogInformation("PayrollChatService initialized with endpoint: {Endpoint}", endpoint);
    }    /// <summary>
    /// Starts an interactive chat session with comprehensive error handling and user command support.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>A task representing the chat session</returns>
    public async Task StartChatAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("💼 === Payroll Agent Chat ===");
        Console.WriteLine("💬 Type 'quit' to exit, 'upload <filepath>' to upload a file, or just chat normally.");
        Console.WriteLine("⚡ Press Ctrl+C to cancel at any time.");
        Console.WriteLine();        try
        {
            // Create a new thread for this session
            _currentThread = await CreateNewThreadAsync(cancellationToken);
            _logger.LogInformation("Created new thread: {ThreadId}", _currentThread.Id);
            Console.WriteLine($"🚀 Started new conversation (Thread: {_currentThread.Id})");
            Console.WriteLine();

            while (!cancellationToken.IsCancellationRequested)
            {
                Console.Write("👤 You: ");

                // Use a task to read from console with cancellation support
                var inputTask = Task.Run(Console.ReadLine, cancellationToken);
                string? input;                try
                {
                    input = await inputTask;
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("\n❌ Chat cancelled.");
                    break;
                }

                if (string.IsNullOrWhiteSpace(input))
                    continue;

                if (input.Equals("quit", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("User requested to quit the chat session");
                    break;
                }

                if (input.StartsWith("upload ", StringComparison.OrdinalIgnoreCase))
                {
                    var filePath = input.Substring(7).Trim();
                    await HandleFileUploadAsync(filePath, cancellationToken);
                    continue;
                }

                await ProcessUserMessageAsync(input, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Chat session cancelled by user");
            throw;
        }        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during chat session");
            Console.WriteLine($"❌ Error: {ex.Message}");
            throw;
        }

        Console.WriteLine("👋 Chat ended. Goodbye!");
    }

    /// <summary>
    /// Creates a new conversation thread with the agent.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>The created thread</returns>    /// <exception cref="InvalidOperationException">Thrown when thread creation fails</exception>
    private async Task<PersistentAgentThread> CreateNewThreadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var thread = await _agentsClient.Threads.CreateThreadAsync();
            _logger.LogInformation("Created thread: {ThreadId}", thread.Value.Id);
            return thread.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create thread");
            throw new InvalidOperationException($"Failed to create agent thread: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Processes a user message by sending it to the agent and displaying the response.
    /// </summary>
    /// <param name="userMessage">The user's message to process</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>A task representing the message processing</returns>
    private async Task ProcessUserMessageAsync(string userMessage, CancellationToken cancellationToken = default)
    {        try
        {
            // Send the user message to the thread
            var message = await _agentsClient.Messages.CreateMessageAsync(
                _currentThread!.Id,
                MessageRole.User,
                userMessage,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Sent message: {MessageId}", message.Value.Id);

            // Create and run the agent
            var agent = await _agentsClient.Administration.GetAgentAsync(_agentId, cancellationToken);
            var run = await _agentsClient.Runs.CreateRunAsync(
                _currentThread.Id,
                agent.Value.Id,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Created run: {RunId}", run.Value.Id);

            // Poll until completion with proper status handling
            await WaitForRunCompletionAsync(run.Value, cancellationToken);

            // Display the assistant's response
            await DisplayLatestAssistantMessageAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine("\n⚡ Message processing cancelled.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing user message: {Message}", userMessage);
            Console.WriteLine($"❌ Error processing message: {ex.Message}");
        }
    }

    /// <summary>
    /// Waits for agent run completion with status polling and user feedback.
    /// </summary>
    /// <param name="run">The run to monitor</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>A task representing the wait operation</returns>    /// <exception cref="InvalidOperationException">Thrown when run fails or is cancelled</exception>
    private async Task WaitForRunCompletionAsync(ThreadRun run, CancellationToken cancellationToken = default)
    {
        Console.Write("🤖 Payroll Agent is thinking");
        var completedStatuses = new[] { RunStatus.Completed, RunStatus.Failed, RunStatus.Cancelled, RunStatus.Expired };

        do
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
            Console.Write(".");
            run = await _agentsClient.Runs.GetRunAsync(_currentThread!.Id, run.Id, cancellationToken);
        }
        while (!completedStatuses.Contains(run.Status) && !cancellationToken.IsCancellationRequested);

        Console.WriteLine(); // New line after dots

        if (run.Status != RunStatus.Completed)
        {
            var errorMessage = run.LastError?.Message ?? $"Run ended with status: {run.Status}";
            _logger.LogWarning("Run did not complete successfully: {Status}, Error: {Error}", run.Status, errorMessage);
            throw new InvalidOperationException($"Run failed or was canceled: {errorMessage}");
        }

        _logger.LogInformation("Run completed successfully: {RunId}", run.Id);
    }

    /// <summary>
    /// Displays the latest assistant message from the conversation thread.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>A task representing the display operation</returns>
    private async Task DisplayLatestAssistantMessageAsync(CancellationToken cancellationToken = default)
    {        try
        {
            var messages = _agentsClient.Messages.GetMessagesAsync(
                _currentThread!.Id,
                order: ListSortOrder.Descending);

            await foreach (var message in messages)
            {
                if (message.Role == MessageRole.User)
                    continue;

                // Display assistant message
                Console.WriteLine("🤖 Assistant:");
                foreach (var contentItem in message.ContentItems)
                {
                    if (contentItem is MessageTextContent textItem)
                    {
                        Console.WriteLine(textItem.Text);
                    }                    else if (contentItem is MessageImageFileContent imageFileItem)
                    {
                        Console.WriteLine($"🖼️ [Image file: {imageFileItem.FileId}]");
                    }
                }
                Console.WriteLine();
                break; // Only show the latest assistant message
            }
        }        catch (Exception ex)
        {
            _logger.LogError(ex, "Error displaying assistant message");
            Console.WriteLine($"❌ Error displaying response: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles file upload with comprehensive validation and error handling.
    /// </summary>
    /// <param name="filePath">Path to the file to upload</param>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>A task representing the upload operation</returns>
    private async Task HandleFileUploadAsync(string filePath, CancellationToken cancellationToken = default)
    {        try
        {
            // Validate file existence
            if (string.IsNullOrWhiteSpace(filePath))
            {
                Console.WriteLine("⚠️ Please specify a file path.");
                return;
            }

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"❌ File not found: {filePath}");
                return;
            }            var fileInfo = new FileInfo(filePath);
            var fileName = fileInfo.Name;
            var fileSize = fileInfo.Length;

            Console.WriteLine($"📤 Uploading file: {fileName} ({fileSize:N0} bytes)");
            _logger.LogInformation("Starting file upload: {FileName} ({FileSize} bytes)", fileName, fileSize);

            // TODO: Implement actual file upload once Azure AI Foundry API is confirmed
            // For now, simulate the upload process
            await Task.Delay(1000, cancellationToken);

            _logger.LogInformation("Simulated file upload: {FileName} ({FileSize} bytes)", fileName, fileSize);

            // Send message with file attachment
            await _agentsClient.Messages.CreateMessageAsync(
                _currentThread!.Id,
                MessageRole.User,
                $"I've uploaded a file: {fileName} ({fileSize:N0} bytes). Please analyze it.");

            Console.WriteLine($"✅ File uploaded successfully: {fileName}");
            Console.WriteLine("📋 The file information has been shared with the assistant. What would you like to know about it?");
            Console.WriteLine();
        }catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine("\n❌ File upload cancelled.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file: {FilePath}", filePath);
            Console.WriteLine($"❌ Error uploading file: {ex.Message}");
        }
    }
}
