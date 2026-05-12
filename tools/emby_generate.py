#!/usr/bin/env python3
# -*- coding: utf-8 -*-
'''
根据用户指定目录生成 Emby nfo 文件
例如：用户指定目录 "\\\\192.168.1.199\\Jav\\Western\\No1syGirl"，目录中有很多媒体文件，遍历该目录下所有媒体文件，先将创建同名文件夹，然后将媒体文件移动到同名文件夹中，并生成对应的 nfo 文件，nfo 文件标题和简介使用文件名，演员名使用父目录的目录名（此处应为：No1syGirl），发布时间使用文件的创建时间，时长使用媒体文件的时长。
用法：python emby_generate.py "\\192.168.1.199\Jav\Korea"
'''

import os
import sys
import shutil
import argparse
import subprocess
import json
import xml.etree.ElementTree as ET
from datetime import datetime

MEDIA_EXTS = (
    '.mp4', '.mkv', '.avi', '.wmv', '.mov', '.flv',
    '.m4v', '.ts', '.mpg', '.mpeg', '.webm', '.rmvb',
    '.rm', '.3gp', '.vob', '.iso'
)


def get_media_duration(file_path):
    """使用 ffprobe 获取媒体文件时长（分钟），失败返回 """
    try:
        cmd = [
            'ffprobe', '-v', 'error', '-show_entries', 'format=duration',
            '-of', 'json', file_path
        ]
        result = subprocess.run(
            cmd, capture_output=True, text=True, timeout=30
        )
        if result.returncode == 0:
            info = json.loads(result.stdout)
            seconds = float(info.get('format', {}).get('duration', 0))
            minutes = int(round(seconds / 60))
            return str(minutes) if minutes > 0 else ""
    except FileNotFoundError:
        print("[WARN] 未找到 ffprobe，跳过时长提取")
    except Exception as e:
        print(f"[WARN] 获取时长失败 {file_path}: {e}")
    return ""


def get_file_create_time(file_path):
    """获取文件创建时间 YYYY-MM-DD"""
    try:
        # Windows 上 st_ctime 是创建时间，Unix 上是元数据更改时间
        ctime = os.path.getctime(file_path)
        return datetime.fromtimestamp(ctime).strftime('%Y-%m-%d')
    except Exception as e:
        print(f"[WARN] 获取创建时间失败 {file_path}: {e}")
        return ""


def build_nfo(title, plot, actor_name, date, runtime, nfo_path):
    """构建并写入 nfo 文件"""
    root = ET.Element("movie")

    def sub(tag, text):
        el = ET.SubElement(root, tag)
        el.text = str(text) if text is not None else ""
        return el

    sub("plot", plot)
    sub("outline", "")
    sub("lockdata", "true")
    sub("lockedfields",
        "Name|OriginalTitle|SortName|Overview|OfficialRating|Cast|Studios")
    sub("dateadded", date)
    sub("title", title)
    sub("originaltitle", title)

    if actor_name:
        actor_el = ET.SubElement(root, "actor")
        ET.SubElement(actor_el, "name").text = actor_name
        ET.SubElement(actor_el, "type").text = "Actor"

    year = date[:4] if len(date) >= 4 else ""
    sub("year", year)
    sub("sorttitle", title)
    sub("mpaa", "XXX")
    sub("premiered", date)
    sub("releasedate", date)
    sub("runtime", runtime)
    if actor_name:
        sub("studio", actor_name)

    ET.indent(root, space="  ")
    tree = ET.ElementTree(root)
    tree.write(nfo_path, encoding="utf-8", xml_declaration=True)


def process_directory(directory, dry_run=False):
    """处理指定目录，为其中每个媒体文件创建同名文件夹并生成 nfo"""
    directory = os.path.abspath(directory)
    if not os.path.isdir(directory):
        print(f"[ERROR] 目录不存在: {directory}")
        return

    # 演员名 = 父目录名（即用户指定的目录本身的名字）
    actor_name = os.path.basename(directory.rstrip(os.sep))
    print(f"[INFO] 目录: {directory}")
    print(f"[INFO] 演员名: {actor_name}")

    media_files = []
    for entry in os.listdir(directory):
        print(f"[INFO] 检测文件: {entry}")
        full_path = os.path.join(directory, entry)
        if os.path.isfile(full_path) and entry.lower().endswith(MEDIA_EXTS):
            media_files.append(full_path)

    if not media_files:
        print(f"[WARN] 目录下未找到媒体文件")
        return

    print(f"[INFO] 发现 {len(media_files)} 个媒体文件\n")

    success = 0
    skipped = 0
    failed = 0

    for media_path in media_files:
        file_name = os.path.basename(media_path)
        base_name, ext = os.path.splitext(file_name)

        target_folder = os.path.join(directory, base_name)
        target_media = os.path.join(target_folder, file_name)
        nfo_path = os.path.join(target_folder, f"{base_name}.nfo")

        print(f"[PROC] {file_name}")

        if dry_run:
            print(f"  [DRY-RUN] 创建目录: {target_folder}")
            print(f"  [DRY-RUN] 移动文件: {file_name} -> {target_folder}\\")
            print(f"  [DRY-RUN] 生成 NFO: {nfo_path}")
            success += 1
            continue

        try:
            # 获取时长（移动前）
            # runtime = get_media_duration(media_path)
            runtime = 0
            date = get_file_create_time(media_path)

            # 创建目标文件夹
            os.makedirs(target_folder, exist_ok=True)

            # 移动媒体文件
            if os.path.exists(target_media):
                print(f"  [SKIP] 目标文件已存在: {target_media}")
                skipped += 1
                continue
            shutil.move(media_path, target_media)
            print(f"  [MOVE] -> {target_folder}")

            # 生成 NFO
            build_nfo(
                title=base_name,
                plot=base_name,
                actor_name=actor_name,
                date=date,
                runtime=runtime,
                nfo_path=nfo_path
            )
            print(f"  [NFO]  时长={runtime or '?'}分钟 日期={date}")
            success += 1
        except Exception as e:
            print(f"  [ERROR] 处理失败: {e}")
            failed += 1

    print(f"\n{'=' * 50}")
    print(f"完成统计:")
    print(f"  媒体文件总数: {len(media_files)}")
    print(f"  成功处理:     {success}")
    print(f"  跳过:         {skipped}")
    print(f"  失败:         {failed}")


def main():
    parser = argparse.ArgumentParser(
        description="根据指定目录为其中的媒体文件生成 Emby NFO"
    )
    parser.add_argument("directory", help="目录路径（演员名取此目录名）")
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="试运行，只打印将要执行的操作而不实际移动和生成"
    )
    args = parser.parse_args()

    process_directory(args.directory, dry_run=args.dry_run)


if __name__ == "__main__":
    main()