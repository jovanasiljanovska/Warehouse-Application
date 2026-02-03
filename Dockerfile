# 1. Use the .NET 8 SDK to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 2. Copy the project files and restore dependencies
COPY . .
RUN dotnet restore

# 3. Publish the app in Release mode
RUN dotnet publish -c Release -o /app/publish

# 4. Use the ASP.NET runtime image for the final container
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# 5. Tell the container to start your specific DLL
# Replace 'Warehouse.Web.dll' with your actual DLL name if different
ENTRYPOINT ["dotnet", "Warehouse.Web.dll"]