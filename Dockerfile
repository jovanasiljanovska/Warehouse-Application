# 1. Change 8.0 to 10.0
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .

# 2. Restore and Publish using the 10.0 paths
RUN dotnet restore "Warehouse.Web/Warehouse.Web.csproj"
RUN dotnet publish "Warehouse.Web/Warehouse.Web.csproj" -c Release -o /app/publish

# 3. Change the runtime to 10.0 as well
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Warehouse.Web.dll"]
