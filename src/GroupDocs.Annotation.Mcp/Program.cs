using System.Reflection;
using GroupDocs.Mcp.Core.Licensing;
using GroupDocs.Annotation.Mcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var version = typeof(Program).Assembly
    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
    ?.InformationalVersion
    ?.Split('+')[0]
    ?? "0.0.0";

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
builder.Services
    .AddGroupDocsMcp()
    .AddLocalStorage("./Files");
builder.Services.AddSingleton<ILicenseManager, AnnotationLicenseManager>();
builder.Services
    .AddMcpServer(options => { options.ServerInfo = new() { Name = "GroupDocs.Annotation.Mcp", Version = version }; })
    .WithStdioServerTransport()
    .WithToolsFromAssembly();
await builder.Build().RunAsync();
