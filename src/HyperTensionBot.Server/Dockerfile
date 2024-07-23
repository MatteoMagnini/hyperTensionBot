FROM debian:bullseye-slim
WORKDIR /app
EXPOSE 80
EXPOSE 4040
EXPOSE 5183
EXPOSE 27017

# Install MongoDB
RUN apt-get update && apt-get install -y \
    gnupg \
    wget \
    curl \
    && wget -qO - https://www.mongodb.org/static/pgp/server-5.0.asc | apt-key add - \
    && echo "deb [ arch=amd64 ] https://repo.mongodb.org/apt/debian bullseye 10gen" | tee /etc/apt/sources.list.d/mongodb-org-5.0.list \
    && apt-get update \
    && apt-get install -y mongodb-org \
    && rm -rf /var/lib/apt/lists/*

# Install ngrok
RUN curl -s https://ngrok.com/download | tar xz -C /usr/local/bin
COPY ngrok.yml /root/.ngrok2/ngrok.yml

# Install .NET SDK
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["HyperTensionBot.Server.csproj", "."]
RUN dotnet restore "HyperTensionBot.Server.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "HyperTensionBot.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "HyperTensionBot.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=build /src/ModelML/trainingData.tsv /app/ModelML/trainingData.tsv

COPY update_webhook.sh /update_webhook.sh
RUN chmod +x /update_webhook.sh
COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh
ENTRYPOINT ["/entrypoint.sh"]
