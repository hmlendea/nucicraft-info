[![GitHub Pages](https://img.shields.io/badge/GitHub%20Pages-Visit-blue)](https://hmlendea.github.io/nucicraft-info/)
[![Build Status](https://github.com/hmlendea/nucicraft-info/actions/workflows/dotnet.yml/badge.svg)](https://github.com/hmlendea/nucicraft-info/actions/workflows/dotnet.yml)
[![License](https://img.shields.io/github/license/hmlendea/nucicraft-info)](https://github.com/hmlendea/nucicraft-info/blob/master/LICENSE)

# NuciCraft Info Website

The player information website for the NuciCraft Minecraft server.

## 📑 Table of Contents

- [Capabilities](#capabilities)
- [Use Cases](#use-cases)
- [Usage](#usage)
- [Known Limitations](#known-limitations)
- [Configuration](#configuration)
  - [Configuration Files](#configuration-files)
  - [Settings](#settings)
  - [Precedence](#precedence)
  - [Reload Behaviour](#reload-behaviour)
  - [Secret Management](#secret-management)
  - [Profiles and Scopes](#profiles-and-scopes)
  - [Network Ports](#network-ports)
  - [Validation](#validation)
- [Integrations](#integrations)
- [Privacy and Data](#privacy-and-data)
- [Development](#development)
  - [Requirements](#requirements)
  - [Setup](#setup)
  - [Build](#build)
  - [Run](#run)
  - [Test](#test)
  - [Coverage](#coverage)
  - [Release](#release)
  - [Dependencies](#dependencies)
- [GitHub Actions](#github-actions)
- [Project Structure](#project-structure)
  - [Projects and Packages](#projects-and-packages)
  - [Directories](#directories)
- [Deployment](#deployment)
- [Troubleshooting](#troubleshooting)
- [Contributing](#contributing)
- [Related Projects](#related-projects)
- [Acknowledgements](#acknowledgements)
- [Support](#support)
- [Project Engagement](#project-engagement)
- [License](#license)

## ✨ Capabilities

- **Server information:** presents the server address, the world map, the donation page, and the Discord community in a single scrolling page.
- **Live player count:** shows the number of players currently connected to the server, retrieved from the NuciCraft API.
- **Connection guidance:** explains how to link a Minecraft account to Discord, how to install a launcher, and how to set a skin, each in a dedicated dialog.
- **Short-link redirects:** forwards the legacy `/links/*.html` voting and Discord invitation addresses to their destinations.
- **Discoverability:** publishes page metadata and a `robots.txt`.
- **Responsive presentation:** adapts to desktop and mobile viewports, with animated reveals while scrolling and a typewriter effect on the masthead.

## 🎯 Use Cases

- **Server players:** find the connection address, the world map, and instructions for linking Discord, installing a launcher, and setting a skin.
- **Prospective players:** check whether the server is online and how many players are connected before joining.
- **Community members:** reach the voting pages and the Discord invitation through stable short links.

## 🚀 Usage

Open the hosted website in a browser. The home page is one scrolling document organised into a masthead and an **Info** section, whose rows link to the server address, the live player count, the Discord dialog, the world map, the donation page, the launcher dialog, and the skin dialog.

The pages are rendered on the server; client-side scripts provide the typewriter effect, the animated reveals, and the retrieval of the live player count.

The player count is fed by a JSON endpoint that proxies the NuciCraft API:
| Endpoint | Description |
|----------|-------------|
| `/api/nucicraft/server` | Server name, hostname, Java and Bedrock ports, and the live online player count. |

The legacy short links remain available and redirect to their destinations:
| Address | Destination |
|---------|-------------|
| `/links/discord-invitation` | The Discord invitation of the community. |
| `/links/vote-minecraft-mp` | The Minecraft MP voting page. |
| `/links/vote-minecraftserverlist` | The Minecraft Server List voting page. |
| `/links/vote-planetminecraft` | The Planet Minecraft voting page. |
| `/links/vote-topminecraftservers` | The Top Minecraft Servers voting page. |

Each redirect also answers on its legacy `.html` address.

## ⚠️ Known Limitations

- The live player count is available only while the NuciCraft API is reachable and the configured API key is accepted; otherwise the figure stays at `—`.
- The figure is cached on the server for at least 30 minutes, so it can lag behind the server.
- The interface depends on third-party content delivery networks for Bootstrap, Start Bootstrap Freelancer, Font Awesome, jQuery, Animate.css, WOW.js, and Google Fonts; if those are unreachable, the presentation degrades.
- The player-facing content is written in Romanian only.
- GitHub Pages serves repository files verbatim, so it cannot host this ASP.NET Core application.

## ⚙️ Configuration

### Configuration Files

| File | Scope | Purpose |
|------|-------|---------|
| `NuciCraftInfo/appsettings.json` | Application | Defines the NuciCraft API address, the API key placeholder, the cache duration, the log levels, and the accepted host headers. |

### Settings

The subsequent settings are recognised:
| Section | Key | Type | Default | Required | Description |
|---------|-----|------|---------|----------|-------------|
| `NuciCraftApi` | `BaseUrl` | `string` | `https://api.mc.nucilandia.ro/` | Yes | Base address of the NuciCraft API that supplies the server information. |
| `NuciCraftApi` | `ApiKey` | `string` | `—` | Yes | Bearer token accepted by the NuciCraft API. |
| `NuciCraftApi` | `CacheDurationInMinutes` | `integer` | `30` | No | Number of minutes for which the proxied API responses are cached. |
| `Logging:LogLevel` | `Default` | `string` | `Information` | No | Minimum log level for all categories. |
| `Logging:LogLevel` | `Microsoft.AspNetCore` | `string` | `Warning` | No | Minimum log level for the ASP.NET Core categories. |
| — | `AllowedHosts` | `string` | `*` | No | Semicolon-separated list of accepted `Host` header values. |

### Precedence

Configuration is resolved through the standard ASP.NET Core providers. From highest to lowest precedence:
- Command-line arguments
- Environment variables
- `appsettings.json`

### Reload Behaviour

`NuciCraftApi:CacheDurationInMinutes` is read once while starting and determines the expiry of the output cache, so a change requires restarting the application. The remaining settings are read through the configuration providers on each request.

### Secret Management

`NuciCraftApi:ApiKey` is a credential and must never be committed. Supply it through an environment variable or a command-line argument in every environment that must reach the API:

```bash
NuciCraftApi__ApiKey='…' dotnet run --project NuciCraftInfo/NuciCraftInfo.csproj
```

### Profiles and Scopes

Local development relies on the launch profiles declared in `NuciCraftInfo/Properties/launchSettings.json`:
| Profile | Environment | URLs |
|---------|-------------|------|
| `http` | `Development` | `http://localhost:5039` |
| `https` | `Development` | `https://localhost:7226` and `http://localhost:5039` |

Outside the development environment the application registers the exception handler, the HTTP Strict Transport Security middleware, and the HTTPS redirection.

### Network Ports

| Port | Protocol | Direction | Purpose | Required |
|------|----------|-----------|---------|----------|
| `5039` | HTTP | Inbound | Local development through the `http` launch profile. | No |
| `7226` | HTTPS | Inbound | Local development through the `https` launch profile. | No |

### Validation

A value below `30` for `NuciCraftApi:CacheDurationInMinutes` causes the application to throw `ArgumentOutOfRangeException` while starting, so the process refuses to run until the value is corrected. Opening `/api/nucicraft/server` reports whether the API address and key are effective: a successful response carries the server information, while a rejected key or an unreachable API yields `503 Service Unavailable`.

## 🔌 Integrations

| Integration | Compatibility | Purpose | Required |
|-------------|---------------|---------|----------|
| [NuciCraft API](https://github.com/hmlendea/nucicraft-api) | `GET /Server`, bearer `Authorization` header, `content` response envelope | Supplies the server name, hostname, ports, and the live online player count shown in the Info section. | Yes |

Requests to the NuciCraft API are made through `NuciAPI.Client`, which attaches the bearer token and the `X-Client-ID`, `X-Request-ID`, and `X-Timestamp` metadata headers the API boundary requires.

## 🛡️ Privacy and Data

NuciCraft Info Website sets no cookies, runs no analytics, and persists no information concerning its visitors.

The public server figures shown in the interface are retrieved server-side and held in a memory cache in order to limit the load on the NuciCraft API. The pages additionally load assets from third-party services, so the browser contacts jsDelivr, cdnjs, Google Fonts, Font Awesome, Start Bootstrap, and GitHub directly for stylesheets, scripts, typefaces, and images.

| Data | Purpose | Storage | Retention | Optional |
|------|---------|---------|-----------|----------|
| Public server status and online player count | Render the Info section and the live player count | Server memory | At least 30 minutes after each retrieval | No |

## 🛠️ Development

### Requirements

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- A reachable NuciCraft API instance and its API key, for the live player count

### Setup

```bash
git clone https://github.com/hmlendea/nucicraft-info.git
cd nucicraft-info
dotnet restore NuciCraftInfo/NuciCraftInfo.csproj
```

### Build

```bash
dotnet build NuciCraftInfo/NuciCraftInfo.csproj
```

The equivalent `build`, `publish`, and `watch` tasks are defined in `.vscode/tasks.json`.

### Run

```bash
NuciCraftApi__ApiKey='…' dotnet run --project NuciCraftInfo/NuciCraftInfo.csproj
```

The `http` launch profile applies by default, so the site becomes available at `http://localhost:5039`. For an automatic restart after every change:

```bash
NuciCraftApi__ApiKey='…' dotnet watch run --project NuciCraftInfo/NuciCraftInfo.csproj
```

### Test

The NUnit unit suite renders the Razor pages with bUnit, while the integration suite hosts the application with `WebApplicationFactory` and replaces the NuciCraft `HttpClient` with a stub handler, so no live API access is required:

```bash
dotnet test NuciCraftInfo.slnx
```

### Coverage

Generate a Cobertura report with Coverlet:

```bash
dotnet test NuciCraftInfo.slnx --collect:"XPlat Code Coverage" --results-directory TestResults
```

### Release

Releases are produced by the [GitHub Release](https://github.com/hmlendea/nucicraft-info/blob/master/.github/workflows/github-release.yml) workflow. Maintainers publish a GitHub release whose tag carries the semantic version, with an optional leading `v`; publishing it triggers the workflow, which builds the self-contained archives and attaches them to that release.

The workflow derives the .NET version from `NuciCraftInfo/NuciCraftInfo.csproj` and downloads and executes an external release helper from `https://raw.githubusercontent.com/hmlendea/dotnet-release-scripts/refs/heads/master/dotnet/10.0.sh` to build the artefacts.

**Note:** Piping into `bash` is an intensely controversial topic. Please review any external scripts before running them in your environment!

The same build can be reproduced locally with the published SDK:

```bash
dotnet publish NuciCraftInfo/NuciCraftInfo.csproj --configuration Release --runtime linux-x64 --self-contained true
```

### Dependencies

| Package | Version | Scope | Purpose |
|---------|---------|-------|---------|
| `NuciAPI.Client` | 1.2.4 | Runtime | Supplies the request/response contracts and the `INuciApiClient` used to call the NuciCraft API, including the required metadata headers. |
| `NuciWeb.HTTP` | 1.7.3 | Runtime | Provides the HTTP client factory used to query the NuciCraft API. |
| `Bootstrap` | 4.6.0 | Runtime (front end) | Layout, components, and utility styles. |
| `Start Bootstrap Freelancer` | — | Runtime (front end) | Base theme of the presentation layout. |
| `Font Awesome` | 6.4.0 | Runtime (front end) | Iconography. |
| `jQuery` | 3.6.3 | Runtime (front end) | Client-side helper library. |
| `jQuery Easing` | 1.12.1 | Runtime (front end) | Easing functions for the scroll animations. |
| `Animate.css` | 4.1.1 | Runtime (front end) | Entry animations. |
| `WOW.js` | 1.1.2 | Runtime (front end) | Reveals animated elements while scrolling. |
| `bunit` | 2.15.2 | Development | Renders the Razor components in the unit tests. |
| `coverlet.collector` | 10.1.0 | Development | Coverage collection during test execution. |
| `Microsoft.AspNetCore.Mvc.Testing` | 10.0.14 | Development | Hosts the application for the integration tests. |
| `Microsoft.NET.Test.Sdk` | 18.11.0 | Development | Test runner infrastructure. |
| `NUnit` | 5.0.0 | Development | Unit testing framework. |
| `NUnit.Analyzers` | 4.15.0 | Development | NUnit assertion and API analysers. |
| `NUnit3TestAdapter` | 6.3.1 | Development | VS Test Platform adapter. |

## ⚙️ GitHub Actions

| Workflow | Purpose | What it does |
|----------|---------|--------------|
| [ .NET ](https://github.com/hmlendea/nucicraft-info/blob/master/.github/workflows/dotnet.yml) | Continuous integration | Derives the .NET version from the project file, installs the matching SDK, restores dependencies, builds the solution, and runs the unit and integration tests on pushes and pull requests targeting `master`. |
| [ GitHub Release ](https://github.com/hmlendea/nucicraft-info/blob/master/.github/workflows/github-release.yml) | Release automation | Derives the .NET version from the project file, installs the matching SDK, reads the version from the published tag, builds the self-contained archives through the shared release script, and publishes them as the release assets. |

## 🗂️ Project Structure

The repository hosts the ASP.NET Core project, its unit and integration test projects, alongside the solution file, the release helper, and the editor configuration.

### Projects and Packages

| Project | Type | Purpose |
|---------|------|---------|
| `NuciCraftInfo/` | ASP.NET Core web application | Renders and serves the website. |
| `NuciCraftInfo.UnitTests/` | NUnit test project | Verifies the rendered pages and components with bUnit. |
| `NuciCraftInfo.IntegrationTests/` | NUnit test project | Verifies the proxied endpoint and the startup configuration against a stubbed NuciCraft API. |

### Directories

| Directory | Purpose |
|-----------|---------|
| `NuciCraftInfo/Components/` | Razor components, where `App.razor` is the HTML document, `Routes.razor` the router, and `Pages/` the individual pages. |
| `NuciCraftInfo/Components/Pages/Links/` | Pages that redirect the legacy short links to their destinations. |
| `NuciCraftInfo/Endpoints/` | Minimal API endpoints that expose the NuciCraft API data. |
| `NuciCraftInfo/Properties/` | Launch profiles used during local development. |
| `NuciCraftInfo/Requests/` | NuciCraft API request contracts. |
| `NuciCraftInfo/Responses/` | NuciCraft API response contracts. |
| `NuciCraftInfo/Service/` | Application services that query the NuciCraft API through `NuciAPI.Client`. |
| `NuciCraftInfo/wwwroot/` | Static assets: stylesheets, scripts, and `robots.txt`. |
| `NuciCraftInfo.UnitTests/` | bUnit tests covering the rendered pages and layout. |
| `NuciCraftInfo.IntegrationTests/` | Endpoint and startup tests hosting the application in memory. |
| `.vscode/` | Editor tasks and launch configuration for building, publishing, and running the site. |

## 🚢 Deployment

The release built by the [GitHub Release](https://github.com/hmlendea/nucicraft-info/blob/master/.github/workflows/github-release.yml) workflow contains self-contained archives for `linux-arm`, `linux-arm64`, `linux-x64`, `osx-arm64`, `osx-x64`, `win-arm64`, and `win-x64`. Deploy the published output of the matching archive on a host that runs the ASP.NET Core application and terminates TLS, because the application redirects HTTP requests to HTTPS and, outside the development environment, emits HTTP Strict Transport Security headers. Supply `NuciCraftApi:ApiKey` through the environment of that host.

## 🩺 Troubleshooting

| Symptom | Probable Cause | Resolution |
|----------|----------------|------------|
| The player count stays at `—` | `NuciCraftApi:ApiKey` is empty, or `NuciCraftApi:BaseUrl` does not reach a running API | Provide the key through the environment and confirm the base address resolves to the API. |
| `/api/nucicraft/server` answers `503 Service Unavailable` | The API rejected the bearer token or was unreachable | Confirm the key is accepted by the API and that the host can reach it. |

## 🤝 Contributing

You are welcome to submit any suggestion, feedback, or modification to this project.

When doing so, please:
- Maintain cross-platform compatibility
- Submit focused pull requests that conform to the existing code style
- Maintain your branch synchronised with `master`
- Revise the documentation when functionality changes
- Properly test all modifications, including edge cases and error conditions
- Include tests for additional or modified functionality
- Raise a new [issue](https://github.com/hmlendea/nucicraft-info/issues) for problems or suggestions

## 🔗 Related Projects

- [NuciCraft API](https://github.com/hmlendea/nucicraft-api): the REST service that supplies the server information and the live online player count shown by this website.

## 🙏 Acknowledgements

- [Bootstrap](https://getbootstrap.com/): layout, components, and utility styles.
- [Start Bootstrap Freelancer](https://startbootstrap.com/theme/freelancer): the theme the presentation layout derives from.
- [Font Awesome](https://fontawesome.com/): iconography.
- [jQuery](https://jquery.com/) and [jQuery Easing](https://github.com/gdsmith/jquery.easing): client-side helpers used by the scripts.
- [Animate.css](https://animate.style/) and [WOW.js](https://wowjs.uk/): entry and scroll animations.
- [Google Fonts](https://fonts.google.com/): the Montserrat and Lato typefaces.
- [NuciLandia](https://github.com/hmlendea/nucilandia): the NuciCraft coat of arms and the community artwork.

## 💬 Support

For support, please [open an issue](https://github.com/hmlendea/nucicraft-info/issues).

## 💝 Project Engagement

Discovered a problem with NuciCraft Info Website or have a suggestion for it? [Open an issue](https://github.com/hmlendea/nucicraft-info/issues)!

If you find this project useful, consider starring ⭐️ it on GitHub!

## 📄 License

This project is being distributed under the `GNU General Public License v3.0` or later.
See [LICENSE](./LICENSE) for additional information.
