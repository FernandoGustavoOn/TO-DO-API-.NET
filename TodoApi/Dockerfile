# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy csproj and restore as distinct layers
COPY TodoApi.csproj ./
RUN dotnet restore "TodoApi.csproj"

# copy the rest of the source code
COPY . .
RUN dotnet publish "TodoApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

# default port for local run
EXPOSE 8080

ENTRYPOINT ["dotnet", "TodoApi.dll"]


