# ByteFmt disk usage explorer (web app)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/ByteFmt.Web -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
# Mount a host directory to scan, e.g. -v /data:/data:ro, then scan the path /data in the UI.
ENTRYPOINT ["dotnet", "ByteFmt.Web.dll"]
