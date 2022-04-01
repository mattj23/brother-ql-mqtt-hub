FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["BrotherQlHub/BrotherQlHub.csproj", "BrotherQlHub/"]
RUN dotnet restore "BrotherQlHub/BrotherQlHub.csproj"
COPY . .
WORKDIR "/src/BrotherQlHub"
RUN dotnet build "BrotherQlHub.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BrotherQlHub.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BrotherQlHub.dll"]
