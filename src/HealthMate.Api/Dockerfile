FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY HealthMate.sln ./
COPY src/HealthMate.Api/HealthMate.Api.csproj src/HealthMate.Api/
COPY src/HealthMate.Application/HealthMate.Application.csproj src/HealthMate.Application/
COPY src/HealthMate.Infrastructure/HealthMate.Infrastructure.csproj src/HealthMate.Infrastructure/
COPY src/HealthMate.Sina/HealthMate.Sina.csproj src/HealthMate.Sina/
COPY tests/HealthMate.Tests/HealthMate.Tests.csproj tests/HealthMate.Tests/
RUN dotnet restore HealthMate.sln

COPY . .
RUN dotnet publish src/HealthMate.Api/HealthMate.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "HealthMate.Api.dll"]
