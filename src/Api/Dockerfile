# The api image. Stage 1 publishes the API with the .NET SDK. Stage 2 is the
# chiseled ASP.NET runtime: no shell, no package manager, a non-root user, and
# the API on port 8080. Both base images are pinned by digest; Renovate bumps
# them from roadmap phase 12 on. The build context is the repository root.

FROM mcr.microsoft.com/dotnet/sdk:10.0.403-noble@sha256:de4fe90a98845d75910b9769662465c84de1a934665c431279d1382b2bddc02b AS build
WORKDIR /repo
# Restores first, from the project files only, so the layer is reused while
# the dependencies stay the same.
COPY global.json ./
COPY src/Directory.Build.props src/Directory.Packages.props src/
COPY src/Api/Api.csproj src/Api/
RUN dotnet restore src/Api/Api.csproj
COPY src/Api/ src/Api/
RUN dotnet publish src/Api/Api.csproj --configuration Release --no-restore --output /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0.14-noble-chiseled@sha256:c89a6a61f494f20a5ae302990e814627a8d91b332fd4a8e7c1707c2d4da9555d
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Api.dll"]
