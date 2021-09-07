FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build-env

# Copy csproj and restore as distinct layers
COPY BrotherQlMqttHub/ /app/BrotherQlMqttHub/
WORKDIR /app/BrotherQlMqttHub
RUN dotnet restore

# Copy everything else and build
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:5.0
WORKDIR /app
COPY --from=build-env /app/BrotherQlMqttHub/out .
ENTRYPOINT ["dotnet", "BrotherQlMqttHub.dll"]
