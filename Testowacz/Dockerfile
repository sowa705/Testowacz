FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Testowacz/Testowacz.csproj", "Testowacz/"]
RUN dotnet restore "Testowacz/Testowacz.csproj"
COPY . .
WORKDIR "/src/Testowacz"
RUN dotnet build "Testowacz.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Testowacz.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Testowacz.dll"]
