#!/bin/sh
# Host Ollama for the AADI stack: models on the sdx1 store, reachable from containers via host.docker.internal.
export OLLAMA_MODELS=/mnt/sdx1/local_llms/ollama/.ollama/models
export OLLAMA_HOST=0.0.0.0:11434
export OLLAMA_KEEP_ALIVE=30m
exec ollama serve
