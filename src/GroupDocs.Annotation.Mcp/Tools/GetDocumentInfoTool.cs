using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Text.Json;
using GroupDocs.Annotation.Options;
using GroupDocs.Mcp.Core;
using GroupDocs.Mcp.Core.Licensing;
using ModelContextProtocol.Server;

namespace GroupDocs.Annotation.Mcp.Tools;

[McpServerToolType]
public static class GetDocumentInfoTool
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    [McpServerTool, Description(
        "Returns file type, page count, size, and per-page dimensions for a document as JSON. " +
        "Supports PDF, DOCX, XLSX, PPTX, and 50+ more document and image formats. " +
        "Call this tool whenever the user asks for document info, page count, size, dimensions, or wants to inspect a document before annotating. " +
        "Do NOT pre-check whether files exist — pass the filename the user provided directly. " +
        "Returns a JSON object with `fileType`, `pageCount`, `size`, and `pages` (array of per-page width/height when available). " +
        "On failure, the response text starts with 'Document-info lookup failed for' followed by the underlying exception type, message, and inner-exception chain.")]
    public static async Task<string> GetDocumentInfo(
        IFileResolver resolver,
        ILicenseManager licenseManager,
        FileInput file,
        [Description("Password for protected documents")] string? password = null)
    {
        licenseManager.SetLicense();
        using var resolved = await resolver.ResolveAsync(file);

        try
        {
            using var inputMs = new MemoryStream();
            await resolved.Stream.CopyToAsync(inputMs);
            inputMs.Position = 0;

            var loadOptions = password != null ? new LoadOptions { Password = password } : null;
            using var annotator = loadOptions != null
                ? new Annotator(inputMs, loadOptions)
                : new Annotator(inputMs);

            var info = annotator.Document.GetDocumentInfo();
            if (info is null)
                return $"Document-info lookup failed for '{resolved.FileName}': engine returned null.";

            // Reflection-based per-page dimension extraction — IDocumentInfo subtypes
            // (PdfDocumentInfo, WordDocumentInfo, etc.) expose different page-collection
            // names; this stays subtype-agnostic.
            var pages = ExtractPages(info);

            // Pitfall #16: return raw JSON, never via OutputHelper.TruncateText.
            return JsonSerializer.Serialize(new
            {
                fileType  = info.FileType?.ToString(),
                pageCount = info.PageCount,
                size      = info.Size,
                pages,
            }, JsonOptions);
        }
        catch (Exception ex)
        {
            return FormatException(ex, resolved.FileName);
        }
    }

    private static IReadOnlyList<object> ExtractPages(object info)
    {
        var pagesProp = info.GetType().GetProperty("Pages", BindingFlags.Public | BindingFlags.Instance);
        if (pagesProp?.GetValue(info) is not System.Collections.IEnumerable pages)
            return Array.Empty<object>();

        var results = new List<object>();
        foreach (var page in pages)
        {
            if (page is null) continue;
            var t = page.GetType();
            results.Add(new
            {
                number = t.GetProperty("Number")?.GetValue(page),
                width  = t.GetProperty("Width")?.GetValue(page),
                height = t.GetProperty("Height")?.GetValue(page),
            });
        }
        return results;
    }

    private static string FormatException(Exception ex, string fileName)
    {
        var sb = new StringBuilder();
        sb.Append($"Document-info lookup failed for '{fileName}': ");
        sb.Append($"{ex.GetType().FullName}: {ex.Message}");
        var inner = ex.InnerException;
        for (int depth = 0; inner != null && depth < 5; depth++, inner = inner.InnerException)
            sb.Append($" | inner({depth}): {inner.GetType().FullName}: {inner.Message}");
        return sb.ToString();
    }
}
