FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Services/Notification/Notification.csproj", "src/Services/Notification/"]
COPY ["src/Shared/Contracts/Shared.Contracts.csproj", "src/Shared/Contracts/"]
RUN dotnet restore "src/Services/Notification/Notification.csproj"
COPY . .
WORKDIR "/src/src/Services/Notification"
RUN dotnet publish "Notification.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
RUN useradd --uid 10001 --create-home --shell /usr/sbin/nologin appuser
COPY --from=build --chown=appuser:appuser /app/publish .
USER 10001
ENTRYPOINT ["dotnet", "Notification.dll"]
