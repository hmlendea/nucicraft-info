[![Donate](https://img.shields.io/badge/-%E2%99%A5%20Donate-%23ff69b4)](https://hmlendea.go.ro/funding)

# Contributing to NuciCraft Info Website

This document covers the guidelines and processes for contributing to this project, including how to report issues, suggest enhancements, submit code changes, and follow project standards.

## 📑 Table of Contents

- [Contributing](#contributing)
  - [Reporting Issues](#reporting-issues)
  - [Suggesting Enhancements](#suggesting-enhancements)
  - [Code Contributions](#code-contributions)
    - [Prerequisites](#prerequisites)
    - [Development Setup](#development-setup)
    - [Making Changes](#making-changes)
    - [Pull Request Guidelines](#pull-request-guidelines)
  - [Testing](#testing)
  - [Documentation](#documentation)
- [Donations](#donations)
- [License](#license)
- [Getting Assistance](#getting-assistance)

## 🤝 Contributing

### Reporting Issues

- Search existing issues first.
- Use the issue templates if available.
- Provide clear reproduction steps.
- Include environment details.

### Suggesting Enhancements

- Check the existing discussions.
- Explain the use case and expected conduct.
- Consider the implementation complexity.

### Code Contributions

#### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

#### Development Setup

```bash
# Clone the repository
git clone https://github.com/hmlendea/nucicraft-info.git
cd nucicraft-info

# Install dependencies
dotnet restore NuciCraftInfo/NuciCraftInfo.csproj
```

#### Making Changes

1. Fork the repository.
2. Create a feature branch: `git checkout -b feature/your-feature-name`
3. Make your changes.
4. Run tests: `dotnet test NuciCraftInfo.slnx`
5. Commit with clear and descriptive messages.
6. Push to your fork.
7. Open a Pull Request.

#### Pull Request Guidelines

- Target the `master` branch.
- Keep PRs focused and atomic.
- Revise documentation if applicable.
- Include tests for any new functionality.
- Ensure the CI checks pass.

### Testing

```bash
# Run all tests
dotnet test NuciCraftInfo.slnx

# Run specific test suite
dotnet test NuciCraftInfo.UnitTests/NuciCraftInfo.UnitTests.csproj
```

### Documentation

- Revise relevant docs for changes.
- Follow the documentation style guide.
- Preview changes locally if possible.
- When updating dependencies (e.g., NuGet packages) or modifying code that affects project structure, update relevant documentation files (README.md, SECURITY.md, ARCHITECTURE.md, docs/, or any other project-specific documentation files) to reflect the changes.

## � Donations

This project is developed and maintained during spare time. If you find it useful, consider [donating](https://hmlendea.go.ro/funding) and giving it a star ⭐️ on GitHub!

## �📄 License

By contributing, you agree that your contributions will be licensed under the GNU General Public License v3.0.

## ❓ Getting Assistance

- Open an issue on the [issue tracker](https://github.com/hmlendea/nucicraft-info/issues).
- Check existing discussions and documentation first.