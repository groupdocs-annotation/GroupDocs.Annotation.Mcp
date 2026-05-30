using GroupDocs.Mcp.Core;
using GroupDocs.Mcp.Core.Licensing;
using GroupDocs.Annotation.Mcp.Tools;
using Moq;
using Xunit;

namespace GroupDocs.Annotation.Mcp.Tests;

public class UpdateAnnotationToolTests
{
    private readonly Mock<IFileResolver> _resolver = new();
    private readonly Mock<ILicenseManager> _licenseManager = new();
    private readonly Mock<IFileStorage> _storage = new();
    private readonly OutputHelper _output;

    public UpdateAnnotationToolTests()
    {
        _output = new OutputHelper(_storage.Object, Microsoft.Extensions.Options.Options.Create(new McpConfig()));
    }

    [Fact]
    public async Task UpdateAnnotation_WhenResolverThrows_PropagatesException()
    {
        _resolver
            .Setup(r => r.ResolveAsync(It.IsAny<FileInput>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FileNotFoundException("missing.pdf"));

        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            UpdateAnnotationTool.UpdateAnnotation(
                _resolver.Object,
                _storage.Object,
                _licenseManager.Object,
                _output,
                new FileInput { FilePath = "missing.pdf" },
                id: 1, message: "x"));
    }

    [Fact]
    public async Task UpdateAnnotation_WhenResolverThrows_DoesNotWriteToStorage()
    {
        _resolver
            .Setup(r => r.ResolveAsync(It.IsAny<FileInput>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FileNotFoundException("missing.pdf"));

        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            UpdateAnnotationTool.UpdateAnnotation(
                _resolver.Object,
                _storage.Object,
                _licenseManager.Object,
                _output,
                new FileInput { FilePath = "missing.pdf" },
                id: 1, message: "x"));

        _storage.Verify(
            s => s.WriteFileAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAnnotation_SetsLicense_BeforeResolving()
    {
        var sequence = new List<string>();

        _licenseManager.Setup(l => l.SetLicense()).Callback(() => sequence.Add("license"));
        _resolver
            .Setup(r => r.ResolveAsync(It.IsAny<FileInput>(), It.IsAny<CancellationToken>()))
            .Callback(() => sequence.Add("resolve"))
            .ThrowsAsync(new InvalidOperationException("short-circuit"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            UpdateAnnotationTool.UpdateAnnotation(
                _resolver.Object,
                _storage.Object,
                _licenseManager.Object,
                _output,
                new FileInput { FilePath = "missing.pdf" },
                id: 1, message: "x"));

        Assert.Equal(new[] { "license", "resolve" }, sequence);
    }

    [Fact]
    public async Task UpdateAnnotation_PassesFileInputToResolver_Unchanged()
    {
        var input = new FileInput { FilePath = "doc.docx" };
        FileInput? captured = null;

        _resolver
            .Setup(r => r.ResolveAsync(It.IsAny<FileInput>(), It.IsAny<CancellationToken>()))
            .Callback<FileInput, CancellationToken>((fi, _) => captured = fi)
            .ThrowsAsync(new InvalidOperationException("short-circuit"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            UpdateAnnotationTool.UpdateAnnotation(
                _resolver.Object,
                _storage.Object,
                _licenseManager.Object,
                _output,
                input,
                id: 1, message: "x"));

        Assert.Same(input, captured);
    }
}
