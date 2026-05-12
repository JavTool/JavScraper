#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
VixenPlus 刮削器测试脚本
"""

import os
import sys
import tempfile
import xml.etree.ElementTree as ET

# 添加当前目录到 Python 路径
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from vixenscraper import VixenPlusScraper

def create_test_nfo(directory, originaltitle):
    """创建测试 NFO 文件"""
    os.makedirs(directory, exist_ok=True)

    root = ET.Element("movie")
    title_elem = ET.SubElement(root, "title")
    title_elem.text = "Test Video"

    originaltitle_elem = ET.SubElement(root, "originaltitle")
    originaltitle_elem.text = originaltitle

    nfo_path = os.path.join(directory, "Blacked.21.05.29.Test.Video.XXX.1080p.nfo")
    tree = ET.ElementTree(root)
    tree.write(nfo_path, encoding="utf-8", xml_declaration=True)
    return nfo_path

def test_parsing():
    """测试 NFO 解析功能"""
    print("测试 NFO 解析功能...")

    with tempfile.TemporaryDirectory() as temp_dir:
        # 创建测试 NFO
        test_originaltitle = "Blacked.21.05.29 Vacay Part 2"
        nfo_path = create_test_nfo(temp_dir, test_originaltitle)

        # 测试解析
        scraper = VixenPlusScraper()
        parsed_title, parsed_studio = scraper.parse_nfo(nfo_path)
        extracted_title = scraper.extract_title_from_originaltitle(parsed_title)

        print(f"原始 originaltitle: {test_originaltitle}")
        print(f"解析结果: {parsed_title}")
        print(f"解析 studio: {parsed_studio}")
        print(f"提取标题: {extracted_title}")

        assert parsed_title == test_originaltitle, f"解析失败: {parsed_title}"
        assert extracted_title == "Vacay Part 2", f"标题提取失败: {extracted_title}"

        print("✓ NFO 解析测试通过")

def test_tags_management():
    """测试标签管理功能"""
    print("\n测试标签管理功能...")

    with tempfile.TemporaryDirectory() as temp_dir:
        tags_file = os.path.join(temp_dir, "test_tags.json")

        scraper = VixenPlusScraper(tags_file=tags_file)

        # 测试添加标签
        initial_tags = scraper.tags.copy()
        scraper.update_tags(["test_tag1", "test_tag2"])
        scraper.update_tags(["test_tag1", "test_tag3"])  # test_tag1 应该不重复

        expected_tags = ["test_tag1", "test_tag2", "test_tag3"]
        assert set(scraper.tags) == set(expected_tags), f"标签管理失败: {scraper.tags}"

        # 验证文件保存
        scraper2 = VixenPlusScraper(tags_file=tags_file)
        assert set(scraper2.tags) == set(expected_tags), f"标签文件保存失败: {scraper2.tags}"

        print("✓ 标签管理测试通过")

def test_directory_structure():
    """测试目录结构生成"""
    print("\n测试目录结构生成...")

    with tempfile.TemporaryDirectory() as temp_dir:
        emby_dir = os.path.join(temp_dir, "Emby")
        scraper = VixenPlusScraper(emby_base_dir=emby_dir)

        # 模拟数据
        test_data = {
            "title": "Test Video",
            "plot": "Test plot",
            "date": "2021-05-29",
            "actors": ["Test Actor"],
            "genres": ["Test Genre"],
            "cover": "http://example.com/cover.jpg",
            "url": "http://example.com/video",
            "studio": "Blacked",
            "runtime": "30",
        }

        original_nfo_path = "/fake/path/Blacked.21.05.29.Test.Actor.Test.Video.XXX.1080p/Blacked.21.05.29.Test.Actor.Test.Video.XXX.1080p.nfo"

        # 生成 NFO
        nfo_path = scraper.generate_nfo(test_data, original_nfo_path)

        # 验证文件存在
        assert os.path.exists(nfo_path), f"NFO 文件未生成: {nfo_path}"

        # 验证目录结构
        expected_actor = "Test"  # 从目录名中智能提取的演员名
        expected_dir = os.path.join(emby_dir, expected_actor, "Blacked.21.05.29.Test.Actor.Test.Video.XXX.1080p")
        actual_dir = os.path.dirname(nfo_path)
        print(f"期望目录: {expected_dir}")
        print(f"实际目录: {actual_dir}")
        assert actual_dir == expected_dir, f"目录结构错误: {actual_dir}"

        print(f"✓ 目录结构测试通过，生成路径: {nfo_path}")

if __name__ == "__main__":
    print("开始运行 VixenPlus 刮削器测试...\n")

    try:
        test_parsing()
        test_tags_management()
        test_directory_structure()

        print("\n🎉 所有测试通过！")
        print("\n注意：完整功能测试需要实际的 VixenPlus 会员账户和网络连接。")
        print("要运行完整测试，请使用：")
        print("python vixenscraper.py /path/to/your/nfo/directory")

    except Exception as e:
        print(f"\n❌ 测试失败: {e}")
        import traceback
        traceback.print_exc()
        sys.exit(1)