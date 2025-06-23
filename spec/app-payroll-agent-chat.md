---
title: Payroll Agent Chat Application Specification
version: 1.0
date_created: 2025-06-24
last_updated: 2025-06-24
owner: Development Team
tags: [app, ai, payroll, chat, console, azure]
---

# Payroll Agent Chat Application Specification

A .NET 8 console application that provides a multi-turn chat interface with an Azure AI Foundry Agent for payroll-related tasks and document analysis.

## 1. Purpose & Scope

This specification defines the requirements, constraints, and interfaces for a console-based chat application that integrates with Azure AI Foundry agents to provide payroll expertise and document analysis capabilities. The application is designed for HR professionals, payroll administrators, and finance teams who need AI-assisted guidance on payroll processes, regulations, and data analysis.

**Intended Audience**: Software engineers, DevOps engineers, and solution architects implementing AI-powered payroll assistance tools.

**Assumptions**:
- Azure AI Foundry project is configured with appropriate payroll agent
- Users have valid Azure credentials configured
- Target environment supports .NET 8 runtime

## 2. Definitions

- **AI Foundry**: Microsoft's Azure AI platform for building and deploying AI agents
- **PersistentAgent**: An Azure AI agent that maintains conversation context across sessions
- **Thread**: A conversation session between user and agent, maintaining message history
- **Run**: An execution instance of an agent processing user input within a thread
- **CLI**: Command Line Interface
- **DI**: Dependency Injection
- **CTS**: CancellationTokenSource for graceful shutdown handling
- **DefaultAzureCredential**: Azure SDK authentication mechanism supporting multiple credential sources

## 3. Requirements, Constraints & Guidelines

### Functional Requirements
- **REQ-001**: Application must provide interactive console-based chat interface
- **REQ-002**: Application must support multi-turn conversations with context retention
- **REQ-003**: Application must support file upload for document analysis (PDF, Excel, etc.)
- **REQ-004**: Application must handle graceful shutdown on Ctrl+C
- **REQ-005**: Application must create new conversation thread per session
- **REQ-006**: Application must display real-time processing indicators during agent thinking
- **REQ-007**: Application must support 'quit' command to exit gracefully
- **REQ-008**: Application must support 'upload <filepath>' command for file operations

### Security Requirements
- **SEC-001**: Application must use Azure DefaultAzureCredential for authentication
- **SEC-002**: Application must not expose sensitive configuration in logs
- **SEC-003**: Application must validate file paths and existence before upload
- **SEC-004**: Application must handle authentication failures gracefully

### Technical Requirements
- **TEC-001**: Application must be built on .NET 8 framework
- **TEC-002**: Application must use Generic Host pattern for dependency injection
- **TEC-003**: Application must implement structured logging with Microsoft.Extensions.Logging
- **TEC-004**: Application must use async/await patterns for all I/O operations
- **TEC-005**: Application must support cancellation tokens throughout
- **TEC-006**: Application must return appropriate exit codes (0 success, 1 error)

### Performance Requirements
- **PER-001**: Application must respond to user input within 500ms (excluding agent processing)
- **PER-002**: Application must poll agent status every 500ms during processing
- **PER-003**: Application must handle concurrent file upload and message processing

### Constraints
- **CON-001**: Application must be single-threaded console application
- **CON-002**: Application must use only stable Azure SDK packages in production
- **CON-003**: File uploads are limited by Azure AI Foundry agent capabilities
- **CON-004**: Application requires active internet connection for Azure services

### Guidelines
- **GUD-001**: Follow SOLID principles in service design
- **GUD-002**: Use descriptive method and variable names for self-documenting code
- **GUD-003**: Implement comprehensive error handling with user-friendly messages
- **GUD-004**: Log all significant operations for troubleshooting
- **GUD-005**: Use configuration-based settings for environment-specific values

### Patterns to Follow
- **PAT-001**: Use Generic Host pattern for application lifecycle management
- **PAT-002**: Implement service layer abstraction for agent interactions
- **PAT-003**: Use builder pattern for host configuration
- **PAT-004**: Apply async/await consistently for I/O bound operations

## 4. Interfaces & Data Contracts

### Configuration Interface
```json
{
  "ProjectEndpoint": "https://your-project-endpoint.services.ai.azure.com/api/projects/your-project",
  "AgentId": "your-agent-id"
}
```

### Service Interface
```csharp
public interface IPayrollChatService
{
    Task StartChatAsync(CancellationToken cancellationToken = default);
}
```

### Core Dependencies
```csharp
// Required NuGet packages
- Azure.AI.Projects (1.0.0-beta.9)
- Azure.AI.Agents.Persistent (1.0.0)
- Azure.Identity (1.13.1)
- Microsoft.Extensions.Hosting (8.0.1)
- Microsoft.Extensions.Configuration (8.0.1)
- Microsoft.Extensions.Logging (8.0.1)
```

