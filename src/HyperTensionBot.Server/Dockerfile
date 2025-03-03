FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 4040
EXPOSE 5183
EXPOSE 27017

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["HyperTensionBot.Server.csproj", "."]
RUN dotnet restore "HyperTensionBot.Server.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "HyperTensionBot.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "HyperTensionBot.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=build /src/ModelML/trainingData.tsv /app/ModelML/trainingData.tsv

RUN apt-get update && apt-get install -y fonts-dejavu gettext

COPY appsettings.json /app/appsettings.json

RUN envsubst < /app/appsettings.json > /app/appsettings.replaced.json

RUN cat /app/appsettings.replaced.json

RUN mv /app/appsettings.replaced.json /app/appsettings.json

ENTRYPOINT ["dotnet", "HyperTensionBot.Server.dll"]
