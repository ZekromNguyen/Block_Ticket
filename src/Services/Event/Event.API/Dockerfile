FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Services/Event/Event.API/Event.API.csproj", "src/Services/Event/Event.API/"]
COPY ["src/Services/Event/Event.Application/Event.Application.csproj", "src/Services/Event/Event.Application/"]
COPY ["src/Services/Event/Event.Domain/Event.Domain.csproj", "src/Services/Event/Event.Domain/"]
COPY ["src/Services/Event/Event.Infrastructure/Event.Infrastructure.csproj", "src/Services/Event/Event.Infrastructure/"]
COPY ["src/Shared/Common/Shared.Common.csproj", "src/Shared/Common/"]
COPY ["src/Shared/Contracts/Shared.Contracts.csproj", "src/Shared/Contracts/"]
RUN dotnet restore "src/Services/Event/Event.API/Event.API.csproj"
COPY . .
WORKDIR "/src/src/Services/Event/Event.API"
RUN dotnet publish "Event.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
RUN useradd --uid 10001 --create-home --shell /usr/sbin/nologin appuser
COPY --from=build --chown=appuser:appuser /app/publish .
USER 10001
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Event.API.dll"]
