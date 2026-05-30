# GroupDocs.Annotation MCP Server

MCP server that exposes [GroupDocs.Annotation](https://products.groupdocs.com/annotation) as AI-callable tools
for Claude, Cursor, GitHub Copilot, and other MCP agents.

## Installation

Requires [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

**Run directly with `dnx` (recommended — no install step):**

```bash
dnx GroupDocs.Annotation.Mcp --yes
```

Pulls the latest stable release on every invocation. To pin to a specific
version (recommended for shared configs and CI), append `@<version>`:

```bash
dnx GroupDocs.Annotation.Mcp@26.5.0 --yes
```

**Or install as a global dotnet tool:**

```bash
dotnet tool install -g GroupDocs.Annotation.Mcp
groupdocs-annotation-mcp
```

**Or run via Docker:**

```bash
docker run --rm -i \
  -v $(pwd)/documents:/data \
  ghcr.io/groupdocs-annotation/annotation-net-mcp:latest
```

## Native prerequisites

The underlying GroupDocs engine rasterises annotated pages and previews via
`System.Drawing` (GDI+). When you run the server **natively** (via `dnx` or
the global dotnet tool) on Linux or macOS, install the native `libgdiplus`
library and a fonts package first:

| Platform | Setup |
|---|---|
| Windows | Nothing — GDI+ is built into the OS. |
| Linux | `sudo apt-get install -y libgdiplus libfontconfig1 ttf-mscorefonts-installer` |
| macOS | `brew install mono-libgdiplus` |
| Docker | Nothing — the image already bundles libgdiplus, libfontconfig1, and ttf-mscorefonts-installer. |

Skipping this on Linux/macOS surfaces as `DllNotFoundException: libgdiplus` in
the tool response. The simplest zero-setup option on Linux/macOS is the
**Docker image**.

## Available MCP Tools

| Tool | Description |
|---|---|
| `AddAnnotation` | Add a textfield / area / point / arrow / highlight / underline / strikeout annotation; saves the annotated file as `<name>_annotated.<ext>` |
| `GetAnnotations` | List all annotations in a document as JSON (id, type, message, page, box, user, replies) |
| `UpdateAnnotation` | Modify an existing annotation's message and/or bounding box by id |
| `RemoveAnnotations` | Remove annotations by id list, or all if none specified |
| `AddReply` | Add a reply / comment thread to an existing annotation |
| `RemoveReplies` | Remove replies by reply-id list, by user name, or all |
| `ImportAnnotations` | Import annotations from an XML file or another annotated document into a target file |
| `ExportAnnotations` | Extract annotations from a document and save them as XML (re-importable via ImportAnnotations) |
| `GetDocumentInfo` | Return file type, page count, size, and per-page dimensions as JSON (no modification) |
| `GeneratePagesPreview` | Render up to 5 document pages as inline PNG images so AI clients display them directly |

## Example prompts

- "Annotate contract.pdf — add a highlight on page 2 around the indemnity clause and an arrow with the comment 'Needs legal review'"
- "List every annotation in design-review.docx and group them by author"
- "Reply to annotation 7 in spec.pdf with 'Implemented in PR #42' under the user 'alice'"
- "Export the annotations from old.pdf to XML, then re-import them into new.pdf"
- "Preview page 1 and page 5 of report.pdf with annotations baked in"

## Configuration

| Variable | Description | Default |
|---|---|---|
| `GROUPDOCS_MCP_STORAGE_PATH` | Base folder for input and output files | current directory |
| `GROUPDOCS_MCP_OUTPUT_PATH` | *(Optional)* separate folder for output files | `GROUPDOCS_MCP_STORAGE_PATH` |
| `GROUPDOCS_LICENSE_PATH` | Path to GroupDocs license file | (evaluation mode) |

## Usage with Claude Desktop

```json
{
  "mcpServers": {
    "groupdocs-annotation": {
      "type": "stdio",
      "command": "dnx",
      "args": ["GroupDocs.Annotation.Mcp", "--yes"],
      "env": {
        "GROUPDOCS_MCP_STORAGE_PATH": "/path/to/documents"
      }
    }
  }
}
```

> To pin to a specific version, replace `"GroupDocs.Annotation.Mcp"` with
> `"GroupDocs.Annotation.Mcp@26.5.0"` in `args`. Pinning is recommended for
> shared / committed configs to avoid surprise upgrades.

## Usage with VS Code / GitHub Copilot

NuGet.org generates a ready-to-use `mcp.json` snippet on the [package page](https://www.nuget.org/packages/GroupDocs.Annotation.Mcp).
Copy it directly into your `.vscode/mcp.json`.

Alternatively, add manually to `.vscode/mcp.json`:

```json
{
  "inputs": [
    {
      "type": "promptString",
      "id": "storage_path",
      "description": "Base folder for input and output files.",
      "password": false
    }
  ],
  "servers": {
    "groupdocs-annotation": {
      "type": "stdio",
      "command": "dnx",
      "args": ["GroupDocs.Annotation.Mcp", "--yes"],
      "env": {
        "GROUPDOCS_MCP_STORAGE_PATH": "${input:storage_path}"
      }
    }
  }
}
```

> Same pinning rule as above — swap `"GroupDocs.Annotation.Mcp"` for
> `"GroupDocs.Annotation.Mcp@26.5.0"` to lock to a specific release.

## Usage with Docker Compose

```bash
cd docker
docker compose up
```

Edit `docker/docker-compose.yml` to point volumes at your local documents folder.

## License

MIT — see [LICENSE](LICENSE)

<!-- mcp-name: io.github.groupdocs-annotation/groupdocs-annotation-mcp -->
