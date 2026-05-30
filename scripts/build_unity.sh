#!/usr/bin/env bash
# 构建 CombatFramework net48 DLL ── 用于 Unity 项目
# 用法: ./scripts/build_unity.sh
# 输出: dist/CombatFramework.dll + dist/MoonSharp.dll

set -euo pipefail
PROJ_DIR="$(cd "$(dirname "$0")"/../src && pwd)"
OUT_DIR="$(cd "$(dirname "$0")"/.. && pwd)/dist"

echo "== CombatFramework Unity Build =="
echo "目标: net48 / Release"
echo ""

dotnet build "$PROJ_DIR/CombatFramework.csproj" \
    -f net48 \
    -c Release \
    -o "$OUT_DIR" \
    --nologo

echo ""
echo "== 完成 =="
echo "输出目录: $OUT_DIR"
ls -1 "$OUT_DIR/CombatFramework.dll" "$OUT_DIR/MoonSharp.Interpreter.dll" 2>/dev/null
echo ""
echo "将 dist/CombatFramework.dll 和 dist/MoonSharp.Interpreter.dll 复制到 Unity 的 Assets/Plugins/"
