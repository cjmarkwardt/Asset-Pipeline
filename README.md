# Asset Pipeline

Asset Pipeline is a desktop application for managing projects that rely on AI generation APIs to
produce game/media assets. It coordinates requests across different AI providers and asset types
(art, audio, models, text, etc.), tracks generated output, and helps keep a project's asset library
organized as it grows.

## Solution Structure

| Project | Description |
| --- | --- |
| [`Client/`](Client) | Avalonia desktop application providing the UI for managing projects and asset generation. |
| [`Tests/`](Tests) | xUnit test suite covering `Client` components. |

The solution file [`AssetPipeline.slnx`](AssetPipeline.slnx) ties both projects together.

## Getting Started

Requires the .NET SDK matching the target framework used by the projects.

```bash
# Restore and build the whole solution
dotnet build

# Run the client application
dotnet run --project Client

# Run the test suite
dotnet test
```

## Development

Project conventions and coding standards are documented in [`AGENTS.md`](AGENTS.md).
