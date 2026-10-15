# =========================================
# STAGE 1: BUILD ASP.NET CORE .NET 9
# =========================================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

COPY NhaKhoaHoang.API/NhaKhoaHoang.API.csproj NhaKhoaHoang.API/

RUN dotnet restore NhaKhoaHoang.API/NhaKhoaHoang.API.csproj

COPY NhaKhoaHoang.API/ NhaKhoaHoang.API/

RUN dotnet publish NhaKhoaHoang.API/NhaKhoaHoang.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# =========================================
# STAGE 2: RUN ASP.NET CORE
# =========================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:10000
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 10000

ENTRYPOINT ["dotnet", "NhaKhoaHoang.API.dll"]
