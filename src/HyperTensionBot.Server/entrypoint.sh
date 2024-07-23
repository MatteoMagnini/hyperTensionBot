#!/bin/bash

# Start MongoDB
mongod --bind_ip 0.0.0.0 --dbpath /data/db &

# Start ngrok
ngrok start --all &

# Wait for ngrok to start
sleep 10

# Update the webhook
/update_webhook.sh

# Start .NET
dotnet HyperTensionBot.Server.dll

echo "HyperTensionBot.Server started"
