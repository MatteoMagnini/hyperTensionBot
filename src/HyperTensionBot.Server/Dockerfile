FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["HyperTensionBot.Server.csproj", "."]
RUN dotnet restore "HyperTensionBot.Server.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "HyperTensionBot.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "HyperTensionBot.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=build /src/ModelML/trainingData.tsv /app/ModelML/trainingData.tsv
ENTRYPOINT ["dotnet", "HyperTensionBot.Server.dll"]
COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh
CMD ["/entrypoint.sh"]
