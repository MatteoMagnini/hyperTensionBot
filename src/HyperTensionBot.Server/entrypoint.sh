#!/bin/bash

# Attendi che MongoDB sia avviato
until nc -z -v -w30 mongodb 27017
do
  echo "Waiting for MongoDB to start..."
  sleep 1
done

# Mantieni il container in esecuzione
tail -f /dev/null
