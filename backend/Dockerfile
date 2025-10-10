# Use the official .NET 8 runtime as the base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Use the official .NET 8 SDK as the build image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["RecruiterApi.csproj", "."]
RUN dotnet restore "./RecruiterApi.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "RecruiterApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RecruiterApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RecruiterApi.dll"]
