#!/bin/sh
# Starts Ollama, then pulls the models listed in OLLAMA_PULL_MODELS (space separated) in the background.
ollama serve &
PID=$!
sleep 3
for m in ${OLLAMA_PULL_MODELS:-bge-m3}; do
  echo "Pulling $m ..."
  ollama pull "$m" || echo "Could not pull $m"
done &
wait $PID
