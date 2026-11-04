FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json nuget.config Directory.Build.props Directory.Packages.props ./
COPY src/Starter.Api/Starter.Api.csproj src/Starter.Api/
RUN dotnet restore src/Starter.Api/Starter.Api.csproj

COPY src/ src/
RUN dotnet publish src/Starter.Api/Starter.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Starter.Api.dll"]
