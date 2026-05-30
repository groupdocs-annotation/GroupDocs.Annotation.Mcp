using GroupDocs.Mcp.Core;
using GroupDocs.Mcp.Core.Licensing;
using GroupDocs.Annotation.Mcp.Tools;
using Moq;
using Xunit;

namespace GroupDocs.Annotation.Mcp.Tests;

public class GeneratePagesPreviewToolTests
{
    private readonly Mock<IFileResolver> _resolver = new();
    private readonly Mock<ILicenseManager> _licenseManager = new();


    [Fact]
    public async Task GeneratePagesPreview_WhenResolverThrows_PropagatesException()
    {
        _resolver
            .Setup(r => r.ResolveAsync(It.IsAny<FileInput>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FileNotFoundException("missing.pdf"));

        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            GeneratePagesPreviewTool.GeneratePagesPreview(
                _resolver.Object,
                _licenseManager.Object,
                new FileInput { FilePath = "missing.pdf" },
                pages: "1"));
    }


    [Fact]
    public async Task GeneratePagesPreview_SetsLicense_BeforeResolving()
    {
        var sequence = new List<string>();

        _licenseManager.Setup(l => l.SetLicense()).Callback(() => sequence.Add("license"));
        _resolver
            .Setup(r => r.ResolveAsync(It.IsAny<FileInput>(), It.IsAny<CancellationToken>()))
            .Callback(() => sequence.Add("resolve"))
            .ThrowsAsync(new InvalidOperationException("short-circuit"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            GeneratePagesPreviewTool.GeneratePagesPreview(
                _resolver.Object,
                _licenseManager.Object,
                new FileInput { FilePath = "missing.pdf" },
                pages: "1"));

        Assert.Equal(new[] { "license", "resolve" }, sequence);
    }

    [Fact]
    public async Task GeneratePagesPreview_PassesFileInputToResolver_Unchanged()
    {
        var input = new FileInput { FilePath = "doc.docx" };
        FileInput? captured = null;

        _resolver
            .Setup(r => r.ResolveAsync(It.IsAny<FileInput>(), It.IsAny<CancellationToken>()))
            .Callback<FileInput, CancellationToken>((fi, _) => captured = fi)
            .ThrowsAsync(new InvalidOperationException("short-circuit"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            GeneratePagesPreviewTool.GeneratePagesPreview(
                _resolver.Object,
                _licenseManager.Object,
                input,
                pages: "1"));

        Assert.Same(input, captured);
    }
}
