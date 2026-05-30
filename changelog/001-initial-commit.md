---
id: 001
date: 2026-05-29
version: 26.5.0
type: feature
---

# Initial release of GroupDocs.Annotation.Mcp

## What changed

- New MCP server exposing GroupDocs.Annotation for .NET as AI-callable tools, packaged as `GroupDocs.Annotation.Mcp` on NuGet (PackageType=`McpServer`, ToolCommandName=`groupdocs-annotation-mcp`) and as `ghcr.io/groupdocs-annotation/annotation-net-mcp` + `docker.io/groupdocs/annotation-net-mcp` Docker images.
- Target framework `net10.0`, `dnx`-launchable.
- **10 tools exposed**:
  - `AddAnnotation` — supports text / area / point / arrow / highlight / underline / strikeout types; saves `<name>_annotated.<ext>`.
  - `GetAnnotations` — JSON: id, type, message, page (1-based), bounding box, user, replies (Pitfall #16 — raw JSON, never via `OutputHelper.TruncateText`).
  - `UpdateAnnotation` — modify message and/or bounding box by id.
  - `RemoveAnnotations` — by comma-separated id list or all.
  - `AddReply` — add a reply to an annotation by id (collaboration workflow).
  - `RemoveReplies` — by reply-id list, by user name, or all.
  - `ImportAnnotations` — auto-detects XML vs. document source by extension; dispatches to `Annotator.ExportAnnotationsFromXMLFile` (XML) or `Annotator.ImportAnnotationsFromDocument` (.pdf/.docx/etc.). The engine's naming is inverted from the English sense — the wrapper hides that.
  - `ExportAnnotations` — XmlSerializer dump of the `List<AnnotationBase>` to `<name>.annotations.xml`.
  - `GetDocumentInfo` (Step 11 mandatory) — JSON with fileType / pageCount / size / per-page width+height via reflection over `IDocumentInfo.Pages`.
  - `GeneratePagesPreview` — returns `CallToolResult` with one `TextContentBlock` (summary, with eval-mode prefix when unlicensed) plus one `ImageContentBlock.FromBytes(..., "image/png")` per page. Hard cap of 5 pages per call (`MaxPagesPerCall`) to stay under MCP client size limits. Accepts comma-separated page lists ('1,3,5') or ranges ('1-3'). Pages are rendered via `Annotator.Document.GeneratePreview(PreviewOptions { PreviewFormat = PNG, PageNumbers = ... })`.
- All 10 tools wrap their engine call in `try { … } catch (Exception ex) { return FormatException(...); }` (Pitfall #18) so engine exceptions surface as descriptive `"<Op> failed for '<file>': <Type>: <msg> | inner(0): ..."` strings instead of MCP's opaque `"An error occurred invoking '<tool>'"`. `GeneratePagesPreview` returns `CallToolResult { IsError = true }` in the catch path.
- Environment variables: `GROUPDOCS_MCP_STORAGE_PATH`, optional `GROUPDOCS_MCP_OUTPUT_PATH`, `GROUPDOCS_LICENSE_PATH`.
- Native dependencies on Linux: `libgdiplus` + `libfontconfig1` + `ttf-mscorefonts-installer` (the engine rasterises annotated pages and previews with text glyphs baked in, so MS core fonts are required). `System.Drawing.EnableUnixSupport` is set in the csproj.
- **SkiaSharp explicit pin (3.119.4)** + matching `SkiaSharp.NativeAssets.Linux.NoDependencies 3.119.4`. `GroupDocs.Annotation 25.11.0` pins managed SkiaSharp 3.x but transitively pulls the old 2.80.1 NativeAssets, which is ABI-incompatible with managed 3.x; the explicit pin keeps the Linux runtime resolution coherent. Re-evaluate when the engine bumps its transitive.
- License is applied via Pattern A (the cross-product License-class style): `new GroupDocs.Annotation.License().SetLicense(licensePath)` — `License` is public in the engine.
- Unit tests: xUnit + Moq, one test class per tool, 39 tests total (license manager 2 + per-tool 3-4 each).

## Why

Exposes GroupDocs.Annotation for .NET as AI-callable MCP tools for Claude, Cursor, VS Code / GitHub Copilot, and other MCP-compatible agents, so document review, mark-up, comment threads, annotation import/export, and page preview can be invoked from natural-language instructions without bespoke per-agent integration. `GeneratePagesPreview` implements the cross-MCP roadmap of self-sufficient page rendering — users of this server don't need to attach the Viewer MCP to see what they're annotating.

## Migration / impact

First release of this repository — no migration. Engine version `GroupDocs.Annotation 25.11.0`; CalVer `26.5.0` (YY.M.N, first month-release uses N=0).
