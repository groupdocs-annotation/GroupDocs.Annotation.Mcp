FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/GroupDocs.Annotation.Mcp/GroupDocs.Annotation.Mcp.csproj \
    -c Release -o /app --no-self-contained

FROM base AS final

# Native graphics deps GroupDocs.Annotation needs on Linux. The engine rasterizes
# annotated document pages and previews via System.Drawing.Common — that path
# P/Invokes libgdiplus (the GDI+ implementation) and uses libfontconfig1 to
# discover fonts. Without both, AddAnnotation / GeneratePagesPreview throw
# PlatformNotSupportedException on Linux. Because the engine RENDERS text
# glyphs (annotation messages baked onto pages, comment thread text, etc.),
# Microsoft core fonts (Arial / Times New Roman) must also be installed —
# ttf-mscorefonts-installer ships them, with debconf-set-selections to auto-accept
# the EULA and fc-cache to refresh the font cache so freshly-installed faces are
# discoverable. Pair this with <RuntimeHostConfigurationOption Include=
# "System.Drawing.EnableUnixSupport" Value="true" /> in the csproj.
USER root
RUN apt-get update \
 && echo "ttf-mscorefonts-installer msttcorefonts/accepted-mscorefonts-eula select true" | debconf-set-selections \
 && apt-get install -y --no-install-recommends \
      libgdiplus libfontconfig1 ttf-mscorefonts-installer \
 && fc-cache -f -v > /dev/null \
 && rm -rf /var/lib/apt/lists/*

# OCI image labels. VERSION is supplied by the publish workflow via
# --build-arg VERSION=YY.M.N. Default 'dev' makes local `docker build` runs
# self-identify instead of inheriting the base image's misleading version.
ARG VERSION=dev
LABEL org.opencontainers.image.title="GroupDocs.Annotation.Mcp" \
      org.opencontainers.image.description="MCP server exposing GroupDocs.Annotation for .NET as AI-callable tools (AddAnnotation, GetAnnotations, UpdateAnnotation, RemoveAnnotations, AddReply, RemoveReplies, ImportAnnotations, ExportAnnotations, GetDocumentInfo, GeneratePagesPreview) for Claude, Cursor, GitHub Copilot, and other MCP agents." \
      org.opencontainers.image.version="${VERSION}" \
      org.opencontainers.image.source="https://github.com/groupdocs-annotation/GroupDocs.Annotation.Mcp" \
      org.opencontainers.image.url="https://www.nuget.org/packages/GroupDocs.Annotation.Mcp" \
      org.opencontainers.image.licenses="MIT" \
      org.opencontainers.image.vendor="Aspose Pty Ltd"

COPY --from=build /app .

ENV GROUPDOCS_MCP_STORAGE_PATH=/data
VOLUME /data

USER $APP_UID

ENTRYPOINT ["dotnet", "GroupDocs.Annotation.Mcp.dll"]
