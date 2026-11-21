FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY apps/knowledge-base ./apps/knowledge-base
RUN dotnet publish apps/knowledge-base/src/KnowledgeBase.Api/KnowledgeBase.Api.csproj -c Release -o /publish/api
RUN dotnet publish apps/knowledge-base/src/KnowledgeBase.Worker/KnowledgeBase.Worker.csproj -c Release -o /publish/worker

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
FROM runtime AS api
COPY --from=build /publish/api .
EXPOSE 8082
ENTRYPOINT ["dotnet", "KnowledgeBase.Api.dll"]
FROM runtime AS worker
COPY --from=build /publish/worker .
ENTRYPOINT ["dotnet", "KnowledgeBase.Worker.dll"]
