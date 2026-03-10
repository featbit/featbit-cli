---
applyTo: "**"
---

# Coding Guidelines

## Technology Stack

1. Use **.NET 10** as the target framework for all projects.
2. **FeatBit.Cli** is an **AOT (Ahead-of-Time compilation)** project (`<PublishAot>true</PublishAot>`). All code must be AOT-compatible: avoid reflection, dynamic types, and non-trimmer-friendly patterns.

## Documentation & Research

When uncertain about .NET, C#, or any Microsoft technology (APIs, libraries, SDK behavior, best practices, etc.), always search the official Microsoft documentation first using the `#microsoftdocs` MCP tool before answering or generating code. This ensures the information is accurate and up-to-date.