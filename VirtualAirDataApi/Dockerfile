#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5020

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["VirtualAirDataApi/VirtualAirDataApi.csproj", "VirtualAirDataApi/"]
RUN dotnet restore "./VirtualAirDataApi/VirtualAirDataApi.csproj"

COPY . .

WORKDIR "/src/VirtualAirDataApi"
RUN dotnet build "./VirtualAirDataApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./VirtualAirDataApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "VirtualAirDataApi.dll"]