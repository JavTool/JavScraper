#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
测试 jacketdownload.py 功能
"""

import os
import tempfile
import xml.etree.ElementTree as ET
from jacketdownload import JacketDownloader


def create_test_nfo(file_path: str, studio: str = "カリビアンコム", number: str = "123456-789"):
    """创建测试 NFO 文件"""
    nfo_content = f'''<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<movie>
    <plot>测试剧情</plot>
    <title>测试标题</title>
    <originaltitle>{number} 测试原始标题</originaltitle>
    <sorttitle>{number}</sorttitle>
    <studio>{studio}</studio>
</movie>'''
    
    with open(file_path, 'w', encoding='utf-8') as f:
        f.write(nfo_content)
    print(f"✓ 创建测试 NFO 文件: {file_path}")


def test_extract_functions():
    """测试提取功能"""
    print("\n=== 测试提取功能 ===")
    
    with tempfile.TemporaryDirectory() as temp_dir:
        # 创建测试 NFO 文件
        test_nfo = os.path.join(temp_dir, "test.nfo")
        create_test_nfo(test_nfo)
        
        # 创建下载器实例
        downloader = JacketDownloader()
        
        # 测试提取番号
        number = downloader.extract_number_from_nfo(test_nfo)
        print(f"提取的番号: {number}")
        assert number == "123456-789", f"番号提取错误，期望: 123456-789, 实际: {number}"
        
        # 测试提取工作室
        studio = downloader.extract_studio_from_nfo(test_nfo)
        print(f"提取的工作室: {studio}")
        assert studio == "カリビアンコム", f"工作室提取错误，期望: カリビアンコム, 实际: {studio}"
        
        print("✓ 提取功能测试通过")


def test_non_caribbean_studio():
    """测试非 Caribbean.com 工作室的处理"""
    print("\n=== 测试非 Caribbean.com 工作室 ===")
    
    with tempfile.TemporaryDirectory() as temp_dir:
        # 创建非 Caribbean.com 的测试 NFO 文件
        test_nfo = os.path.join(temp_dir, "test_other.nfo")
        create_test_nfo(test_nfo, studio="其他工作室")
        
        # 创建下载器实例
        downloader = JacketDownloader()
        
        # 处理文件，应该跳过
        result = downloader.process_nfo_file(test_nfo)
        assert result == False, "非 Caribbean.com 工作室应该被跳过"
        
        print("✓ 非 Caribbean.com 工作室测试通过")


def test_directory_processing():
    """测试目录处理功能"""
    print("\n=== 测试目录处理功能 ===")
    
    with tempfile.TemporaryDirectory() as temp_dir:
        # 创建子目录
        sub_dir = os.path.join(temp_dir, "subdir")
        os.makedirs(sub_dir)
        
        # 创建多个测试文件
        create_test_nfo(os.path.join(temp_dir, "caribbean1.nfo"), "カリビアンコム", "123456-001")
        create_test_nfo(os.path.join(sub_dir, "caribbean2.nfo"), "カリビアンコム", "123456-002")
        create_test_nfo(os.path.join(temp_dir, "other.nfo"), "其他工作室", "ABC-123")
        
        # 创建下载器实例
        downloader = JacketDownloader()
        
        # 处理目录
        stats = downloader.process_directory(temp_dir)
        
        # 验证统计信息
        assert stats['total_nfo'] == 3, f"总 NFO 文件数错误，期望: 3, 实际: {stats['total_nfo']}"
        assert stats['caribbean_nfo'] == 2, f"Caribbean NFO 文件数错误，期望: 2, 实际: {stats['caribbean_nfo']}"
        
        print("✓ 目录处理功能测试通过")
        print(f"统计信息: {stats}")


def main():
    """运行所有测试"""
    print("开始测试 JacketDownloader 功能...")
    
    try:
        test_extract_functions()
        test_non_caribbean_studio()
        test_directory_processing()
        
        print("\n🎉 所有测试通过！")
        print("\n注意: 网络下载功能需要实际的网络连接和有效的番号才能测试")
        
    except Exception as e:
        print(f"\n❌ 测试失败: {e}")
        raise


if __name__ == "__main__":
    main()