#!/bin/bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

configuration="Release"
output_dir="./artifacts/packages"
version=""
version_suffix=""
restore=true

usage() {
  cat <<'EOF'
Usage: ./pack-packages.sh [options]

Options:
  --configuration <name>   Build configuration. Defaults to Release.
  --output <path>          Package output directory. Defaults to ./artifacts/packages.
  --version <version>      Set the exact NuGet package version.
  --version-suffix <text>  Set the NuGet package version suffix.
  --no-restore            Skip dotnet restore before packing.
  -h, --help              Show this help.
EOF
}

while [ "$#" -gt 0 ]; do
  case "$1" in
    --configuration)
      configuration="${2:?Missing value for --configuration}"
      shift 2
      ;;
    --output)
      output_dir="${2:?Missing value for --output}"
      shift 2
      ;;
    --version)
      version="${2:?Missing value for --version}"
      shift 2
      ;;
    --version-suffix)
      version_suffix="${2:?Missing value for --version-suffix}"
      shift 2
      ;;
    --no-restore)
      restore=false
      shift
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      usage >&2
      exit 2
      ;;
  esac
done

if [ -n "${version}" ] && [ -n "${version_suffix}" ]; then
  echo "--version and --version-suffix are mutually exclusive." >&2
  exit 2
fi

package_projects=(
  "src/Pretext.Contracts/Pretext.Contracts.csproj"
  "src/Pretext/Pretext.csproj"
  "src/Pretext.Layout/Pretext.Layout.csproj"
  "src/Pretext.DirectWrite/Pretext.DirectWrite.csproj"
  "src/Pretext.FreeType/Pretext.FreeType.csproj"
  "src/Pretext.CoreText/Pretext.CoreText.csproj"
  "src/Pretext.SkiaSharp/Pretext.SkiaSharp.csproj"
  "src/Pretext.Uno/Pretext.Uno.csproj"
)

pack_properties=()
if [ -n "${version}" ]; then
  pack_properties+=("/p:Version=${version}")
elif [ -n "${version_suffix}" ]; then
  pack_properties+=("/p:VersionSuffix=${version_suffix}")
fi

cd "${repo_root}"
mkdir -p "${output_dir}"

if [ "${restore}" = true ]; then
  for project in "${package_projects[@]}"; do
    dotnet restore "${project}"
  done
fi

for project in "${package_projects[@]}"; do
  dotnet pack "${project}" \
    -c "${configuration}" \
    --no-restore \
    -o "${output_dir}" \
    "${pack_properties[@]}"
done
