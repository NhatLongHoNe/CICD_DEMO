#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Directory.Build.props", "."]
COPY ["src/DemoCICD.API/DemoCICD.API.csproj", "src/DemoCICD.API/"]
COPY ["src/DemoCICD.Application/DemoCICD.Application.csproj", "src/DemoCICD.Application/"]
COPY ["src/DemoCICD.Contract/DemoCICD.Contract.csproj", "src/DemoCICD.Contract/"]
COPY ["src/DemoCICD.Domain/DemoCICD.Domain.csproj", "src/DemoCICD.Domain/"]
COPY ["src/DemoCICD.Persistence/DemoCICD.Persistence.csproj", "src/DemoCICD.Persistence/"]
COPY ["src/DemoCICD.Infrastructure/DemoCICD.Infrastructure.csproj", "src/DemoCICD.Infrastructure/"]
COPY ["src/DemoCICD.Presentation/DemoCICD.Presentation.csproj", "src/DemoCICD.Presentation/"]
RUN dotnet restore "src/DemoCICD.API/DemoCICD.API.csproj"
COPY . .
WORKDIR "/src/src/DemoCICD.API"
RUN dotnet build "DemoCICD.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DemoCICD.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DemoCICD.API.dll"]