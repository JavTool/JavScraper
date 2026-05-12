#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
tag_convert.py — 遍历指定目录下所有 NFO 文件，
将 <genre> 和 <tag> 节点的值按 tag_convert.json 的映射规则替换。

用法：
    python tag_convert.py <目录路径>
    python tag_convert.py "G:\JavScraper\tools\scrapers\Emby"
    python tag_convert.py . --dry-run       # 只预览，不写入
    python tag_convert.py . --log           # 将替换日志写入 tag_convert.log
"""

import os
import sys
import json
import shutil
import argparse
import logging
from datetime import datetime
from xml.etree import ElementTree as ET

# ---------------------------------------------------------------------------
# 常量
# ---------------------------------------------------------------------------

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
TAG_CONVERT_JSON = os.path.join(SCRIPT_DIR, "tag_convert.json")
TAG_NODES = ("genre", "tag")          # NFO 中需要替换的节点名


# ---------------------------------------------------------------------------
# 加载映射表
# ---------------------------------------------------------------------------

def load_mapping(path: str = None) -> dict:
    """
    从 tag_convert.json 加载映射表。
    key 统一转为小写以实现大小写不敏感匹配。
    """
    fpath = path or TAG_CONVERT_JSON
    if not os.path.exists(fpath):
        raise FileNotFoundError(f"tag_convert.json 未找到: {fpath}")
    with open(fpath, encoding="utf-8") as f:
        raw = json.load(f)
    return {k.lower().strip(): v for k, v in raw.items()}


# ---------------------------------------------------------------------------
# 单文件处理
# ---------------------------------------------------------------------------

def convert_nfo(nfo_path: str, mapping: dict,
                dry_run: bool = False, backup: bool = True):
    """
    处理单个 NFO 文件。

    返回: (changes, skip_reason)
        changes     : [(节点名, 原值, 新值), ...] 变更列表；无变更时为 []
        skip_reason : 跳过原因字符串；成功处理时为 ""
    """
    try:
        tree = ET.parse(nfo_path)
    except ET.ParseError as e:
        reason = f"XML 解析失败: {e}"
        logging.warning(f"[SKIP] {reason}: {nfo_path}")
        return [], reason

    root = tree.getroot()
    changes = []

    # -----------------------------------------------------------------------
    # 第一步：替换 <genre> 和 <tag> 的值，并去重
    # -----------------------------------------------------------------------
    # written 记录本次已写入的值，防止两个不同原始标签替换后碰撞
    written: set = set()

    to_remove = []
    for node in root:
        if node.tag not in TAG_NODES:
            continue
        original = (node.text or "").strip()
        converted = mapping.get(original.lower(), original)

        if converted != original:
            changes.append((node.tag, original, converted))

        if converted in written:
            to_remove.append(node)      # 替换后与已写入值重复，删除
        else:
            node.text = converted
            written.add(converted)

    for node in to_remove:
        root.remove(node)

    # -----------------------------------------------------------------------
    # 第二步：收集替换后所有 <genre> 的最终值
    # -----------------------------------------------------------------------
    final_genres = [
        (node.text or "").strip()
        for node in root
        if node.tag == "genre" and (node.text or "").strip()
    ]

    # -----------------------------------------------------------------------
    # 第三步：删除旧 <tag> 节点，按 <genre> 值重建 <tag> 节点
    # -----------------------------------------------------------------------
    old_tags = [node for node in root if node.tag == "tag"]
    old_tag_values = [(node.text or "").strip() for node in old_tags]

    # 判断 tag 是否需要重建：值集合与 genre 不同，或顺序不同
    needs_rebuild = (old_tag_values != final_genres)

    if needs_rebuild:
        # 记录新增/变更的 tag 变更日志
        for val in final_genres:
            if val not in old_tag_values:
                changes.append(("tag", "", val))

        # 删除所有旧 <tag>
        for node in old_tags:
            root.remove(node)

        # 在最后一个 <genre> 之后插入新 <tag> 节点（保持 XML 整洁）
        genre_indices = [i for i, n in enumerate(list(root)) if n.tag == "genre"]
        insert_pos = (genre_indices[-1] + 1) if genre_indices else len(list(root))

        for offset, val in enumerate(final_genres):
            tag_node = ET.Element("tag")
            tag_node.text = val
            root.insert(insert_pos + offset, tag_node)

    # 无任何变更：分辨跳过原因
    if not changes and not needs_rebuild:
        if not final_genres:
            skip_reason = "NFO 中无 <genre> 节点"
        else:
            skip_reason = f"<genre>/<tag> 均已是目标值，无需替换"
        return [], skip_reason

    if dry_run:
        return changes, ""

    # 备份原文件
    if backup:
        shutil.copy2(nfo_path, nfo_path + ".bak")

    # 写回文件，保留 XML 声明和 utf-8 编码
    ET.indent(root, space="  ")
    tree.write(nfo_path, encoding="utf-8", xml_declaration=True)

    return changes, ""


# ---------------------------------------------------------------------------
# 目录遍历
# ---------------------------------------------------------------------------

def convert_directory(root_dir: str, mapping: dict,
                      dry_run: bool = False,
                      backup: bool = True,
                      log_path: str = None) -> dict:
    """
    递归遍历目录，处理所有 .nfo 文件。

    返回统计字典:
        {
            "total":    处理的 NFO 文件总数,
            "changed":  发生替换的文件数,
            "skipped":  跳过（无变更或解析失败）的文件数,
            "details":  {nfo_path: [(tag, old, new), ...]}
        }
    """
    stats = {"total": 0, "changed": 0, "skipped": 0, "details": {}}
    ok_lines = []
    skip_lines = []

    for dirpath, _, filenames in os.walk(root_dir):
        for fname in filenames:
            if not fname.lower().endswith(".nfo"):
                continue
            nfo_path = os.path.join(dirpath, fname)
            stats["total"] += 1

            changes, skip_reason = convert_nfo(nfo_path, mapping,
                                               dry_run=dry_run, backup=backup)
            if changes:
                stats["changed"] += 1
                stats["details"][nfo_path] = changes
                label = "[DRY]" if dry_run else "[OK] "
                for tag, old, new in changes:
                    line = f"{label} {nfo_path}  <{tag}> {old!r} -> {new!r}"
                    print(line)
                    ok_lines.append(line)
            else:
                stats["skipped"] += 1
                reason_str = skip_reason or "无变更"
                skip_line = f"[SKIP] {nfo_path}  # {reason_str}"
                print(skip_line)
                skip_lines.append(skip_line)

    # 写日志
    if log_path:
        ts = datetime.now().strftime('%Y-%m-%d %H:%M:%S')
        header = f"\n# {ts} root={root_dir} dry_run={dry_run}\n"
        with open(log_path, "a", encoding="utf-8") as f:
            f.write(header)
            if ok_lines:
                f.write("\n".join(ok_lines) + "\n")
            if skip_lines:
                f.write("\n".join(skip_lines) + "\n")

    return stats


# ---------------------------------------------------------------------------
# CLI
# ---------------------------------------------------------------------------

def main():
    # ===== 手动赋值区（直接运行时在此修改）=====
    MANUAL_DIR = ""          # 要处理的目录，留空则使用命令行参数
    MANUAL_DRY_RUN = False   # True = 只预览不写入
    MANUAL_BACKUP = True     # True = 写入前备份为 .bak
    MANUAL_LOG = True        # True = 写日志到 tag_convert.log
    # =============================================

    parser = argparse.ArgumentParser(
        description="遍历目录下所有 NFO 文件，按 tag_convert.json 替换 <genre>/<tag> 标签值"
    )
    parser.add_argument(
        "directory",
        nargs="?",
        default="",
        help="要处理的目录路径（可选，留空时使用脚本内 MANUAL_DIR）"
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        default=False,
        help="预览模式：只打印变更，不写入文件"
    )
    parser.add_argument(
        "--no-backup",
        action="store_true",
        default=False,
        help="写入前不创建 .bak 备份（默认会备份）"
    )
    parser.add_argument(
        "--log",
        action="store_true",
        default=False,
        help="将替换日志追加写入 tag_convert.log"
    )
    parser.add_argument(
        "--mapping",
        default=None,
        help=f"自定义映射 JSON 路径（默认: {TAG_CONVERT_JSON}）"
    )

    args = parser.parse_args()

    # 目录优先级：手动赋值 > 命令行参数
    target_dir = MANUAL_DIR.strip() if MANUAL_DIR.strip() else args.directory.strip()
    if not target_dir:
        parser.print_help()
        print("\n[ERROR] 请指定目录路径（命令行参数或修改脚本内 MANUAL_DIR）")
        sys.exit(1)

    if not os.path.isdir(target_dir):
        print(f"[ERROR] 目录不存在: {target_dir}")
        sys.exit(1)

    dry_run  = MANUAL_DRY_RUN or args.dry_run
    backup   = MANUAL_BACKUP and not args.no_backup
    use_log  = MANUAL_LOG or args.log
    log_path = os.path.join(SCRIPT_DIR, "tag_convert.log") if use_log else None

    # 加载映射
    try:
        mapping = load_mapping(args.mapping)
    except FileNotFoundError as e:
        print(f"[ERROR] {e}")
        sys.exit(1)

    print(f"[INFO] 映射条目: {len(mapping)} 条")
    print(f"[INFO] 目标目录: {target_dir}")
    print(f"[INFO] 模式: {'预览（不写入）' if dry_run else '写入'}"
          f" | 备份: {'是' if backup else '否'}"
          f" | 日志: {log_path or '否'}")
    print("-" * 60)

    stats = convert_directory(
        target_dir, mapping,
        dry_run=dry_run,
        backup=backup,
        log_path=log_path,
    )

    print("-" * 60)
    print(f"[完成] 扫描 {stats['total']} 个 NFO 文件  |  "
          f"替换 {stats['changed']} 个  |  "
          f"跳过 {stats['skipped']} 个")
    if dry_run:
        print("[INFO] 预览模式，文件未被修改")
    if log_path and stats["changed"]:
        print(f"[INFO] 日志已写入: {log_path}")


if __name__ == "__main__":
    main()
