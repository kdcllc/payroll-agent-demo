# Payroll Agent Chat CLI

A .NET 8 console application that provides a multi-turn chat interface with an Azure AI Foundry Agent for payroll-related tasks.

## Current Status

**Note**: This is a demo version that currently simulates agent responses. The Azure AI Projects SDK for C# is in beta and the agents API is still evolving. This application provides the complete framework and can be easily updated to use the actual Azure AI agents once the API stabilizes.

## Features

- Multi-turn chat conversation with an AI agent (currently simulated)
- File upload capability to provide additional context
- Uses Azure DefaultAzureCredential for authentication
- Creates a new thread for each session
- Graceful error handling and logging
- Full .NET 8 console application structure with best practices
- Proper cancellation token support (Ctrl+C handling)
- Structured using dependency injection and the Generic Host pattern
- Exit codes for proper CLI behavior

## Architecture

The application follows .NET 8 CLI best practices:

- **Single-file structure**: All code consolidated in `Program.cs`
- **Generic Host pattern**: Uses Microsoft.Extensions.Hosting for dependency injection
- **Cancellation support**: Proper CancellationToken handling throughout
- **Exit codes**: Returns appropriate exit codes (0 for success, 1 for errors)
- **Structured logging**: Uses Microsoft.Extensions.Logging
- **Async/await patterns**: All I/O operations are properly async

## Prerequisites

- .NET 8 SDK
- Azure AI Foundry project with an agent configured
- Azure authentication configured (Azure CLI login or environment variables)

## Configuration

Update the `appsettings.json` file with your Azure AI Foundry project details:

```json
{
  "ProjectEndpoint": "https://your-project-endpoint.services.ai.azure.com/api/projects/your-project",
  "AgentId": "your-agent-id"
}
```

## Implementation Notes

The application is structured to easily integrate with the actual Azure AI Foundry agents API once it becomes stable. Key components that will need updating when the API is finalized:

1. **Thread Creation**: Currently simulated in `CreateNewThreadAsync()`
2. **Message Processing**: Currently uses simple response logic in `ProcessUserMessageAsync()`
3. **File Upload**: Currently simulated but structured for easy API integration

The `PayrollChatService` class provides the complete framework for:
- Thread management
- Message handling
- File upload processing
- Error handling and logging
- Cancellation token support

## Project Structure

- `PayrollAgentChat.csproj` - Project file with .NET 8 and required packages
- `Program.cs` - Complete application code (main entry point and chat service)
- `appsettings.json` - Configuration file for Azure endpoints and agent ID
- `README.md` - This documentation

## Authentication

This application uses `DefaultAzureCredential` which will attempt authentication in this order:
1. Environment variables
2. Managed Identity
3. Azure CLI
4. Azure PowerShell
5. Interactive browser

Make sure you're logged in with Azure CLI:
```bash
az login
```

## Usage

1. Build and run the application:
```bash
dotnet run
```

2. Start chatting with the agent by typing messages and pressing Enter

3. Upload files for the agent to analyze:
```
upload C:\path\to\your\file.pdf
```

4. Type `quit` to exit the application

## Commands

- `quit` - Exit the application
- `upload <filepath>` - Upload a file to the current conversation thread
- Any other text - Send a message to the agent

## Example Session

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

You: upload C:\payroll-data.xlsx
Uploading file: C:\payroll-data.xlsx
File uploaded successfully: payroll-data.xlsx
You can now ask questions about this file.

You: Can you analyze the payroll data I just uploaded?
Assistant is thinking...
Assistant:
I can see the payroll data you've uploaded. Let me analyze it for you...

You: quit
Chat ended. Goodbye!
```

## Error Handling

The application includes comprehensive error handling for:
- Network connectivity issues
- Authentication failures
- File upload errors
- Agent processing errors

All errors are logged and displayed to the user with helpful messages.

## Logging

The application uses Microsoft.Extensions.Logging with console output. Log levels can be configured in `appsettings.json` if needed.
