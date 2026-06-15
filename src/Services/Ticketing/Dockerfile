FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Services/Ticketing/Ticketing.Api.csproj", "src/Services/Ticketing/"]
COPY ["src/Services/Ticketing/Ticketing.Application/Ticketing.Application.csproj", "src/Services/Ticketing/Ticketing.Application/"]
COPY ["src/Services/Ticketing/Ticketing.Domain/Ticketing.Domain.csproj", "src/Services/Ticketing/Ticketing.Domain/"]
COPY ["src/Services/Ticketing/Ticketing.Infrastructure/Ticketing.Infrastructure.csproj", "src/Services/Ticketing/Ticketing.Infrastructure/"]
COPY ["src/Shared/Common/Shared.Common.csproj", "src/Shared/Common/"]
COPY ["src/Shared/Contracts/Shared.Contracts.csproj", "src/Shared/Contracts/"]
RUN dotnet restore "src/Services/Ticketing/Ticketing.Api.csproj"
COPY . .
WORKDIR "/src/src/Services/Ticketing"
RUN dotnet publish "Ticketing.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Ticketing.Api.dll"]
