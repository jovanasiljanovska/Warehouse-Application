# 1. Use the .NET 8 SDK to build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 2. Copy everything from your GitHub root into the container
COPY . .

# 3. Restore dependencies specifically for the Web project
# This avoids the "Specify a project file" error
RUN dotnet restore "Warehouse.Web/Warehouse.Web.csproj"

# 4. Publish the project
RUN dotnet publish "Warehouse.Web/Warehouse.Web.csproj" -c Release -o /app/publish

# 5. Build the final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# 6. Start the app (Note: Ensure the DLL name matches your project output)
ENTRYPOINT ["dotnet", "Warehouse.Web.dll"]
