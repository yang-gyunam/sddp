#!/usr/bin/env bash
# =============================================================================
# run-e2e-showcase.sh
# Headed browser showcase runner for the public E2E scenario
#
# The public repository supports only the OnBrick (S2) scenario.
# =============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
MAGENTA='\033[0;35m'
BOLD='\033[1m'
NC='\033[0m'

SHOWCASE_SLOW_MO="${SHOWCASE_SLOW_MO:-1500}"
SHOWCASE_DELAY_MS="${SHOWCASE_DELAY_MS:-3000}"
SHOWCASE_VIDEO="${SHOWCASE_VIDEO:-on}"
SHOWCASE_WINDOW_WIDTH="${SHOWCASE_WINDOW_WIDTH:-1440}"
SHOWCASE_WINDOW_HEIGHT="${SHOWCASE_WINDOW_HEIGHT:-900}"
E2E_API_BASE="${E2E_API_BASE:-http://localhost:5001/api}"

print_header() {
  echo ""
  echo -e "${MAGENTA}╔══════════════════════════════════════════════════════════╗${NC}"
  echo -e "${MAGENTA}║${NC}  ${BOLD}SDDP E2E Showcase - OnBrick${NC}                          ${MAGENTA}║${NC}"
  echo -e "${MAGENTA}╚══════════════════════════════════════════════════════════╝${NC}"
  echo ""
}

check_service() {
  local name=$1
  local url=$2
  local timeout=${3:-5}

  if curl -sf --max-time "$timeout" "$url" > /dev/null 2>&1; then
    echo -e "  ${GREEN}●${NC} $name - ${GREEN}Running${NC}"
    return 0
  else
    echo -e "  ${RED}○${NC} $name - ${RED}Not available${NC}"
    return 1
  fi
}

print_config() {
  echo -e "${BOLD}Configuration:${NC}"
  echo "  slowMo         = ${SHOWCASE_SLOW_MO}ms"
  echo "  sceneDelay     = ${SHOWCASE_DELAY_MS}ms"
  echo "  video          = ${SHOWCASE_VIDEO}"
  echo "  window         = ${SHOWCASE_WINDOW_WIDTH}x${SHOWCASE_WINDOW_HEIGHT}"
  echo "  scenario       = OnBrick (S2)"
  echo ""
}

print_header

if [ "${1:-}" != "" ] && [ "${1:-}" != "onbrick" ] && [ "${1:-}" != "s2" ] && [ "${1:-}" != "S2" ]; then
  echo -e "${YELLOW}Only the OnBrick showcase is available in the public repository.${NC}"
  echo "Usage: ./scripts/run-e2e-showcase.sh [onbrick]"
  exit 1
fi

echo -e "${BOLD}Service Health Check:${NC}"

API_OK=false
if check_service "Backend API" "${E2E_API_BASE%/api}/health"; then
  API_OK=true
fi

FRONTEND_OK=false
if check_service "Frontend" "http://localhost:3500" 3; then
  FRONTEND_OK=true
fi

MISSING_SERVICES=false
if [ "$API_OK" = false ]; then
  echo -e "${YELLOW}Backend API is not running.${NC}"
  echo "  docker compose up -d sddp-api"
  MISSING_SERVICES=true
fi

if [ "$FRONTEND_OK" = false ]; then
  echo -e "${YELLOW}Frontend development server is not running.${NC}"
  echo "  cd src/frontend/packages/web && npm run dev"
  MISSING_SERVICES=true
fi

if [ "$MISSING_SERVICES" = true ]; then
  echo ""
  echo -e "${RED}Required services are unavailable, so the showcase run cannot start.${NC}"
  exit 1
fi

echo ""
print_config

export SHOWCASE_SLOW_MO
export SHOWCASE_DELAY_MS
export SHOWCASE_VIDEO
export SHOWCASE_WINDOW_WIDTH
export SHOWCASE_WINDOW_HEIGHT
export E2E_API_BASE

cd "$PROJECT_DIR"
npx playwright test --config=playwright.showcase.config.ts S/S2-onbrick
