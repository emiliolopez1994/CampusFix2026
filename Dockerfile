FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/CampusFix.Api/ backend/CampusFix.Api/
RUN dotnet restore backend/CampusFix.Api/CampusFix.Api.csproj
RUN dotnet publish backend/CampusFix.Api/CampusFix.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://0.0.0.0:10000
EXPOSE 10000
ENTRYPOINT ["dotnet", "CampusFix.Api.dll"]
