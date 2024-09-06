require 'json'

# Ottieni i segreti dalle variabili d'ambiente
telegram_token = ENV['SECRET_TELEGRAM_TOKEN']
openai_key = ENV['SECRET_KEY_OPENAI']

# Verifica che i segreti siano stati trovati
if telegram_token.nil? || openai_key.nil?
  puts "Errore: Impossibile trovare i segreti nelle variabili d'ambiente."
  exit 1
end

# Definisci la struttura del file appsettings.json
appsettings = {
  "Logging" => {
    "LogLevel" => {
      "Default" => "Information",
      "Microsoft.AspNetCore" => "Warning",
      "HyperTensionBot" => "Information"
    }
  },
  "AllowedHosts" => "*",
  "Bot" => {
    "TelegramToken" => telegram_token
  },
  "OpenAI" => {
    "OpenKey" => openai_key
  },
  "MongoDB" => {
    "connection" => "mongodb://mongodb:27017"
  },
  "Clusters" => {
    "UrlLLM" => "http://clusters.almaai.unibo.it:11434/api/chat"
  }
}

# Scrivi il contenuto nel file appsettings.json
File.open('appsettings.json', 'w') do |file|
  file.write(JSON.pretty_generate(appsettings))
end

puts "Il file appsettings.json è stato generato correttamente."
