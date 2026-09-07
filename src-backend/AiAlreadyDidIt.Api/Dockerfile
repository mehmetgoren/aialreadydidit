FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["AiAlreadyDidIt.Api/AiAlreadyDidIt.Api.csproj", "AiAlreadyDidIt.Api/"]
RUN dotnet restore "AiAlreadyDidIt.Api/AiAlreadyDidIt.Api.csproj"
COPY . .
WORKDIR "/src/AiAlreadyDidIt.Api"
RUN dotnet build "./AiAlreadyDidIt.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./AiAlreadyDidIt.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
USER root
RUN mkdir -p /app/Files/tmp && chown -R $APP_UID:$APP_UID /app/Files
USER $APP_UID
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AiAlreadyDidIt.Api.dll"]
