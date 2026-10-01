# Build from the repository root: podman build -f examples/csharp-game-server/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY sdks/csharp/Directory.Build.props sdks/csharp/Directory.Build.props
COPY sdks/csharp/README.md sdks/csharp/README.md
COPY sdks/csharp/src/AgonesTournament.Sdk.Core/ sdks/csharp/src/AgonesTournament.Sdk.Core/
COPY sdks/csharp/src/AgonesTournament.Sdk.GameServer/ sdks/csharp/src/AgonesTournament.Sdk.GameServer/
COPY examples/csharp-game-server/GameServer.cs examples/csharp-game-server/GameServer.cs
RUN dotnet publish examples/csharp-game-server/GameServer.cs -c Release -r linux-x64 \
    --self-contained false -p:PublishSingleFile=true -p:DebugType=None -o /app

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app/ ./
USER $APP_UID
ENTRYPOINT ["/app/GameServer"]
