FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Services/Verification/Verification.Api.csproj", "src/Services/Verification/"]
COPY ["src/Shared/Common/Shared.Common.csproj", "src/Shared/Common/"]
RUN dotnet restore "src/Services/Verification/Verification.Api.csproj"
COPY . .
WORKDIR "/src/src/Services/Verification"
RUN dotnet publish "Verification.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Verification.Api.dll"]
