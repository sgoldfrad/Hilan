# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/LeaveManagement.Api/LeaveManagement.Api.csproj src/LeaveManagement.Api/
# Cache dependencies first for faster rebuilds.
RUN dotnet restore src/LeaveManagement.Api/LeaveManagement.Api.csproj
COPY src ./src
RUN dotnet publish src/LeaveManagement.Api/LeaveManagement.Api.csproj -c Release -o /app/publish

# --- Run stage ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5080
ENTRYPOINT ["dotnet", "LeaveManagement.Api.dll"]
