# Contributor Guidelines

## Project Structure
- **src/**: Main C# libraries and client code
  - `ClassicUO.Client`: game client logic
  - `ClassicUO.Assets`: asset and data loaders
  - `ClassicUO.IO`: file handling utilities
  - `ClassicUO.Renderer`: rendering system
  - `ClassicUO.Utility`: shared helpers
  - `ClassicUO.Bootstrap`: platform bootstrap code
- **tests/**: unit tests (`ClassicUO.UnitTests`)
- **tools/**: auxiliary tools (manifest creator, etc.)
- **external/**: bundled dependencies and native libraries

## Coding Standards
- Follow settings in `.editorconfig` (4 spaces for C#, 2 for XML/JSON/config)
- Constants are ALL_CAPS with underscore separators
- Prefer explicit types over `var` unless type is obvious
- Keep line length under 180 characters

## Build & Run
- Clone repository with submodules: `git clone --recursive`
- Build: `dotnet build ClassicUO.sln`
- Launch scripts: see `scripts/` for platform-specific helpers

## Testing & Formatting
- When modifying **code**, run:
  - `dotnet format ClassicUO.sln` for style checks
  - `dotnet test` to execute the unit tests
- If only editing documentation or comments, tests are not required

## Contribution Workflow
- Use `rg` for searching the codebase
- Commit directly on the current branch with descriptive messages
- Update `todo.md` when adding or resolving tasks
- After committing, create a pull request

## Additional Notes
- This project targets .NET; ensure the appropriate SDK is installed
- Refer to `README.md` for build prerequisites and platform details
