# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source
EXPOSE 8080

# copy everything else and build app
COPY . ./group_finder/
WORKDIR /source/group_finder
RUN dotnet publish -c release -o /app

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
VOLUME /app/dpkeys
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "group-finder.dll"]