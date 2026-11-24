FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY EventoApi/EventoApi.csproj EventoApi/
COPY Core/Core.csproj Core/
COPY Service/Service.csproj Service/
COPY Util/Util.csproj Util/
RUN dotnet restore EventoApi/EventoApi.csproj

COPY EventoApi/ EventoApi/
COPY Core/ Core/
COPY Service/ Service/
COPY Util/ Util/

RUN dotnet publish EventoApi/EventoApi.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EventoApi.dll"]
