FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["cqtrailsclientcore/cqtrailsclientcore.csproj", "cqtrailsclientcore/"]
RUN dotnet restore "cqtrailsclientcore/cqtrailsclientcore.csproj"
COPY . .
WORKDIR "/src/cqtrailsclientcore"
RUN dotnet build "cqtrailsclientcore.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "cqtrailsclientcore.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "cqtrailsclientcore.dll"]