#!/usr/bin/env python3
"""
CombatFramework 文档关键词检索

零外部依赖，仅用 Python 3 标准库。
构建倒排索引 → 关键词匹配 → 排序输出。

用法：
    python scripts/search_docs.py <关键词>
    python scripts/search_docs.py <关键词1> <关键词2>  # 同时匹配多个词
"""

import re
import sys
import os
import math
from pathlib import Path
from collections import defaultdict

DOCS_DIR = Path(__file__).resolve().parent.parent / "docs"


def tokenize(text: str) -> list[str]:
    """分词：小写 + 按非字母数字拆分"""
    return re.findall(r"[a-zA-Z一-鿿0-9_]+", text.lower())


def build_index():
    """
    构建倒排索引。
    Returns:
        index: word -> {file_path: {"freq": int, "positions": list[int]}}
        files: file_path -> {"path": Path, "title": str, "size": int}
    """
    index: dict[str, dict[str, dict]] = defaultdict(lambda: defaultdict(lambda: {"freq": 0, "positions": []}))
    files: dict[str, dict] = {}

    md_files = sorted(DOCS_DIR.rglob("*.md"))
    for fp in md_files:
        rel = str(fp.relative_to(DOCS_DIR.parent)).replace("\\", "/")
        text = fp.read_text(encoding="utf-8")
        tokens = tokenize(text)
        # 提取第一个 # 作为标题
        title_match = re.search(r"^#\s+(.+)", text, re.MULTILINE)
        title = title_match.group(1).strip() if title_match else fp.stem

        files[rel] = {"path": rel, "title": title, "size": len(text)}

        for pos, word in enumerate(tokens):
            entry = index[word][rel]
            entry["freq"] += 1
            entry["positions"].append(pos)

    return index, files


def search(query: str, index: dict, files: dict, top_k: int = 10):
    """
    关键词检索，TF-IDF 排序。
    """
    query_tokens = tokenize(query)
    if not query_tokens:
        return []

    # 计算每个文档的得分
    scores: dict[str, float] = defaultdict(float)
    match_details: dict[str, list[str]] = defaultdict(list)

    for qt in query_tokens:
        if qt not in index:
            continue

        # 该词的文档频率（IDF）
        df = len(index[qt])
        idf = math.log((len(files) + 1) / (df + 1)) + 1

        for rel_path, entry in index[qt].items():
            tf = 1 + math.log(entry["freq"])  # log TF
            scores[rel_path] += tf * idf

            # 找片段上下文
            text = (DOCS_DIR.parent / rel_path).read_text(encoding="utf-8")
            lines = text.split("\n")
            for pos in entry["positions"][:3]:  # 最多取 3 个位置
                snippet = _find_snippet(lines, qt, context_lines=2)
                if snippet and snippet not in match_details[rel_path]:
                    match_details[rel_path].append(snippet)

    # 排序
    ranked = sorted(scores.items(), key=lambda x: -x[1])
    results = []
    for rel_path, score in ranked[:top_k]:
        results.append({
            "path": rel_path,
            "title": files[rel_path]["title"],
            "score": round(score, 2),
            "snippets": match_details.get(rel_path, [])[:3],
        })
    return results


def _find_snippet(lines: list[str], keyword: str, context_lines: int = 2) -> str:
    """从行列表中找包含关键词的行及其上下文"""
    keyword_lower = keyword.lower()
    for i, line in enumerate(lines):
        if keyword_lower in line.lower():
            start = max(0, i - context_lines)
            end = min(len(lines), i + context_lines + 1)
            ctx = lines[start:end]
            snippet_lines = []
            for j, cl in enumerate(ctx):
                lineno = start + j + 1
                marker = ">" if (start + j) == i else " "
                snippet_lines.append(f"  {marker} L{lineno:4d} {cl.strip()}")
            return "\n".join(snippet_lines)
    return ""


def format_results(results: list[dict]):
    """格式化输出"""
    if not results:
        print("没有匹配结果。")
        return

    print(f"\n找到 {len(results)} 个匹配结果：\n")
    for r in results:
        print(f"  [{r['score']:6.2f}] {r['path']}")
        print(f"        ↳ {r['title']}")
        for snippet in r["snippets"]:
            print(snippet)
            print()
    print(f"\n--- 共 {len(results)} 条 ---\n")


def main():
    if len(sys.argv) < 2:
        print(f"用法: python {sys.argv[0]} <关键词> [关键词2 ...]")
        print(f"示例: python {sys.argv[0]} ModifierHookType")
        print(f"      python {sys.argv[0]} 伤害 抗性")
        sys.exit(1)

    if not DOCS_DIR.exists():
        print(f"错误: docs 目录不存在 ({DOCS_DIR})")
        print("请从项目根目录运行: python scripts/search_docs.py <关键词>")
        sys.exit(1)

    query = " ".join(sys.argv[1:])
    print(f"正在检索: {query}")
    index, files = build_index()
    results = search(query, index, files)
    format_results(results)


if __name__ == "__main__":
    main()
