#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src

COPY ["not-a-google-drive-backend.csproj", "not-a-google-drive-backend/"]
COPY ["DatabaseModule.csproj", "not-a-google-drive-backend/"]

RUN dotnet restore "not-a-google-drive-backend/not-a-google-drive-backend.csproj"
RUN dotnet restore "not-a-google-drive-backend/DatabaseModule.csproj"

COPY [".", "not-a-google-drive-backend/"]

WORKDIR "/src/not-a-google-drive-backend"
RUN dotnet build "not-a-google-drive-backend.csproj" -c Release -o /app/build
RUN dotnet build "DatabaseModule.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "not-a-google-drive-backend.csproj" -c Release -o /app/publish
RUN dotnet publish "DatabaseModule.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
RUN mkdir -p /app/saved_files
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "not-a-google-drive-backend.dll"]