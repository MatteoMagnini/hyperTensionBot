#!/bin/bash

# Ottieni i segreti dalle variabili d'ambiente
TELEGRAM_TOKEN=${SECRET_TELEGRAM_TOKEN}
OPENAI_KEY=${SECRET_KEY_OPENAI}
MONGODB_CONNECTION=${MONGODB_CONNECTION_STRING}

# Verifica che i segreti siano stati trovati
if [[ -z "$TELEGRAM_TOKEN" || -z "$OPENAI_KEY" ]]; then
  echo "Errore: Impossibile trovare i segreti nelle variabili d'ambiente."
  exit 1
fi

# Definisci la struttura del file appsettings.json
cat <<EOF > appsettings.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "HyperTensionBot": "Information"
    }
  },
  "AllowedHosts": "*",
  "Bot": {
    "TelegramToken": "$TELEGRAM_TOKEN"
  },
  "OpenAI": {
    "OpenKey": "$OPENAI_KEY"
  },
  "MongoDB": {
    "connection": "$MONGODB_CONNECTION"
  },
  "Clusters": {
    "UrlLLM": "http://clusters.almaai.unibo.it:11434/api/chat"
  }
}
EOF

echo "Il file appsettings.json è stato generato correttamente."
