# Privacy and Personal Data

This document describes how the NuciCraft Info Website handles personal data, both when it is operated by the project maintainers and when a user or organisation operates its own instance. It covers the application conduct and integrations verified in this repository as of the current version.

## 📑 Table of Contents

- [What This Document Covers](#-what-this-document-covers)
- [Self-Hosted Deployments](#-self-hosted-deployments)
- [Data We Handle](#-data-we-handle)
  - [Data Provided to the Application](#data-provided-to-the-application)
  - [Data Generated or Collected by the Application](#data-generated-or-collected-by-the-application)
  - [Data Received from Integrations](#data-received-from-integrations)
- [Processing and Use](#-processing-and-use)
- [Storage, Retention, and Deletion](#-storage-retention-and-deletion)
- [External Processing and Integrations](#-external-processing-and-integrations)
- [User Controls and Requests](#-user-controls-and-requests)
- [International Transfers](#-international-transfers)
- [Data Protection and Security](#-data-protection-and-security)
- [Document Changes](#-document-changes)
- [Contact](#-contact)

## 🔎 What This Document Covers

This document describes how the NuciCraft Info Website at [https://github.com/hmlendea/nucicraft-info](https://github.com/hmlendea/nucicraft-info) manages personal data. It covers the application conduct and verified integrations described below. Where the software is self-hosted, the instance operator may have separate responsibilities described below.

## 🏠 Self-Hosted Deployments

This document covers the project maintainers and the operator of a self-hosted instance, which is the person or organisation that deploys and runs the application. The instance operator controls the configuration of that instance, its local storage, its logs, its backups, its access controls, the preservation of its data, and the handling of requests concerning that data, unless the project directly controls those functions.

Each instance queries the NuciCraft API defined by `NuciCraftApi:BaseUrl` in order to present the server figures. The application contains no telemetry, no update check, and no crash reporting service, so a self-hosted instance transmits no data of its visitors to the project maintainers. An operator that publishes the instance on the public internet nevertheless makes the ordinary access logs of the hosting web server available to the party that operates that server.

## 📥 Data We Handle

### Data Provided to the Application

- No personal data is requested, registered, or submitted. The application offers no accounts, no forms, and no user input, so a visitor supplies no personal data to it.

### Data Generated or Collected by the Application

- No personal data is generated or collected automatically. The application sets no cookies, runs no analytics, and writes no visit log of its own.
- The application holds the public server figures described below in its memory cache, and that copy derives from a public API response rather than from a visitor.

### Data Received from Integrations

- No personal data is received from integrations or third parties. The application receives the public server name, hostname, port numbers, and online player count from the NuciCraft API, and it renders those figures to every visitor in the same form.

## 🧭 Processing and Use

The application processes the data described above for these verified functions:
- Presentation of the server figures — public server name, hostname, ports, and online player count retrieved from the NuciCraft API
- Shortening of response latency — the retrieved API response held in the server memory cache for the configured duration

The server's hosting environment additionally receives the requests that reach the web server in order to serve the site, and those requests determine the access logs of the hosting environment rather than any log written by the application.

## 🗄️ Storage, Retention, and Deletion

The application stores the retrieved NuciCraft API response in the output cache held in the memory of the running process. It contains the public server figures and no visitor information. The entry expires after `NuciCraftApi:CacheDurationInMinutes`, whose value cannot be lower than `30` minutes, and it disappears with the process when the process stops. The application writes no database, no log file, no cookie, and no local storage on this basis.

For a self-hosted deployment, the instance operator controls the storage, the retention, and the deletion of any access log, error log, or backup produced by the hosting environment, the reverse proxy in front of the application, or the operating system.

The browser stores the static assets it receives, including those loaded from third-party content delivery networks, according to the cache headers those providers define. That storage is governed by the browser configuration of the visitor, and the application neither controls nor deletes it.

## 🔗 External Processing and Integrations

The public server figures are retrieved server-side from the NuciCraft API, which the application reaches through `NuciAPI.Client` with a bearer token carried in the `Authorization` header and with the `X-Client-ID`, `X-Request-ID`, and `X-Timestamp` metadata headers. The request identifies the application by the client identifier `NuciCraftInfo` and contains no visitor information.

The rendered pages load stylesheets, scripts, typefaces, and imagery directly from third-party providers, so the browser contacts those providers and discloses to them the ordinary information that a browser discloses when it requests a resource. The pages also contain ordinary hyperlinks to destinations such as the world map, the donation page, the Discord community, the launcher downloads, and the voting pages, which contact the destination only after the visitor follows the link.

| Service or integration | Purpose | Data involved | Configuration or documentation |
|-----------------------|---------|---------------|--------------------------------|
| [NuciCraft API](https://github.com/hmlendea/nucicraft-api) | Retrieve the server figures and the online player count rendered by the Info section | Public server status and online player count | `NuciCraftApi:BaseUrl` in `NuciCraftInfo/appsettings.json`; see [Integrations](./README.md#integrations) |
| [jsDelivr](https://www.jsdelivr.com/) | Serve the Bootstrap 4.6.0 stylesheet and bundle | Ordinary browser request metadata | Linked in `NuciCraftInfo/Components/App.razor` |
| [cdnjs](https://cdnjs.com/) | Serve Animate.css, jQuery, jQuery Easing, and WOW.js | Ordinary browser request metadata | Linked in `NuciCraftInfo/Components/App.razor` |
| [Start Bootstrap](https://startbootstrap.github.io/startbootstrap-freelancer/) | Serve the Freelancer theme stylesheet and script | Ordinary browser request metadata | Linked in `NuciCraftInfo/Components/App.razor` |
| [Google Fonts](https://fonts.google.com/) | Serve the Montserrat and Lato typefaces | Ordinary browser request metadata | Linked in `NuciCraftInfo/Components/App.razor` |
| [Font Awesome](https://fontawesome.com/) | Serve the iconography of the interface | Ordinary browser request metadata | Linked in `NuciCraftInfo/Components/App.razor` |
| [imgur](https://i.imgur.com/wtqqImu.png) | Serve the site icon and the guidance imagery | Ordinary browser request metadata | Linked in `NuciCraftInfo/Components/App.razor` and `NuciCraftInfo/Components/Pages/Home.razor` |
| [GitHub](https://raw.githubusercontent.com/hmlendea/nucilandia/master/Coat%20of%20Arms/Coat%20of%20Arms.svg) | Serve the coat of arms displayed on the masthead | Ordinary browser request metadata | Linked in `NuciCraftInfo/Components/Pages/Home.razor` |

## ⚙️ User Controls and Requests

The following documented controls or request procedures are available:
- Configuration of the NuciCraft API — the instance operator sets `NuciCraftApi:BaseUrl` to the API that supplies the figures, and points it at another deployment of the NuciCraft API when one is required; see [Configuration](./README.md#configuration)
- Configuration of the cache duration — the instance operator sets `NuciCraftApi:CacheDurationInMinutes`, which cannot be lower than `30`, in order to choose for how long a retrieved response is reused; see [Reload Behaviour](./README.md#reload-behaviour)
- Visitor-side control of the third-party assets — the operator of an instance that must be free of third-party requests replaces the external links of `NuciCraftInfo/Components/App.razor` with locally served copies, because this repository does not implement that localisation
- Request handling for an instance — a person that operates the instance answers requests concerning the data that instance processes, and the project maintainers answer requests concerning the hosted website at the contact below

## 🌍 International Transfers

The third-party providers listed above and the hosts referenced by the pages process each request from their own infrastructure, which spans multiple countries, so a request may be answered outside the country of both the visitor and the operator. For a self-hosted deployment, the operator determines the country in which the application, the NuciCraft API it queries, and its reverse proxy operate.

## 🛡️ Data Protection and Security

The NuciCraft `ApiKey` is held in the configuration of the instance and is transmitted to the NuciCraft API as a bearer token; it is never sent to a visitor, never written into a page, and must be supplied through the environment or the command line rather than committed. Outside the development environment the application redirects every HTTP request to HTTPS, emits HTTP Strict Transport Security headers, and registers an exception handler that prevents an error message from being rendered to a visitor, so no exception detail reaches the response. The proxied endpoint answers only from the NuciCraft API and passes no client-supplied value to it.

For a self-hosted deployment, the instance operator is responsible for installing updates, for protecting the API key and any other secret, for restricting access to the host, for maintaining backups, for limiting network exposure, and for protecting any log produced by the hosting environment. No measure removes the possibility of exposure entirely.

The original maintainers are not responsible for changes, modifications, or privacy issues introduced by downstream forks.

## 🔄 Document Changes

Update this document when application data processes, storage, integrations, or deployment responsibilities change. The current version is published at [PRIVACY.md](https://github.com/hmlendea/nucicraft-info/blob/master/PRIVACY.md).

## 📬 Contact

For questions about application data handling, contact the project maintainers by [opening an issue](https://github.com/hmlendea/nucicraft-info/issues). For a self-hosted instance, contact the instance operator, unless the project explicitly manages the request. Include the instance URL and the page on which the question arises; do not send passwords, access tokens, or other secrets.