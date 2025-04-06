# ----- STAGE 1: BUILD -----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore as separate layers
COPY ChatService/ChatService.csproj ./ChatService/
WORKDIR /src/ChatService
RUN dotnet restore

# Copy the rest of the source code
COPY ChatService/. ./

# Publish the application
RUN dotnet publish -c Release -o /app/publish

# ----- STAGE 2: RUNTIME -----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy the build output from the build stage
COPY --from=build /app/publish .

# Expose port (default Kestrel)
EXPOSE 80
ENV ASPNETCORE_URLS=http://+:80

# Run the application
ENTRYPOINT ["dotnet", "ChatService.dll"]
