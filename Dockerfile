# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy all .csproj files first to cache restore layers
COPY ["Warehouse.Web/Warehouse.Web.csproj", "Warehouse.Web/"]
COPY ["Warehouse.Service/Warehouse.Service.csproj", "Warehouse.Service/"]
COPY ["Warehouse.Repository/Warehouse.Repository.csproj", "Warehouse.Repository/"]
COPY ["Warehouse.Domain/Warehouse.Domain.csproj", "Warehouse.Domain/"]

RUN dotnet restore "Warehouse.Web/Warehouse.Web.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/Warehouse.Web"
RUN dotnet build "Warehouse.Web.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "Warehouse.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Final Runtime Image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Warehouse.Web.dll"]