# The migrator image, the init container of the api app. The same base images
# and user as src/Api/Dockerfile; it listens on no port. The build context is
# the repository root.

FROM mcr.microsoft.com/dotnet/sdk:10.0.403-noble@sha256:de4fe90a98845d75910b9769662465c84de1a934665c431279d1382b2bddc02b AS build
WORKDIR /repo
COPY global.json ./
COPY src/Directory.Build.props src/Directory.Packages.props src/
COPY src/Api/Api.csproj src/Api/
COPY src/Migrator/Migrator.csproj src/Migrator/
RUN dotnet restore src/Migrator/Migrator.csproj
COPY src/Api/ src/Api/
COPY src/Migrator/ src/Migrator/
RUN dotnet publish src/Migrator/Migrator.csproj --configuration Release --no-restore --output /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0.14-noble-chiseled@sha256:c89a6a61f494f20a5ae302990e814627a8d91b332fd4a8e7c1707c2d4da9555d
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "Migrator.dll"]
