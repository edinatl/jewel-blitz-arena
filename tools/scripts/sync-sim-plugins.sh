#!/usr/bin/env bash
# Build Sim/Data/PresentationBridge/Meta/Net and copy netstandard2.1 DLLs into the Unity host Plugins folder.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
OUT="${ROOT}/unity/JewelBlitzArena/Assets/Plugins/Sim"
CFG="${1:-Release}"

mkdir -p "${OUT}"

dotnet build "${ROOT}/src/JewelBlitzArena.Data/JewelBlitzArena.Data.csproj" -c "${CFG}" --nologo
dotnet build "${ROOT}/src/JewelBlitzArena.Sim/JewelBlitzArena.Sim.csproj" -c "${CFG}" --nologo
dotnet build "${ROOT}/src/JewelBlitzArena.Meta/JewelBlitzArena.Meta.csproj" -c "${CFG}" --nologo
dotnet build "${ROOT}/src/JewelBlitzArena.Net/JewelBlitzArena.Net.csproj" -c "${CFG}" --nologo
dotnet build "${ROOT}/src/JewelBlitzArena.PresentationBridge/JewelBlitzArena.PresentationBridge.csproj" -c "${CFG}" --nologo

DATA_OUT="${ROOT}/src/JewelBlitzArena.Data/bin/${CFG}/netstandard2.1"
SIM_OUT="${ROOT}/src/JewelBlitzArena.Sim/bin/${CFG}/netstandard2.1"
META_OUT="${ROOT}/src/JewelBlitzArena.Meta/bin/${CFG}/netstandard2.1"
NET_OUT="${ROOT}/src/JewelBlitzArena.Net/bin/${CFG}/netstandard2.1"
BRIDGE_OUT="${ROOT}/src/JewelBlitzArena.PresentationBridge/bin/${CFG}/netstandard2.1"

copy_dll() {
  local src="$1"
  if [[ ! -f "${src}" ]]; then
    echo "missing: ${src}" >&2
    exit 1
  fi
  cp -f "${src}" "${OUT}/"
  echo "  $(basename "${src}")"
}

echo "Syncing plugins → ${OUT}"
copy_dll "${DATA_OUT}/JewelBlitzArena.Data.dll"
copy_dll "${SIM_OUT}/JewelBlitzArena.Sim.dll"
copy_dll "${META_OUT}/JewelBlitzArena.Meta.dll"
copy_dll "${NET_OUT}/JewelBlitzArena.Net.dll"
copy_dll "${BRIDGE_OUT}/JewelBlitzArena.PresentationBridge.dll"

# Data depends on System.Text.Json (authoring/export). Ship the runtime assembly for Unity.
for dep in System.Text.Json System.Text.Encodings.Web System.IO.Pipelines Microsoft.Bcl.AsyncInterfaces; do
  found="$(find "${DATA_OUT}" -maxdepth 1 -name "${dep}.dll" 2>/dev/null | head -n 1 || true)"
  if [[ -z "${found}" ]]; then
    found="$(find "${HOME}/.nuget/packages" -iname "${dep}.dll" 2>/dev/null | head -n 1 || true)"
  fi
  if [[ -n "${found}" ]]; then
    copy_dll "${found}"
  else
    echo "  (skip optional dep ${dep} — not found)"
  fi
done

CONTENT_DST="${ROOT}/unity/JewelBlitzArena/Assets/StreamingAssets"
mkdir -p "${CONTENT_DST}"
if [[ -f "${ROOT}/content/export/s5_stub.jba" ]]; then
  cp -f "${ROOT}/content/export/s5_stub.jba" "${CONTENT_DST}/s5_stub.jba"
  echo "  StreamingAssets/s5_stub.jba"
fi

echo "Done."
