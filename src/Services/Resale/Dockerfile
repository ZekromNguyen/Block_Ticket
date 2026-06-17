FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Services/Resale/Resale.Api.csproj", "src/Services/Resale/"]
COPY ["src/Services/Ticketing/Ticketing.Application/Ticketing.Application.csproj", "src/Services/Ticketing/Ticketing.Application/"]
COPY ["src/Services/Ticketing/Ticketing.Domain/Ticketing.Domain.csproj", "src/Services/Ticketing/Ticketing.Domain/"]
COPY ["src/Shared/Common/Shared.Common.csproj", "src/Shared/Common/"]
COPY ["src/Shared/Contracts/Shared.Contracts.csproj", "src/Shared/Contracts/"]
RUN dotnet restore "src/Services/Resale/Resale.Api.csproj"
COPY . .
WORKDIR "/src/src/Services/Resale"
RUN dotnet publish "Resale.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
RUN useradd --uid 10001 --create-home --shell /usr/sbin/nologin appuser
COPY --from=build --chown=appuser:appuser /app/publish .
USER 10001
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Resale.Api.dll"]
