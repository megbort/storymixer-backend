FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/StoryMixer.Api/StoryMixer.Api.csproj src/StoryMixer.Api/
RUN dotnet restore src/StoryMixer.Api/StoryMixer.Api.csproj
COPY src/ src/
RUN dotnet publish src/StoryMixer.Api/StoryMixer.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
# Railway injects PORT; fall back to 8080 locally
ENTRYPOINT ["/bin/sh", "-c", "ASPNETCORE_HTTP_PORTS=${PORT:-8080} exec dotnet StoryMixer.Api.dll"]
