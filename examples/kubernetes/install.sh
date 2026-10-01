#!/bin/bash
# Installs the minimal setup from README.md: Agones with one Fleet, PostgreSQL
# through CloudNativePG, a development Keycloak, and Open Tournament. Safe to
# re-run; every step installs or upgrades.
set -euo pipefail
cd "$(dirname "$0")"

OPENTOURNAMENT_VERSION=${OPENTOURNAMENT_VERSION:-0.0.4}
NAMESPACE=opentournament

# Requires the opentournament-game-server:dev image in the cluster; see README.md.
echo "==> Agones"
helm upgrade --install agones agones --repo https://agones.dev/chart/stable --version 1.61.0 \
  -n agones-system --create-namespace --wait
kubectl apply -f fleet.yaml

echo "==> PostgreSQL"
helm upgrade --install cnpg cloudnative-pg --repo https://cloudnative-pg.github.io/charts --version 0.29.1 \
  -n cnpg-system --create-namespace --wait
kubectl create namespace "$NAMESPACE" --dry-run=client -o yaml | kubectl apply -f -
helm upgrade --install postgres cluster --repo https://cloudnative-pg.github.io/charts --version 0.8.1 \
  -n "$NAMESPACE" -f postgres-values.yaml

echo "==> Keycloak"
kubectl -n "$NAMESPACE" create configmap games-realm --from-file=realm.json --dry-run=client -o yaml |
  kubectl apply -f -
helm upgrade --install keycloak keycloakx --repo https://codecentric.github.io/helm-charts --version 7.3.2 \
  -n "$NAMESPACE" -f keycloak-values.yaml --wait --timeout 5m

echo "==> Open Tournament"
# The service migrates the database and discovers the Keycloak realm at startup.
kubectl -n "$NAMESPACE" wait cluster.postgresql.cnpg.io/postgres-cluster --for=condition=Ready --timeout=5m
helm upgrade --install ot oci://ghcr.io/couchpartygames/charts/opentournament --version "$OPENTOURNAMENT_VERSION" \
  -n "$NAMESPACE" -f opentournament-values.yaml --wait --timeout 5m

echo
echo "Done. Try it with the commands under \"Try it\" in README.md."
