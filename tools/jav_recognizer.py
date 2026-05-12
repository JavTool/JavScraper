#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
jav_recognizer.py — 媒体文件整理工具

功能：
  1. 识别文件名中的番号前缀（方括号内）、发布日期、剩余标题，
     生成统一格式的番号（例如 X-Art.16.07.24）和新文件名。
  2. 将同一组文件（同主干名的视频/图片/NFO）重命名后移动到
     与媒体文件同名的子目录下。

支持的文件名日期格式：
  - 2016-07-24          → 16.07.24
  - 26.12.2016          → 16.12.26
  - Jul 27, 2016        → 16.07.27
  - 2016.07.24          → 16.07.24

用法：
  python jav_recognizer.py <目标目录> [--dry-run] [--verbose]
"""

import os
import re
import sys
import shutil
import argparse
import logging
from dataclasses import dataclass, field
from typing import Optional

# ---------------------------------------------------------------------------
# 月份名称映射
# ---------------------------------------------------------------------------
MONTH_ABBR = {
    "jan": "01", "feb": "02", "mar": "03", "apr": "04",
    "may": "05", "jun": "06", "jul": "07", "aug": "08",
    "sep": "09", "oct": "10", "nov": "11", "dec": "12",
    "january": "01", "february": "02", "march": "03", "april": "04",
    "june": "06", "july": "07", "august": "08", "september": "09",
    "october": "10", "november": "11", "december": "12",
}

# ---------------------------------------------------------------------------
# 日期解析
# ---------------------------------------------------------------------------

# 各类日期正则，按优先级排列
_DATE_PATTERNS = [
    # 2016-07-24 / 2016.07.24
    (re.compile(r'\b(\d{4})[-.](\d{2})[-.](\d{2})\b'), 'ymd'),
    # 26.12.2016  （日.月.年，年份4位在末尾）
    (re.compile(r'\b(\d{1,2})\.(\d{2})\.(\d{4})\b'), 'dmy'),
    # Jul 27, 2016  /  July 27, 2016
    (re.compile(r'\b([A-Za-z]{3,9})\s+(\d{1,2}),?\s+(\d{4})\b'), 'mnd'),
    # 27 Jul 2016
    (re.compile(r'\b(\d{1,2})\s+([A-Za-z]{3,9})\s+(\d{4})\b'), 'dmn'),
]


def parse_date(raw: str) -> Optional[tuple[str, str, str]]:
    """
    从字符串中提取日期，返回 (YY, MM, DD) 元组，匹配不到返回 None。
    """
    for pat, fmt in _DATE_PATTERNS:
        m = pat.search(raw)
        if not m:
            continue
        try:
            if fmt == 'ymd':
                y, mo, d = m.group(1), m.group(2), m.group(3)
            elif fmt == 'dmy':
                d, mo, y = m.group(1), m.group(2), m.group(3)
                d = d.zfill(2)
            elif fmt == 'mnd':
                mon_name = m.group(1).lower()
                mo = MONTH_ABBR.get(mon_name)
                if not mo:
                    continue
                d = m.group(2).zfill(2)
                y = m.group(3)
            elif fmt == 'dmn':
                d = m.group(1).zfill(2)
                mon_name = m.group(2).lower()
                mo = MONTH_ABBR.get(mon_name)
                if not mo:
                    continue
                y = m.group(3)
            else:
                continue
            return y[-2:], mo, d
        except (IndexError, AttributeError):
            continue
    return None


def date_to_suffix(raw: str) -> Optional[str]:
    """将日期字符串转为 YY.MM.DD 格式后缀，失败返回 None。"""
    result = parse_date(raw)
    if result:
        yy, mm, dd = result
        return f"{yy}.{mm}.{dd}"
    return None


# ---------------------------------------------------------------------------
# 文件名解析
# ---------------------------------------------------------------------------

@dataclass
class ParsedFilename:
    """解析后的文件名各组成部分"""
    prefix: str = ""         # 方括号内的番号前缀，如 X-Art
    date_suffix: str = ""    # 日期后缀，如 16.07.24
    title_body: str = ""     # 剩余标题（空格已替换为英文点），如 Naomi.Woods.&.Lena.Anderson.-.Pussy.Party
    new_stem: str = ""       # 完整新文件主干名，如 X-Art.16.07.24.Naomi.Woods.&.Lena.Anderson.-.Pussy.Party
    original_stem: str = ""  # 原始文件主干名（无扩展名）


# 文件名格式：[前缀] 内容 日期
_BRACKET_PREFIX_RE = re.compile(r'^\[([^\]]+)\]\s*')


def _remove_date_from_stem(stem: str, date_raw: str, date_match: re.Match) -> str:
    """从 stem 中删除日期字符串（包含前后可能的空格分隔符）"""
    start, end = date_match.span()
    # 删除日期及其前面可能的空格/分隔符
    left = stem[:start].rstrip()
    right = stem[end:].lstrip()
    return left + (" " if (left and right) else "") + right


def parse_filename(stem: str) -> ParsedFilename:
    """
    解析媒体文件主干名，提取番号前缀、日期后缀、剩余标题，
    并生成统一格式的新文件主干名。

    示例输入/输出：
      "[X-Art] Naomi Woods & Lena Anderson - Pussy Party  2016-07-24"
        → prefix="X-Art", date_suffix="16.07.24",
          title_body="Naomi.Woods.&.Lena.Anderson.-.Pussy.Party",
          new_stem="X-Art.16.07.24.Naomi.Woods.&.Lena.Anderson.-.Pussy.Party"
    """
    result = ParsedFilename(original_stem=stem)

    working = stem

    # 1. 提取方括号内的前缀
    m = _BRACKET_PREFIX_RE.match(working)
    if m:
        result.prefix = m.group(1).strip()
        working = working[m.end():]
    else:
        result.prefix = ""

    # 2. 在剩余部分查找日期，取最后一处（文件名末尾的日期）
    date_match = None
    date_suffix = None
    for pat, fmt in _DATE_PATTERNS:
        for cand in pat.finditer(working):
            s = cand.group(0)
            ds = date_to_suffix(s)
            if ds:
                # 优先取靠右的日期（末尾）
                if date_match is None or cand.start() >= date_match.start():
                    date_match = cand
                    date_suffix = ds

    if date_match:
        result.date_suffix = date_suffix
        # 从剩余字符串中移除日期部分
        working = _remove_date_from_stem(working, date_match.group(0), date_match)
    else:
        result.date_suffix = ""

    # 3. 清理剩余标题：去首尾空格，内部连续空格合并，空格→英文点
    working = working.strip()
    # 合并连续空白为单个空格
    working = re.sub(r'\s+', ' ', working)
    # 去掉末尾多余的分隔符（" - " 或 " . " 等）
    working = re.sub(r'[\s.\-]+$', '', working).strip()
    # 空格替换为英文点
    title_body = working.replace(' ', '.')
    # "&" → ".And."（前后各补一个点再合并），先处理 .&. 和孤立 & 两种情况
    title_body = re.sub(r'\.?&\.?', '.And.', title_body)
    # ".-. " → "."（连字符作为分隔符时删除）
    title_body = re.sub(r'\.-\.', '.', title_body)
    # 连续多个点合并为单个点
    title_body = re.sub(r'\.{2,}', '.', title_body)
    # 去掉首尾多余的点
    # title_body = title_body.strip('.')
    result.title_body = title_body

    # 4. 拼接新主干名：前缀.日期.标题
    parts = []
    if result.prefix:
        parts.append(result.prefix)
    if result.date_suffix:
        parts.append(result.date_suffix)
    if result.title_body:
        parts.append(result.title_body)
    result.new_stem = ' '.join(parts)

    return result


# ---------------------------------------------------------------------------
# 文件分组
# ---------------------------------------------------------------------------

# 需要处理的媒体扩展名白名单
VIDEO_EXTS = {'.mp4', '.mkv', '.avi', '.mov', '.wmv', '.flv', '.m4v', '.ts', '.rmvb', '.rm'}
IMAGE_EXTS = {'.jpg', '.jpeg', '.png', '.webp', '.bmp', '.gif', '.tiff'}
NFO_EXTS = {'.nfo'}
ALL_EXTS = VIDEO_EXTS | IMAGE_EXTS | NFO_EXTS

# 附加文件后缀模式（图片/字幕等），用于从文件名中剥离得到"主干 ID"
# 例如：1-poster → 1，1-fanart1 → 1，1.nfo → 1
_SUFFIX_STRIP_RE = re.compile(
    r'[-.](?:poster|fanart\d*|thumb|banner|landscape|clearart|disc|logo|backdrop\d*|extrafanart\d*)$',
    re.IGNORECASE
)


def get_group_key(filename: str) -> str:
    """
    从文件名（不含路径，含扩展名）中提取分组 key（主干 ID）。

    "1.mp4"        → "1"
    "1-poster.jpg" → "1"
    "1.nfo"        → "1"
    "[X-Art] Naomi - Pussy Party  2016-07-24.mp4"
        → "[X-Art] Naomi - Pussy Party  2016-07-24"（直接用主干）
    """
    stem, _ = os.path.splitext(filename)
    # 剥离常见附加后缀
    stripped = _SUFFIX_STRIP_RE.sub('', stem)
    return stripped


def group_files(directory: str) -> dict[str, list[str]]:
    """
    扫描目录（非递归），按分组 key 把文件分组。
    返回 {group_key: [文件名列表]}，只包含 ALL_EXTS 内的文件。
    跳过子目录。
    """
    groups: dict[str, list[str]] = {}
    try:
        entries = os.listdir(directory)
    except PermissionError as e:
        logging.error("无法读取目录 %s: %s", directory, e)
        return groups

    for fname in entries:
        fpath = os.path.join(directory, fname)
        if os.path.isdir(fpath):
            continue
        _, ext = os.path.splitext(fname)
        if ext.lower() not in ALL_EXTS:
            continue
        key = get_group_key(fname)
        groups.setdefault(key, []).append(fname)

    return groups


# ---------------------------------------------------------------------------
# 重命名 & 移动
# ---------------------------------------------------------------------------

def _safe_rename_file(src: str, dst: str, dry_run: bool = False) -> bool:
    """重命名/移动文件，目标存在则跳过，返回是否成功。"""
    if os.path.abspath(src) == os.path.abspath(dst):
        return True
    if os.path.exists(dst):
        logging.warning("目标已存在，跳过: %s", dst)
        return False
    if dry_run:
        logging.info("[DRY-RUN] rename: %s → %s", src, dst)
        return True
    try:
        os.makedirs(os.path.dirname(dst), exist_ok=True)
        shutil.move(src, dst)
        return True
    except Exception as e:
        logging.error("移动失败 %s → %s: %s", src, dst, e)
        return False


def compute_new_filename(original_fname: str, new_stem: str) -> str:
    """
    根据原始文件名和新主干名，推算出完整新文件名。

    处理逻辑：
      - 视频文件：直接用 new_stem + ext
      - NFO 文件：new_stem + .nfo
      - 图片文件：识别原始后缀类型（poster/fanart/thumb 等），
                  新名 = new_stem + "-" + 后缀类型 + ext
                  若无后缀类型则直接 new_stem + ext
    """
    stem, ext = os.path.splitext(original_fname)
    ext_lower = ext.lower()

    if ext_lower in VIDEO_EXTS or ext_lower in NFO_EXTS:
        return new_stem + ext

    # 图片：尝试识别附加后缀
    m = _SUFFIX_STRIP_RE.search(stem)
    if m:
        suffix_part = m.group(0)  # 例如 "-poster"
        return new_stem + suffix_part + ext

    return new_stem + ext


def process_directory(directory: str, dry_run: bool = False, verbose: bool = False):
    """
    对指定目录执行完整的整理流程：
      1. 扫描文件并分组
      2. 解析每组的代表文件名（视频优先），生成新主干名
      3. 对每组文件重命名
      4. 将每组文件移动到同名子目录
    """
    if verbose:
        logging.getLogger().setLevel(logging.DEBUG)

    logging.info("扫描目录: %s", directory)
    groups = group_files(directory)
    logging.info("共发现 %d 个文件组", len(groups))

    processed = 0
    skipped = 0

    for group_key, filenames in sorted(groups.items()):
        logging.debug("处理组 [%s]，共 %d 个文件: %s", group_key, len(filenames), filenames)

        # 找到代表文件（优先视频，其次 NFO，最后图片）
        video_files = [f for f in filenames if os.path.splitext(f)[1].lower() in VIDEO_EXTS]
        nfo_files = [f for f in filenames if os.path.splitext(f)[1].lower() in NFO_EXTS]
        representative = (video_files or nfo_files or filenames)[0]

        stem, _ = os.path.splitext(representative)
        parsed = parse_filename(stem)

        if not parsed.new_stem:
            logging.warning("无法解析文件名，跳过: %s", representative)
            skipped += 1
            continue

        new_stem = parsed.new_stem
        target_dir = os.path.join(directory, new_stem)

        logging.info("  组: %-40s → %s", group_key, new_stem)

        # 先在原目录重命名，再移动到子目录
        for fname in filenames:
            new_fname = compute_new_filename(fname, new_stem)
            # 只替换文件名部分（不含扩展名）的点为空格，忽略扩展名
            stem_part, ext = os.path.splitext(new_fname)
            stem_part = stem_part.replace('.', ' ')
            new_fname = stem_part + ext
            old_path = os.path.join(directory, fname)
            tmp_path = os.path.join(directory, new_fname)
            dest_path = os.path.join(target_dir, new_fname)

            # Step 1: 重命名（原目录内）
            if fname != new_fname:
                if not _safe_rename_file(old_path, tmp_path, dry_run=dry_run):
                    continue
                logging.debug("    重命名: %s → %s", fname, new_fname)
                moved_from = tmp_path
            else:
                moved_from = old_path

            # Step 2: 移动到子目录
            if not dry_run:
                os.makedirs(target_dir, exist_ok=True)
            _safe_rename_file(moved_from, dest_path, dry_run=dry_run)
            logging.debug("    移动: %s → %s/", new_fname, new_stem)

        processed += 1

    logging.info("完成：处理 %d 组，跳过 %d 组", processed, skipped)
    return processed, skipped


# ---------------------------------------------------------------------------
# CLI
# ---------------------------------------------------------------------------

def main():
    parser = argparse.ArgumentParser(
        description="JAV 媒体文件整理工具：识别文件名番号、重命名并移动到子目录"
    )
    parser.add_argument("directory", help="要整理的目标目录路径")
    parser.add_argument(
        "--dry-run", action="store_true", default=False,
        help="只打印操作计划，不实际执行重命名/移动"
    )
    parser.add_argument(
        "--verbose", "-v", action="store_true", default=False,
        help="输出详细日志"
    )
    args = parser.parse_args()

    if not os.path.isdir(args.directory):
        print(f"[ERROR] 目录不存在: {args.directory}", file=sys.stderr)
        sys.exit(1)

    log_level = logging.DEBUG if args.verbose else logging.INFO
    logging.basicConfig(
        level=log_level,
        format="%(levelname)s %(message)s",
        stream=sys.stdout,
    )

    if args.dry_run:
        logging.info("=== DRY-RUN 模式，不会实际修改文件 ===")

    process_directory(args.directory, dry_run=args.dry_run, verbose=args.verbose)


# ---------------------------------------------------------------------------
# 快速自测（python jav_recognizer.py --selftest）
# ---------------------------------------------------------------------------

def _selftest():
    cases = [
        (
            "[X-Art] Naomi Woods & Lena Anderson - Pussy Party  2016-07-24",
            "X-Art.16.07.24.Naomi.Woods.And.Lena.Anderson.Pussy.Party",
        ),
        (
            "[GFRevenge] Blaire Ivory - Bare Blaire  26.12.2016",
            "GFRevenge.16.12.26.Blaire.Ivory.Bare.Blaire",
        ),
        (
            "[X-Art] Lena Anderson - Teenage Lust  Jul 27, 2016",
            "X-Art.16.07.27.Lena.Anderson.Teenage.Lust",
        ),
    ]
    ok = True
    for stem, expected in cases:
        result = parse_filename(stem)
        status = "✓" if result.new_stem == expected else "✗"
        if result.new_stem != expected:
            ok = False
        print(f"{status} 输入: {stem!r}")
        print(f"  期望: {expected!r}")
        print(f"  实际: {result.new_stem!r}")
        print()
    return ok


if __name__ == "__main__":
    if len(sys.argv) == 2 and sys.argv[1] == "--selftest":
        success = _selftest()
        sys.exit(0 if success else 1)
    main()
