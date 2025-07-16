# Instructions for AI Agents in this Repository
This is a .NET 8 console application that provides a multi-turn chat interface with an Azure AI Foundry Agent for payroll-related tasks and document analysis. The application uses Azure AI Foundry's Persistent Agents API to create intelligent conversations about payroll processes, regulations, and data analysis.

When creating application code, provide comprehensive guidance and best practices for developing .NET 8 applications that are designed to run in Azure. Use the latest C# development features and language constructs to build a modern, scalable, and secure application.

## Key Principles
- Use the latest C# language features and constructs to build modern, scalable, and secure applications.
- Use SOLID principles (Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, and Dependency Inversion) to design and implement your application.
- Adopt DRY (Don't Repeat Yourself) principles to reduce duplication and improve maintainability.
- Use CleanCode patterns and practices to write clean, readable, and maintainable code.
- Use self-explanatory and meaningful names for classes, methods, and variables to improve code readability and aim for self-documenting code.
- Use Dependency Injection to manage dependencies and improve testability.
- Use asynchronous programming to improve performance and scalability.
- Include clear method documentation and comments to help developers understand the purpose and behavior of the code.
- Prioritize secure coding practices, such as input validation, output encoding, and parameterized queries, to prevent common security vulnerabilities.
- Use Azure AI Projects SDK and Azure AI Agents Persistent library to interact with Azure AI Foundry agents.
- Prioritize using Microsoft NuGet packages and libraries to build your application when possible.
- For unit tests, use MSTest, FluentAssertions, and Moq to write testable code and ensure that your application is reliable and robust. As well as using AAA pattern for test structure.
- Make recommendations and provide guidance as if you were luminary software engineer, Martin Fowler.

## CLI Commands
- **build**: `dotnet build` or VS Code task `Build Payroll Agent Chat`
- **run**: `dotnet run` - starts interactive chat session
- **test**: `dotnet test` in solution root
- **test single**: `dotnet test --filter "FullyQualifiedName=Namespace.Class.Method"`
- **format**: `dotnet format`
- **docs**: view `README.md`, `spec/spec-app-payroll-agent-chat.md`

## Chat Application Commands
- `quit` - exit the application gracefully
- `upload <filepath>` - upload document for agent analysis (PDF, Excel, etc.)
- `Ctrl+C` - force cancellation and exit
- Any other text - send message to payroll agent

## High-level Architecture
- **Single Console App**: All code in `Program.cs` using .NET 8 Generic Host pattern
- **Azure AI Integration**: Uses Azure AI Foundry Persistent Agents API
- **Key Dependencies**: Azure.AI.Projects, Azure.AI.Agents.Persistent, Azure.Identity
- **Authentication**: DefaultAzureCredential supporting multiple auth methods
- **Configuration**: `appsettings.json` with ProjectEndpoint and AgentId
- **Knowledge Base**: Payroll documents in `knowledge/` folder for agent training

## Style & Conventions
- Target .NET 8 with C# 11 features (async/await, records, pattern matching)
- Follow SOLID, DRY, CleanCode; meaningful, self-documenting names
- PascalCase for types/methods; camelCase for parameters/locals
- Dependency Injection via `HostBuilderExtensions` and `IOptions<T>`
- Secure coding: parameterized queries, input validation, output encoding
- Logging via `Microsoft.Extensions.Logging`
- Code formatting: run `dotnet format` (pre-commit), follow default .editorconfig or EditorConfig conventions
- Tests: Use AAA pattern, clear test names `Method_State_Expected`, mock with Moq, assert with FluentAssertions

## Agent Rules
- This `.github/copilot-instructions.md` directs AI agents in this repo
- Preserve existing Azure and infrastructure guidance
- Merge, don’t overwrite; be concise and factual
