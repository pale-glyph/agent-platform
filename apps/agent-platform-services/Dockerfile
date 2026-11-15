FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /source
COPY . .

FROM build AS publish-api
RUN dotnet publish apps/agent-platform-services/src/AgentPlatform.Api/AgentPlatform.Api.csproj --configuration Release --output /publish/api

FROM build AS publish-agent-api
RUN dotnet publish apps/agent-platform-services/src/AgentPlatform.AgentApi/AgentPlatform.AgentApi.csproj --configuration Release --output /publish/agent-api

FROM build AS publish-worker
RUN dotnet publish apps/agent-platform-services/src/AgentPlatform.Worker/AgentPlatform.Worker.csproj --configuration Release --output /publish/worker

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

FROM runtime AS api
COPY --from=publish-api /publish/api .
EXPOSE 8080
ENTRYPOINT ["dotnet", "AgentPlatform.Api.dll"]

FROM runtime AS agent-api
COPY --from=publish-agent-api /publish/agent-api .
EXPOSE 8081
ENTRYPOINT ["dotnet", "AgentPlatform.AgentApi.dll"]

FROM runtime AS worker
COPY --from=publish-worker /publish/worker .
ENTRYPOINT ["dotnet", "AgentPlatform.Worker.dll"]
