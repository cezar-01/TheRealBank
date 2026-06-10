#!/usr/bin/env bash
# Script de deploy da TheRealBank numa VM EC2 (Amazon Linux 2023).
# Roda: app ASP.NET + Ollama via Docker Compose. Banco fica no Railway (.env).
#
# COMO USAR (dentro da VM, conectado por SSH):
#   1) git clone https://github.com/cezar-01/TheRealBank.git
#   2) cd TheRealBank
#   3) cp .env.example .env  &&  nano .env   # cole a connection string do Railway
#   4) bash deploy-aws-vm.sh
set -euo pipefail

echo ">> Instalando Docker e Git..."
sudo dnf update -y
sudo dnf install -y docker git

echo ">> Habilitando Docker..."
sudo systemctl enable --now docker

echo ">> Instalando o plugin docker compose..."
sudo mkdir -p /usr/local/lib/docker/cli-plugins
sudo curl -SL "https://github.com/docker/compose/releases/latest/download/docker-compose-linux-x86_64" \
  -o /usr/local/lib/docker/cli-plugins/docker-compose
sudo chmod +x /usr/local/lib/docker/cli-plugins/docker-compose

if [ ! -f .env ]; then
  echo "!! Arquivo .env nao encontrado. Rode: cp .env.example .env e preencha a senha do Railway." >&2
  exit 1
fi

echo ">> Subindo a stack (build do app + ollama)..."
sudo docker compose up -d --build

echo ">> Baixando o modelo de IA (phi3:mini)... pode levar 1-2 min."
sudo docker compose exec -T ollama ollama pull phi3:mini

echo ""
echo "==================================================================="
echo " Pronto! Acesse:  http://<IP-PUBLICO-DA-VM>/"
echo " Ver logs do app:  sudo docker compose logs -f app"
echo "==================================================================="
