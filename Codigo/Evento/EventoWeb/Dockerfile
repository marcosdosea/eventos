FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY EventoWeb/EventoWeb.csproj EventoWeb/
COPY Core/Core.csproj Core/
COPY Service/Service.csproj Service/
COPY Util/Util.csproj Util/
RUN dotnet restore EventoWeb/EventoWeb.csproj

COPY EventoWeb/ EventoWeb/
COPY Core/ Core/
COPY Service/ Service/
COPY Util/ Util/

RUN dotnet publish EventoWeb/EventoWeb.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EventoWeb.dll"]