### Azure AI Foundry Agent Integration Sample
```csharp
using Azure;
using Azure.Identity;
using Azure.AI.Projects;
using Azure.AI.Agents.Persistent;

async Task RunAgentConversation()
{
    var endpoint = new Uri("https://aif-datapay.services.ai.azure.com/api/projects/datapay-project-1");
    AIProjectClient projectClient = new(endpoint, new DefaultAzureCredential());

    PersistentAgentsClient agentsClient = projectClient.GetPersistentAgentsClient();

    PersistentAgent agent = agentsClient.Administration.GetAgent("asst_XbMMtlHwVVYlGtK9CQPVIkeV");

    PersistentAgentThread thread = agentsClient.Threads.GetThread("thread_f4V8mjnfTcSDElMMHNB6nBNf");

    PersistentThreadMessage messageResponse = agentsClient.Messages.CreateMessage(
        thread.Id,
        MessageRole.User,
        "Hi payroll-agent");

    ThreadRun run = agentsClient.Runs.CreateRun(
        thread.Id,
        agent.Id);

    // Poll until the run reaches a terminal status
    do
    {
        await Task.Delay(TimeSpan.FromMilliseconds(500));
        run = agentsClient.Runs.GetRun(thread.Id, run.Id);
    }
    while (run.Status == RunStatus.Queued
        || run.Status == RunStatus.InProgress);
    if (run.Status != RunStatus.Completed)
    {
        throw new InvalidOperationException($"Run failed or was canceled: {run.LastError?.Message}");
    }

    Pageable<PersistentThreadMessage> messages = agentsClient.Messages.GetMessages(
        thread.Id, order: ListSortOrder.Ascending);

    // Display messages
    foreach (PersistentThreadMessage threadMessage in messages)
    {
        Console.Write($"{threadMessage.CreatedAt:yyyy-MM-dd HH:mm:ss} - {threadMessage.Role,10}: ");
        foreach (MessageContent contentItem in threadMessage.ContentItems)
        {
            if (contentItem is MessageTextContent textItem)
            {
                Console.Write(textItem.Text);
            }
            else if (contentItem is MessageImageFileContent imageFileItem)
            {
                Console.Write($"<image from ID: {imageFileItem.FileId}");
            }
            Console.WriteLine();
        }
    }
}

// Main execution
await RunAgentConversation();
```

### Authentication Requirements
The application requires Azure authentication with the following RBAC permissions:
- **Azure AI User** role assigned at the project scope
- Minimum required permissions: `agents/*/read`, `agents/*/action`, `agents/*/delete`

### File Upload Integration Pattern
```csharp
// File upload workflow for document analysis
public async Task<string> UploadFileAsync(string filePath, CancellationToken cancellationToken = default)
{
    // Validate file exists
    if (!File.Exists(filePath))
        throw new FileNotFoundException($"File not found: {filePath}");

    // Upload file to Azure AI Foundry
    using var fileStream = File.OpenRead(filePath);
    var uploadedFile = await _agentsClient.Files.UploadAsync(
        fileStream,
        "assistants",
        fileName: Path.GetFileName(filePath),
        cancellationToken);

    // Attach file to current thread message
    await _agentsClient.Messages.CreateMessageAsync(
        _currentThread.Id,
        MessageRole.User,
        $"I've uploaded a file: {Path.GetFileName(filePath)}. Please analyze it.",
        attachments: new[] { uploadedFile.Value.Id },
        cancellationToken);

    return uploadedFile.Value.Id;
}
```

### Agent Run Status Polling Pattern
```csharp
private async Task<ThreadRun> WaitForRunCompletionAsync(ThreadRun run, CancellationToken cancellationToken = default)
{
    var completedStatuses = new[] { RunStatus.Completed, RunStatus.Failed, RunStatus.Cancelled, RunStatus.Expired };

    while (!completedStatuses.Contains(run.Status) && !cancellationToken.IsCancellationRequested)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        run = await _agentsClient.Runs.GetRunAsync(_currentThread.Id, run.Id, cancellationToken);
    }

    if (run.Status != RunStatus.Completed)
    {
        var errorMessage = run.LastError?.Message ?? $"Run ended with status: {run.Status}";
        throw new InvalidOperationException(errorMessage);
    }

    return run;
}
```

### Command Interface
| Command | Format | Description |
|---------|--------|-------------|
| Chat Message | `<any text>` | Send message to agent |
| File Upload | `upload <filepath>` | Upload document for analysis |
| Quit | `quit` | Exit application |
| Cancel | `Ctrl+C` | Force cancellation |

