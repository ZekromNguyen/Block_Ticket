FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Services/BlockchainOrchestrator/BlockchainOrchestrator.csproj", "src/Services/BlockchainOrchestrator/"]
COPY ["src/Shared/Common/Shared.Common.csproj", "src/Shared/Common/"]
COPY ["src/Shared/Contracts/Shared.Contracts.csproj", "src/Shared/Contracts/"]
RUN dotnet restore "src/Services/BlockchainOrchestrator/BlockchainOrchestrator.csproj"
COPY . .
WORKDIR "/src/src/Services/BlockchainOrchestrator"
RUN dotnet publish "BlockchainOrchestrator.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BlockchainOrchestrator.dll"]
