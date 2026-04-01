#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
测试 nfo 文件处理功能
"""

import os
import tempfile
import xml.etree.ElementTree as ET
from madouposter import process_nfo_files, process_single_nfo_file

def create_test_nfo_file(file_path, plot="測試劇情", title="測試標題", 
                        originaltitle="測試原標題", sorttitle="91CM076", 
                        javscraperid="91CM076"):
    """
    创建测试用的 nfo 文件
    """
    movie = ET.Element('movie')
    
    plot_elem = ET.SubElement(movie, 'plot')
    plot_elem.text = plot
    
    title_elem = ET.SubElement(movie, 'title')
    title_elem.text = title
    
    originaltitle_elem = ET.SubElement(movie, 'originaltitle')
    originaltitle_elem.text = originaltitle
    
    sorttitle_elem = ET.SubElement(movie, 'sorttitle')
    sorttitle_elem.text = sorttitle
    
    javscraperid_elem = ET.SubElement(movie, 'javscraperid')
    javscraperid_elem.text = javscraperid
    
    tree = ET.ElementTree(movie)
    tree.write(file_path, encoding='utf-8', xml_declaration=True)
    print(f"创建测试文件: {file_path}")

def create_test_poster_file(test_dir, format='jpg'):
    """
    创建测试用的 poster 图片文件
    
    参数:
        test_dir (str): 测试目录路径
        format (str): 图片格式 ('jpg', 'png', 'webp')
    """
    try:
        from PIL import Image
        
        # 创建一个简单的测试图片
        img = Image.new('RGB', (100, 150), color='red')
        
        if format == 'jpg':
            poster_path = os.path.join(test_dir, 'poster.jpg')
            img.save(poster_path, 'JPEG')
        elif format == 'png':
            poster_path = os.path.join(test_dir, 'poster.png')
            img.save(poster_path, 'PNG')
        elif format == 'webp':
            poster_path = os.path.join(test_dir, 'poster.webp')
            img.save(poster_path, 'WEBP')
        
        print(f"已创建测试 poster.{format}: {poster_path}")
        return poster_path
        
    except ImportError:
        # 如果没有 PIL，创建文本文件作为替代
        poster_path = os.path.join(test_dir, f'poster.{format}')
        test_content = f"Test poster image content - {format} format"
        
        with open(poster_path, 'w', encoding='utf-8') as f:
            f.write(test_content)
        
        print(f"已创建测试 poster.{format} (文本文件): {poster_path}")
        return poster_path

def read_nfo_file(file_path):
    """
    读取 nfo 文件内容
    """
    tree = ET.parse(file_path)
    root = tree.getroot()
    
    result = {}
    for field in ['plot', 'title', 'originaltitle', 'sorttitle', 'javscraperid']:
        elem = root.find(field)
        result[field] = elem.text if elem is not None else None
    
    return result

def test_nfo_processing():
    """
    测试 nfo 文件处理功能
    """
    # 创建临时目录
    with tempfile.TemporaryDirectory() as temp_dir:
        print(f"使用临时目录: {temp_dir}")
        
        # 创建子目录
        sub_dir = os.path.join(temp_dir, "subdir")
        os.makedirs(sub_dir)
        
        # 测试用例 1: 91 开头的番号
        test1_path = os.path.join(temp_dir, "test1.nfo")
        create_test_nfo_file(test1_path, 
                            plot="測試劇情1", 
                            title="測試標題1", 
                            originaltitle="測試原標題1", 
                            sorttitle="91CM076", 
                            javscraperid="91CM076")
        
        # 测试用例 2: 普通番号，javscraperid 包含连字符
        test2_path = os.path.join(sub_dir, "test2.nfo")
        create_test_nfo_file(test2_path, 
                            plot="測試劇情2", 
                            title="測試標題2", 
                            originaltitle="測試原標題2", 
                            sorttitle="ABC123", 
                            javscraperid="ABC-123")
        
        # 测试用例 3: 普通番号，无连字符
        test3_path = os.path.join(sub_dir, "test3.nfo")
        create_test_nfo_file(test3_path, 
                            plot="測試劇情3", 
                            title="測試標題3", 
                            originaltitle="測試原標題3", 
                            sorttitle="XYZ456", 
                            javscraperid="XYZ456")
        
        # 测试不同格式的 poster 图片
        print("\n=== 测试 poster 图片格式转换功能 ===")
        
        # 测试 PNG 格式转换
        print("\n--- 测试 PNG 转 JPG ---")
        create_test_poster_file(temp_dir, 'png')
        
        # 处理 nfo 文件（这会触发 poster 图片处理）
        process_single_nfo_file(test1_path)
        
        # 验证转换结果
        poster_jpg_path = os.path.join(temp_dir, 'poster.jpg')
        fanart_path = os.path.join(temp_dir, 'fanart.jpg')
        thumb_path = os.path.join(temp_dir, 'thumb.jpg')
        poster_png_path = os.path.join(temp_dir, 'poster.png')
        
        assert os.path.exists(poster_jpg_path), "poster.jpg 应该被创建"
        assert os.path.exists(fanart_path), "fanart.jpg 应该被创建"
        assert os.path.exists(thumb_path), "thumb.jpg 应该被创建"
        
        # 检查原 PNG 文件是否被删除
        png_deleted = not os.path.exists(poster_png_path)
        print(f"PNG 文件删除状态: {png_deleted}")
        
        print("PNG 转换测试通过！")
        
        # 清理文件，准备下一个测试
        for file in [poster_jpg_path, fanart_path, thumb_path]:
            if os.path.exists(file):
                os.remove(file)
        
        # 测试 JPG 格式（不需要转换）
        print("\n--- 测试 JPG 格式（无需转换）---")
        create_test_poster_file(temp_dir, 'jpg')
        
        # 处理 nfo 文件
        process_single_nfo_file(test1_path)
        
        # 验证 JPG 处理结果
        assert os.path.exists(poster_jpg_path), "poster.jpg 应该存在"
        assert os.path.exists(fanart_path), "fanart.jpg 应该被创建"
        assert os.path.exists(thumb_path), "thumb.jpg 应该被创建"
        
        print("JPG 处理测试通过！")
        
        # 清理文件，准备下一个测试
        for file in [poster_jpg_path, fanart_path, thumb_path]:
            if os.path.exists(file):
                os.remove(file)
        
        # 创建子目录的测试文件
        create_test_poster_file(sub_dir, 'jpg')
        poster1_path = os.path.join(temp_dir, 'poster.jpg')
        poster2_path = os.path.join(sub_dir, 'poster.jpg')
        create_test_poster_file(temp_dir, 'jpg')
        
        print("\n=== 处理前的内容 ===")
        for i, path in enumerate([test1_path, test2_path, test3_path], 1):
            print(f"\n测试文件 {i}: {path}")
            content = read_nfo_file(path)
            for key, value in content.items():
                print(f"  {key}: {value}")
        
        # 处理 nfo 文件
        print("\n=== 开始处理 ===")
        process_nfo_files(temp_dir)
        
        print("\n=== 处理后的内容 ===")
        for i, path in enumerate([test1_path, test2_path, test3_path], 1):
            print(f"\n测试文件 {i}: {path}")
            content = read_nfo_file(path)
            for key, value in content.items():
                print(f"  {key}: {value}")
        
        # 验证结果
        print("\n=== 验证结果 ===")
        
        # 验证测试文件 1 (91CM076)
        content1 = read_nfo_file(test1_path)
        expected_sorttitle1 = "91CM-076"
        assert content1['sorttitle'] == expected_sorttitle1, f"测试1失败: sorttitle 应该是 {expected_sorttitle1}, 实际是 {content1['sorttitle']}"
        assert content1['originaltitle'] == f"{expected_sorttitle1} 测试标题1", f"测试1失败: originaltitle 格式错误"
        assert content1['plot'] == "测试标题1", f"测试1失败: plot 应该等于 title"
        print("✓ 测试1通过: 91 开头番号处理正确")
        
        # 验证测试文件 2 (javscraperid 包含连字符)
        content2 = read_nfo_file(test2_path)
        expected_sorttitle2 = "ABC-123"  # 应该使用 javscraperid 的值
        assert content2['sorttitle'] == expected_sorttitle2, f"测试2失败: sorttitle 应该是 {expected_sorttitle2}, 实际是 {content2['sorttitle']}"
        assert content2['originaltitle'] == f"{expected_sorttitle2} 测试标题2", f"测试2失败: originaltitle 格式错误"
        assert content2['plot'] == "测试标题2", f"测试2失败: plot 应该等于 title"
        print("✓ 测试2通过: javscraperid 包含连字符时处理正确")
        
        # 验证测试文件 3 (普通番号)
        content3 = read_nfo_file(test3_path)
        expected_sorttitle3 = "XYZ-456"
        assert content3['sorttitle'] == expected_sorttitle3, f"测试3失败: sorttitle 应该是 {expected_sorttitle3}, 实际是 {content3['sorttitle']}"
        assert content3['originaltitle'] == f"{expected_sorttitle3} 测试标题3", f"测试3失败: originaltitle 格式错误"
        assert content3['plot'] == "测试标题3", f"测试3失败: plot 应该等于 title"
        print("✓ 测试3通过: 普通番号处理正确")
        
        # 验证 poster.jpg 复制为 fanart.jpg 和 thumb.jpg 的功能
        print("\n=== 验证 poster.jpg 复制功能 ===")
        fanart1_path = os.path.join(temp_dir, "fanart.jpg")
        fanart2_path = os.path.join(sub_dir, "fanart.jpg")
        thumb1_path = os.path.join(temp_dir, "thumb.jpg")
        thumb2_path = os.path.join(sub_dir, "thumb.jpg")
        
        assert os.path.exists(fanart1_path), f"fanart.jpg 文件未创建: {fanart1_path}"
        assert os.path.exists(fanart2_path), f"fanart.jpg 文件未创建: {fanart2_path}"
        assert os.path.exists(thumb1_path), f"thumb.jpg 文件未创建: {thumb1_path}"
        assert os.path.exists(thumb2_path), f"thumb.jpg 文件未创建: {thumb2_path}"
        
        # 验证文件内容是否相同
        with open(poster1_path, 'rb') as f1, open(fanart1_path, 'rb') as f2:
            assert f1.read() == f2.read(), "fanart.jpg 内容与 poster.jpg 不一致"
        
        with open(poster2_path, 'rb') as f1, open(fanart2_path, 'rb') as f2:
            assert f1.read() == f2.read(), "fanart.jpg 内容与 poster.jpg 不一致"
        
        with open(poster1_path, 'rb') as f1, open(thumb1_path, 'rb') as f2:
            assert f1.read() == f2.read(), "thumb.jpg 内容与 poster.jpg 不一致"
        
        with open(poster2_path, 'rb') as f1, open(thumb2_path, 'rb') as f2:
            assert f1.read() == f2.read(), "thumb.jpg 内容与 poster.jpg 不一致"
        
        print("✓ poster.jpg 复制为 fanart.jpg 和 thumb.jpg 功能正常")
        
        print("\n🎉 所有测试通过！")

if __name__ == "__main__":
    test_nfo_processing()