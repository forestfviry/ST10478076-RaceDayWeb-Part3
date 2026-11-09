# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first so this layer is cached until the .csproj changes
COPY RaceDayWeb.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish RaceDayWeb.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Plain http inside the container; the API address is supplied at run time
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    UseHttpsRedirection=false \
    Api__BaseUrl=http://host.docker.internal:49557/

EXPOSE 8080

COPY --from=build /app/publish ./

# Run as the non-root user that ships with the .NET 8 images
USER $APP_UID

ENTRYPOINT ["dotnet", "RaceDayWeb.dll"]
