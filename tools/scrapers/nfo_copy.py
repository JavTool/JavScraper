'''NFO 拷贝工具
说明：拷贝指定目录下所有文件夹中的 nfo 文件到目标目录
遍历源目录下所有 nfo 文件，与目标目录下文件夹名称匹配，拷贝 nfo 文件到目标目录
例如：我在 "\\192.168.1.199\\Charles\\nfobak" 存放了所有 nfo 文件的备份，想要把这些 nfo 文件拷贝到 "\\192.168.1.199\\Jav\\Western" 的所有子目录下， "\\192.168.1.199\\Jav\\Western" 中的 nfo 文件可能在更深层次的文件夹中，需要遍历目录寻找匹配的文件夹名称，然后拷贝 nfo 文件到目标目录，如果目标目录下存在同名 nfo 文件则覆盖

用法：python nfo_copy.py "源目录" "目标目录"
示例：python nfo_copy.py "G:/Jav/2" "G:/Jav/1"
python nfo_copy.py "G:/Jav/2" "G:/Jav/1"
'''

import os
import shutil
import sys
import argparse


def find_all_nfo_files(source_dir):
    """递归查找源目录下所有 .nfo 文件，返回 [(绝对路径, 文件名不含扩展名), ...]"""
    result = []
    for root, dirs, files in os.walk(source_dir):
        for f in files:
            if f.lower().endswith('.nfo'):
                full_path = os.path.join(root, f)
                base_name = os.path.splitext(f)[0]
                result.append((full_path, base_name))
    return result


def find_all_folders(target_dir):
    """递归查找目标目录下所有文件夹，返回 [(文件夹名称, 绝对路径), ...]"""
    result = []
    for root, dirs, files in os.walk(target_dir):
        for d in dirs:
            folder_path = os.path.join(root, d)
            result.append((d, folder_path))
    return result


def copy_nfo_files(source_dir, target_dir, dry_run=False):
    """
    拷贝 nfo 文件到目标目录下匹配的文件夹

    匹配规则：nfo 文件名（不含扩展名）与目标目录树中的文件夹名称大小写不敏感匹配
    如果目标文件夹已存在同名 nfo 文件则覆盖
    """
    source_dir = os.path.abspath(source_dir)
    target_dir = os.path.abspath(target_dir)

    nfo_files = find_all_nfo_files(source_dir)
    if not nfo_files:
        print(f"[WARN] 源目录下未找到 .nfo 文件: {source_dir}")
        return

    folders = find_all_folders(target_dir)
    if not folders:
        print(f"[WARN] 目标目录下未找到任何文件夹: {target_dir}")
        return

    # 构建文件夹名称 -> 路径列表 的映射（大小写不敏感）
    folder_map = {}
    for name, path in folders:
        key = name.lower()
        folder_map.setdefault(key, []).append(path)

    copied = 0
    overwritten = 0
    not_found = 0
    multi_match = 0
    not_found_list = []

    print(f"[INFO] 源目录: {source_dir}")
    print(f"[INFO] 目标目录: {target_dir}")
    print(f"[INFO] 发现 {len(nfo_files)} 个 nfo 文件, {len(folders)} 个目标文件夹\n")

    for nfo_path, base_name in nfo_files:
        key = base_name.lower()
        nfo_name = os.path.basename(nfo_path)

        if key not in folder_map:
            not_found_list.append(nfo_name)
            not_found += 1
            continue

        target_folders = folder_map[key]
        if len(target_folders) > 1:
            print(f"[WARN] 找到 {len(target_folders)} 个同名匹配文件夹: {base_name}")
            multi_match += 1

        for target_folder in target_folders:
            target_nfo_path = os.path.join(target_folder, nfo_name)

            exists = os.path.exists(target_nfo_path)
            if dry_run:
                action = "[DRY-RUN OVERWRITE]" if exists else "[DRY-RUN COPY]"
                print(f"{action} {nfo_name} -> {target_folder}")
                continue

            if exists:
                print(f"[OVERWRITE] {nfo_name} -> {target_folder}")
                overwritten += 1
            else:
                print(f"[COPY] {nfo_name} -> {target_folder}")

            try:
                shutil.copy2(nfo_path, target_nfo_path)
                copied += 1
            except Exception as e:
                print(f"[ERROR] 拷贝失败 {nfo_name} -> {target_folder}: {e}")

    print(f"\n{'=' * 50}")
    print(f"完成统计:")
    print(f"  nfo 文件总数: {len(nfo_files)}")
    print(f"  成功拷贝:     {copied}")
    print(f"  覆盖已有:     {overwritten}")
    print(f"  未找到匹配:   {not_found}")
    print(f"  多文件夹匹配: {multi_match}")

    if not_found_list:
        print(f"\n未找到匹配文件夹的 nfo 文件列表 ({len(not_found_list)} 个):")
        for name in not_found_list:
            print(f"  - {name}")


def main():
    parser = argparse.ArgumentParser(
        description="NFO 拷贝工具 - 将 nfo 文件按名称匹配拷贝到目标文件夹"
    )
    parser.add_argument("source_dir", help="源目录（包含 .nfo 文件）")
    parser.add_argument("target_dir", help="目标目录（包含子文件夹）")
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="试运行，只打印将要执行的操作而不实际拷贝"
    )
    args = parser.parse_args()

    if not os.path.isdir(args.source_dir):
        print(f"[ERROR] 源目录不存在: {args.source_dir}")
        sys.exit(1)
    if not os.path.isdir(args.target_dir):
        print(f"[ERROR] 目标目录不存在: {args.target_dir}")
        sys.exit(1)

    copy_nfo_files(args.source_dir, args.target_dir, dry_run=args.dry_run)


if __name__ == "__main__":
    main()