### Agent Response Format
- Text responses displayed as multi-line output
- Image file references shown as `[Image file: {fileId}]`
- Processing status indicated with animated dots
- Error messages prefixed with "Error: "

## 5. Rationale & Context

The application is designed as a bridge between Azure AI Foundry's agent capabilities and end-users who need payroll expertise. Key design decisions:

1. **Console Application**: Provides lightweight, cross-platform accessibility without UI framework dependencies
2. **Generic Host Pattern**: Enables professional-grade dependency injection and configuration management
3. **Single File Structure**: Simplifies deployment and maintenance while maintaining readability
4. **Async/Await Throughout**: Ensures responsive UI and efficient resource utilization
5. **Comprehensive Error Handling**: Critical for production use where network and service failures are expected
6. **File Upload Simulation**: Current implementation prepares for future API updates while providing immediate functionality

The architecture supports easy migration to the stable Azure AI agents API once available, with clear separation of concerns and mockable interfaces.

## 6. Examples & Edge Cases

### Basic Chat Session
```
=== Payroll Agent Chat ===
Type 'quit' to exit, 'upload <filepath>' to upload a file, or just chat normally.

Started new conversation (Thread: thread_abc123)

You: What are the main steps in processing payroll?
Assistant is thinking...
Assistant:
Here are the main steps in processing payroll:
1. Collect employee time data
2. Calculate gross pay
3. Calculate deductions
4. Process net pay
5. Generate payroll reports
6. Submit tax filings

You: quit
Chat ended. Goodbye!
```

### File Upload Scenario
```csharp
// Edge case: File not found
You: upload C:\nonexistent.xlsx
File not found: C:\nonexistent.xlsx

// Edge case: Network interruption during upload
You: upload C:\payroll.xlsx
Uploading file: C:\payroll.xlsx
Error uploading file: Network timeout occurred
```

### Error Handling Examples
```csharp
// Authentication failure
Application error: Authentication failed. Please run 'az login' or configure credentials.

// Configuration missing
Application error: ProjectEndpoint not configured

// Cancellation handling
^C
Application cancelled by user.
```

## 7. Validation Criteria

- **VAL-001**: Application successfully authenticates with Azure using DefaultAzureCredential
- **VAL-002**: Application creates new conversation thread on startup
- **VAL-003**: Application processes user messages and displays agent responses
- **VAL-004**: Application handles file upload command without errors
- **VAL-005**: Application responds to 'quit' command by exiting gracefully
- **VAL-006**: Application cancels operations and exits on Ctrl+C
- **VAL-007**: Application returns exit code 0 on successful completion
- **VAL-008**: Application returns exit code 1 on error conditions
- **VAL-009**: Application logs all significant operations to console
- **VAL-010**: Application handles network interruptions gracefully
- **VAL-011**: Application validates file existence before upload attempts
- **VAL-012**: Application displays processing indicators during agent operations

## 8. Related Specifications / Further Reading

### Azure AI Foundry Documentation
- [Azure AI Foundry Agents Quickstart (C#)](https://learn.microsoft.com/en-us/azure/ai-foundry/agents/quickstart?context=%2Fazure%2Fai-foundry%2Fcontext%2Fcontext&pivots=programming-language-csharp)
- [Azure AI Foundry Agent Environment Setup](https://learn.microsoft.com/en-us/azure/ai-foundry/agents/environment-setup)
- [Azure AI Foundry RBAC Permissions](https://learn.microsoft.com/en-us/azure/ai-foundry/concepts/rbac-azure-ai-foundry)

### Azure SDK Documentation
- [Azure AI Persistent Agents Client Library](https://learn.microsoft.com/en-us/dotnet/api/overview/azure/ai.agents.persistent-readme)
- [Azure AI Persistent Agents Samples](https://github.com/azure-ai-foundry/foundry-samples/tree/main/samples/microsoft/csharp/getting-started-agents)
- [Azure AI Projects SDK](https://github.com/Azure/azure-sdk-for-net/tree/main/sdk/ai/Azure.AI.Projects)
- [Azure AI Agents Library Source Code](https://github.com/Azure/azure-sdk-for-net/tree/main/sdk/ai/Azure.AI.Agents.Persistent)
- [Azure AI Agents NuGet Package](https://www.nuget.org/packages/Azure.AI.Agents.Persistent)

### .NET Framework Documentation
- [.NET Generic Host Documentation](https://docs.microsoft.com/dotnet/core/extensions/generic-host)
- [Azure Identity Documentation](https://docs.microsoft.com/dotnet/api/azure.identity)
- [Microsoft Extensions Logging](https://docs.microsoft.com/dotnet/core/extensions/logging)

### API Reference
- [Azure AI Foundry Agents REST API](https://learn.microsoft.com/en-us/rest/api/aifoundry/aiagents/)
