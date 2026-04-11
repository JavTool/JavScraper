#!/usr/bin/env python3
# -*- coding: utf-8 -*-

import os
import logging
import argparse

'''
使用方法：
python rename_tool.py "G:\Jav\222" --default-patterns
'''

def setup_logging(log_dir):
    """
    配置日志记录
    """
    # 确保日志目录存在
    os.makedirs(log_dir, exist_ok=True)
    
    # 配置重命名记录日志
    renamed_logger = logging.getLogger('renamed')
    renamed_logger.setLevel(logging.INFO)
    renamed_handler = logging.FileHandler(os.path.join(log_dir, 'renamed.txt'), encoding='utf-8')
    renamed_handler.setFormatter(logging.Formatter('%(message)s'))
    renamed_logger.addHandler(renamed_handler)
    
    # 配置跳过记录日志
    skipped_logger = logging.getLogger('skipped')
    skipped_logger.setLevel(logging.INFO)
    skipped_handler = logging.FileHandler(os.path.join(log_dir, 'skipped.txt'), encoding='utf-8')
    skipped_handler.setFormatter(logging.Formatter('%(message)s'))
    skipped_logger.addHandler(skipped_handler)
    
    return renamed_logger, skipped_logger

def rename_items(base_path, replace_map, renamed_logger, skipped_logger):
    """
    遍历并重命名文件和目录
    """
    # 首先收集所有需要重命名的项目（目录和文件）
    items_to_rename = []
    
    # 遍历所有目录
    for root, dirs, files in os.walk(base_path):
        # 检查目录名
        for dir_name in dirs:
            old_dir_path = os.path.join(root, dir_name)
            new_dir_name = dir_name
            has_changed = False
            
            # 替换目录名中的字符串
            for old_str, new_str in replace_map.items():
                if old_str in new_dir_name:
                    new_dir_name = new_dir_name.replace(old_str, new_str)
                    has_changed = True
            
            if has_changed:
                new_dir_path = os.path.join(root, new_dir_name)
                items_to_rename.append((old_dir_path, new_dir_path, 'dir'))
        
        # 检查文件名
        for file_name in files:
            old_file_path = os.path.join(root, file_name)
            new_file_name = file_name
            has_changed = False
            
            # 替换文件名中的字符串
            for old_str, new_str in replace_map.items():
                if old_str in new_file_name:
                    new_file_name = new_file_name.replace(old_str, new_str)
                    has_changed = True
            
            if has_changed:
                new_file_path = os.path.join(root, new_file_name)
                items_to_rename.append((old_file_path, new_file_path, 'file'))
    
    # 执行重命名操作（先重命名目录，再重命名文件）
    # 分离目录和文件
    dir_renames = [item for item in items_to_rename if item[2] == 'dir']
    file_renames = [item for item in items_to_rename if item[2] == 'file']
    
    # 重命名目录
    for old_path, new_path, item_type in dir_renames:
        try:
            os.rename(old_path, new_path)
            renamed_logger.info(f"Directory: {old_path} -> {new_path}")
            print(f"Renamed directory: {old_path} -> {new_path}")
        except Exception as e:
            print(f"Error renaming directory {old_path}: {e}")
    
    # 重命名文件
    for old_path, new_path, item_type in file_renames:
        # 检查是否需要更新路径（因为目录可能已经重命名）
        for old_dir, new_dir, _ in dir_renames:
            if old_path.startswith(old_dir):
                old_path = old_path.replace(old_dir, new_dir)
                new_path = new_path.replace(old_dir, new_dir)
        
        try:
            os.rename(old_path, new_path)
            renamed_logger.info(f"File: {old_path} -> {new_path}")
            print(f"Renamed file: {old_path} -> {new_path}")
        except Exception as e:
            print(f"Error renaming file {old_path}: {e}")
    
    # 记录未重命名的项目
    all_items = []
    for root, dirs, files in os.walk(base_path):
        for dir_name in dirs:
            all_items.append(os.path.join(root, dir_name))
        for file_name in files:
            all_items.append(os.path.join(root, file_name))
    
    renamed_items = [item[0] for item in items_to_rename]
    for item in all_items:
        if item not in renamed_items:
            skipped_logger.info(item)
    
    print(f"\nTotal items processed: {len(all_items)}")
    print(f"Items renamed: {len(items_to_rename)}")
    print(f"Items skipped: {len(all_items) - len(items_to_rename)}")

def main():
    parser = argparse.ArgumentParser(description='Rename files and directories based on specified patterns')
    parser.add_argument('base_path', default='', help='The base directory to traverse')
    parser.add_argument('--log-dir', default='logs', help='Directory to store log files (default: logs)')
    parser.add_argument('--replace', action='append', help='Replacement pairs in format "old_str:new_str"')
    parser.add_argument('--default-patterns', action='store_true', help='Use default replacement patterns')
    
    args = parser.parse_args()
    
    # 配置替换映射
    replace_map = {}
    
    if args.default_patterns:
        # 默认替换模式
        replace_map = {
            '中字無碼破解': '中字无码',
            '中字无码破解':'中字无码',
            '中字无码流出':'中字无码',
            '無碼破解': '无码',
            '无码破解': '无码',
            '无码流出': '无码',
        }
    
    if args.replace:
        # 从命令行参数添加替换模式
        for pair in args.replace:
            if ':' in pair:
                old_str, new_str = pair.split(':', 1)
                replace_map[old_str] = new_str
    
    # 检查是否有替换模式
    if not replace_map:
        print("No replacement patterns specified. Use --default-patterns or --replace.")
        return
    
    # 检查基本路径是否存在
    if not os.path.exists(args.base_path):
        print(f"Error: Base path '{args.base_path}' does not exist.")
        return
    
    # 设置日志
    renamed_logger, skipped_logger = setup_logging(args.log_dir)
    
    # 执行重命名
    print(f"Starting traversal of '{args.base_path}'")
    print(f"Replacement patterns: {replace_map}")
    print("-" * 50)
    rename_items(args.base_path, replace_map, renamed_logger, skipped_logger)
    print("-" * 50)
    print("Processing complete!")
    print(f"Renamed items log: {os.path.abspath(os.path.join(args.log_dir, 'renamed.txt'))}")
    print(f"Skipped items log: {os.path.abspath(os.path.join(args.log_dir, 'skipped.txt'))}")

if __name__ == "__main__":
    main()