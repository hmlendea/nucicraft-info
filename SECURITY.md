# Security Policy

This policy defines how security vulnerabilities concerning the NuciCraft Info Website are reported, assessed, and disclosed. The latest released version is the only version that receives security maintenance.

## 📑 Table of Contents

- [Supported Versions](#supported-versions)
- [Reporting a Vulnerability](#reporting-a-vulnerability)
- [Scope](#scope)
- [Disclosure Policy](#disclosure-policy)
- [Automated Security Scanning](#automated-security-scanning)

## 🛡️ Supported Versions

Use this table to indicate which project versions currently receive security maintenance.

| Version | Distribution Channel | Supported |
|---------|--------------------|-----------|
| Latest version | GitHub Releases | ✅ |
| Latest version | Unofficial third-party distribution channels | ❌ |
| Preceding versions | Any distribution channel | ❌ |

## 🚨 Reporting a Vulnerability

Please do not disclose suspected vulnerabilities publicly before maintainers have had an opportunity to validate and remediate them.

To report a vulnerability:
- [GitHub Security Advisories](https://github.com/hmlendea/nucicraft-info/security/advisories)
- Contact the maintainers directly

## 📌 Scope

The original maintainers are not responsible for changes, modifications, or security issues introduced by downstream forks.

The subsequent report categories are in scope for this repository:
- Vulnerabilities in the application source code of this repository.
- Vulnerabilities in the project dependencies that the application ships.

The subsequent categories are out of scope unless explicitly stated to the contrary:
- Vulnerabilities in the upstream NuciCraft API that this application proxies.
- Vulnerabilities in third-party front-end assets that are loaded from content delivery networks.

## 📢 Disclosure Policy

This project follows coordinated disclosure:
1. Vulnerabilities are investigated privately.
2. A remediation plan is prepared and validated.
3. Public disclosure is published after a fix, mitigation, or agreed risk decision is available.
4. Credit is attributed in accordance with reporter preference and project policy.

## 🔍 Automated Security Scanning

This repository runs automated security scans through the GitHub Actions Security workflow. The scan includes:
- **NuGet packages** The .NET CLI lists packages with known vulnerabilities, including transitive dependencies.
- **C# source code** GitHub CodeQL analyses the C# codebase on pushes and pull requests, together with a weekly scheduled scan.

Results are published to the [Security](https://github.com/hmlendea/nucicraft-info/security) tab and the [Actions](https://github.com/hmlendea/nucicraft-info/actions/workflows/security.yml) workflow page.
