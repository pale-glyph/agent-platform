FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY Directory.Build.props ./
COPY apps/tenancy/src ./apps/tenancy/src
RUN dotnet publish apps/tenancy/src/Tenancy.Api/Tenancy.Api.csproj -c Release -o /publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /publish .
EXPOSE 8083
ENTRYPOINT ["dotnet", "Tenancy.Api.dll"]
