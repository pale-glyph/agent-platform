#!/usr/bin/env bash

set -Eeuo pipefail

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
cd "$script_dir"

if [[ ! -f .env ]]; then
  printf 'Missing .env. Create it from example.env before starting the stack.\n' >&2
  exit 1
fi

docker compose --env-file .env build
docker compose --env-file .env up --remove-orphans "$@" --detach
