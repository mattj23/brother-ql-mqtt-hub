FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["BrotherQlHub/", "."]
RUN dotnet restore "BrotherQlHub.Server/BrotherQlHub.Server.csproj"
COPY . .
WORKDIR "/src/BrotherQlHub.Server"
RUN dotnet build "BrotherQlHub.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BrotherQlHub.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BrotherQlHub.Server.dll"]
