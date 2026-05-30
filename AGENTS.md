# AGENTS.md — Guide for AI coding agents

Brief orientation for AI coding agents (Claude Code, Copilot, Cursor, Aider, Amp, Codex) working in this repository.

## What this repo is

A standalone **MCP server** for [GroupDocs.Annotation for .NET](https://products.groupdocs.com/annotation) — exposes document annotation operations (add / list / update / remove / reply / import / export / preview) as AI-callable tools via the Model Context Protocol.

Published to NuGet as `GroupDocs.Annotation.Mcp` with the `McpServer` package type, and to `ghcr.io/groupdocs-annotation/annotation-net-mcp` + `docker.io/groupdocs/annotation-net-mcp` as a container image.

## MCP tools exposed

| Tool | Description |
|---|---|
| `AddAnnotation` | Adds a textfield / area / point / arrow / highlight / underline / strikeout annotation; saves as `<name>_annotated.<ext>` |
| `GetAnnotations` | Lists all annotations as JSON (id, type, message, page, box, user, replies) |
| `UpdateAnnotation` | Updates an existing annotation's message and/or bounding box by id |
| `RemoveAnnotations` | Removes annotations by id list, or all if none specified |
| `AddReply` | Adds a reply to an existing annotation |
| `RemoveReplies` | Removes replies by reply-id list, by user name, or all |
| `ImportAnnotations` | Imports annotations from an XML file or another annotated document |
| `ExportAnnotations` | Extracts annotations to XML (re-importable via ImportAnnotations) |
| `GetDocumentInfo` | Returns file type, page count, size, and per-page dimensions as JSON (read-only) |
| `GeneratePagesPreview` | Renders up to 5 pages as inline PNG image content blocks (annotations baked in) |

All tools accept `FileInput` (resolved via `IFileResolver`) and an optional `password` for protected documents.

## Folder layout

```
src/                                                       ← all projects + sln + Directory.Build.props
  GroupDocs.Annotation.Mcp/
    Program.cs                                             ← host bootstrap + stdio transport
    AnnotationLicenseManager.cs                            ← applies GroupDocs.Total license
    Tools/
      AddAnnotationTool.cs                                 ← [McpServerTool] — AddAnnotation
      GetAnnotationsTool.cs                                ← [McpServerTool] — GetAnnotations
      UpdateAnnotationTool.cs                              ← [McpServerTool] — UpdateAnnotation
      RemoveAnnotationsTool.cs                             ← [McpServerTool] — RemoveAnnotations
      AddReplyTool.cs                                      ← [McpServerTool] — AddReply
      RemoveRepliesTool.cs                                 ← [McpServerTool] — RemoveReplies
      ImportAnnotationsTool.cs                             ← [McpServerTool] — ImportAnnotations
      ExportAnnotationsTool.cs                             ← [McpServerTool] — ExportAnnotations
      GetDocumentInfoTool.cs                               ← [McpServerTool] — GetDocumentInfo
      GeneratePagesPreviewTool.cs                          ← [McpServerTool] — GeneratePagesPreview
    .mcp/
      server.json                                          ← NuGet.org reads this to generate mcp.json snippet
    GroupDocs.Annotation.Mcp.csproj                        ← PackageType=McpServer + ToolCommandName
  GroupDocs.Annotation.Mcp.Tests/                          ← xUnit + Moq unit tests
  GroupDocs.Annotation.Mcp.sln
  Directory.Build.props
build/
  dependencies.props                                       ← single source of truth for all versions
changelog/                                                 ← one MD file per change (see changelog/README.md)
docker/
  Dockerfile                                               ← multi-stage, runtime on aspnet:10.0
  docker-compose.yml
.github/workflows/                                         ← build_packages.yml, run_tests.yml, publish_prod.yml, publish_docker.yml
```

## Dependencies

- `GroupDocs.Mcp.Core` + `GroupDocs.Mcp.Local.Storage` — infrastructure NuGet packages from the [GroupDocs.Mcp.Core](https://github.com/groupdocs/GroupDocs.Mcp.Core) repo
- `GroupDocs.Annotation` — the annotation engine
- `ModelContextProtocol` — MCP SDK for .NET
- `Microsoft.Extensions.Hosting` — host builder for the stdio server
- `SkiaSharp` + `SkiaSharp.NativeAssets.Linux.NoDependencies` — pinned explicitly to matching `3.119.4`. The engine pulls managed SkiaSharp 3.x but transitively pulls the old 2.x NativeAssets, which is ABI-incompatible. The explicit pin keeps the Linux runtime resolution coherent. Re-evaluate when the engine bumps its transitive.

## Commands you can run

```bash
# Restore + build
dotnet restore
dotnet build src/GroupDocs.Annotation.Mcp.sln -c Release

# Run unit tests
dotnet test src/GroupDocs.Annotation.Mcp.sln -c Release

# Run the server locally (stdio)
dotnet run --project src/GroupDocs.Annotation.Mcp

# Local pack (writes to ./build_out) — validates server.json version matches dependencies.props
pwsh ./build.ps1

# Build + run the Docker image
docker build -f docker/Dockerfile -t annotation-net-mcp:local .
docker run --rm -i -v $(pwd)/documents:/data annotation-net-mcp:local
```

## Version scheme

CalVer `YY.MM.N`. The version lives in **two** places that MUST stay in lockstep:
1. `build/dependencies.props` → `<GroupDocsAnnotationMcp>`
2. `src/GroupDocs.Annotation.Mcp/.mcp/server.json` → both top-level `"version"` and `packages[0].version`

`build.ps1` enforces this at pack time (`Assert-ServerJsonVersionMatchesDependencies`) — if they drift, the build fails.

## Pre-shipped pitfall remediations

The following cross-product pitfalls were addressed at clone time and are already in the codebase:

- **Pitfall #18 (unhandled exceptions in tool methods)**: all 10 tools wrap their engine calls in a top-level `try { … } catch (Exception ex) { return FormatException(ex, …); }` block. Engine failures surface as a descriptive `"<Op> failed for '<file>': <type>: <msg>"` string instead of MCP's opaque `"An error occurred invoking '<tool>'"`. Do not remove these wrappers. `GeneratePagesPreview` returns `CallToolResult { IsError = true }` instead of a plain string.
- **Pitfall #16 (JSON via TruncateText)**: tools that return JSON (`GetAnnotations`, `GetDocumentInfo`) call `JsonSerializer.Serialize(...)` directly and never pipe their JSON through `OutputHelper.TruncateText`. The truncation marker is plain text and breaks JSON parsing.

## Native-deps note

On Linux, `System.Drawing.Common` (used by the engine to rasterise annotated pages and previews) requires `libgdiplus` + `libfontconfig1`. Because the engine renders text glyphs (annotation messages, baked-in comment text), `ttf-mscorefonts-installer` is also installed (with the debconf EULA accept + `fc-cache`) so Arial / Times New Roman etc. are discoverable. The `System.Drawing.EnableUnixSupport` runtime host config option is set in the csproj. `SkiaSharp.NativeAssets.Linux.NoDependencies` IS pinned at the csproj level (matching managed `SkiaSharp 3.119.4`) to override the engine's transitive 2.80.1 native asset, which is ABI-incompatible with managed Skia 3.x.

## House rules

1. **Tools must have rich `[Description("...")]` strings** — these are what AI agents read via the MCP protocol. Write them as task-oriented sentences, not method-signature summaries. Always include: supported formats, response shape, failure prefix.
2. **Never add new env vars beyond** `GROUPDOCS_MCP_STORAGE_PATH`, `GROUPDOCS_MCP_OUTPUT_PATH`, `GROUPDOCS_LICENSE_PATH` without updating `server.json`, `docker-compose.yml`, and `README.md` together.
3. **Tests use xUnit + Moq** — mock `IFileResolver`, `IFileStorage`, `ILicenseManager`, `OutputHelper`.
4. **Changelog entries required** — any PR that changes behaviour adds `changelog/NNN-slug.md`.
5. **Do not edit `obj/` or `build_out/`** — build artifacts.
6. **Target framework is `net10.0` only** — required by `dnx` and the MCP SDK.

## Release flow

See [RELEASE.md](RELEASE.md) for the exact per-release checklist.

## What NOT to change

- Do not hardcode the version in `.csproj` — it flows from `$(GroupDocsAnnotationMcp)` in `dependencies.props`.
- Do not remove the `<PackageType>McpServer</PackageType>` or `<ToolCommandName>groupdocs-annotation-mcp</ToolCommandName>` from the csproj — NuGet.org discoverability and `dnx` invocation depend on them.
- Do not change the `.mcp/server.json` schema URL without cross-checking with the NuGet MCP docs.
