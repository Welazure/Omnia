FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Packages.props ./
COPY src/Omnia.Shared/Omnia.Shared.csproj src/Omnia.Shared/
COPY src/Omnia.Api/Omnia.Api.csproj src/Omnia.Api/
RUN dotnet restore src/Omnia.Api/Omnia.Api.csproj

COPY src/Omnia.Shared/ src/Omnia.Shared/
COPY src/Omnia.Api/ src/Omnia.Api/
RUN dotnet publish src/Omnia.Api/Omnia.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Omnia.Api.dll"]
