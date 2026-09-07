#!/usr/bin/env bash
# Sync the project to the server and (re)build + (re)start the production stack.
#   SERVER=ubuntu@18.195.74.135 SSH_KEY=~/.ssh/LightsailDefaultKey-eu-central-1.pem deploy/deploy.sh
# Re-run after every change; only changed files are transferred and only changed images are rebuilt.
set -euo pipefail
cd "$(dirname "$0")/.."

SERVER="${SERVER:-ubuntu@18.195.74.135}"
SSH_KEY="${SSH_KEY:-$HOME/.ssh/LightsailDefaultKey-eu-central-1.pem}"
REMOTE_DIR="${REMOTE_DIR:-/srv/aadi}"
SSH="ssh -i $SSH_KEY -o StrictHostKeyChecking=accept-new"

[ -f .env.production ] || { echo ".env.production is missing"; exit 1; }

echo "== rsync → $SERVER:$REMOTE_DIR"
rsync -az --delete -e "$SSH" \
  --exclude '.git' --exclude 'node_modules' --exclude '/src-frontend/dist' --exclude '/src-backend/*/bin' --exclude '/src-backend/*/obj' --exclude '.vs' --exclude '.venv' --exclude '__pycache__' \
  --exclude '.env' --exclude 'TestResults' --exclude '*.log' --exclude '*.pem' \
  ./ "$SERVER:$REMOTE_DIR/"

echo "== build + start"
$SSH "$SERVER" "cd $REMOTE_DIR && cp .env.production .env && chmod 600 .env \
  && docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build --remove-orphans \
  && docker image prune -f > /dev/null \
  && docker compose -f docker-compose.yml -f docker-compose.prod.yml ps"

echo "== smoke (waits up to 2 min for Caddy to hold certificates)"
for i in $(seq 1 24); do
  code=$(curl -s -o /dev/null -w "%{http_code}" --max-time 10 https://aialreadydidit.com/api/v1/site/config || true)
  [ "$code" = "200" ] && break
  sleep 5
done
curl -sS -o /dev/null -w "site      %{http_code}\n" https://aialreadydidit.com/ || true
curl -sS -o /dev/null -w "api       %{http_code}\n" https://aialreadydidit.com/api/v1/site/config || true
curl -sS -o /dev/null -w "files     %{http_code}\n" https://files.aialreadydidit.com/minio/health/live || true
curl -sS -o /dev/null -w "redirect  %{http_code} → %{redirect_url}\n" https://aialreadymadeit.com/ || true
