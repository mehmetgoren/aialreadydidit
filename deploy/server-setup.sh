#!/usr/bin/env bash
# One-time preparation of a fresh Ubuntu 24.04 host (AWS Lightsail). Run as the default "ubuntu" user:
#   ssh -i ~/.ssh/LightsailDefaultKey-eu-central-1.pem ubuntu@18.195.74.135 'bash -s' < deploy/server-setup.sh
set -euo pipefail

echo "== packages"
sudo apt-get update -q
sudo DEBIAN_FRONTEND=noninteractive apt-get install -y -q ca-certificates curl gnupg rsync unattended-upgrades

echo "== docker engine + compose plugin (official repository)"
sudo install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg --yes
sudo chmod a+r /etc/apt/keyrings/docker.gpg
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
sudo apt-get update -q
sudo DEBIAN_FRONTEND=noninteractive apt-get install -y -q docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
sudo usermod -aG docker "$USER"
sudo systemctl enable --now docker

echo "== 4 GB swap (headroom for ClamAV + embedding model spikes)"
if ! swapon --show | grep -q /swapfile; then
  sudo fallocate -l 4G /swapfile && sudo chmod 600 /swapfile && sudo mkswap /swapfile && sudo swapon /swapfile
  echo '/swapfile none swap sw 0 0' | sudo tee -a /etc/fstab > /dev/null
fi

echo "== app directory"
sudo mkdir -p /srv/aadi && sudo chown "$USER":"$USER" /srv/aadi
sudo timedatectl set-timezone UTC

echo "== done. Log out and back in once so the docker group applies, then run deploy/deploy.sh from your workstation."
docker --version; docker compose version
