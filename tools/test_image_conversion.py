#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
测试图片格式转换功能的演示脚本

这个脚本演示了 madouposter.py 中新增的图片格式转换功能：
1. 创建不同格式的测试图片
2. 测试格式转换功能
3. 验证复制功能
"""

import os
import tempfile
import shutil
from madouposter import copy_poster_to_fanart

def create_test_image(file_path, format_type, color='blue'):
    """
    创建测试图片文件
    
    参数:
        file_path (str): 图片文件路径
        format_type (str): 图片格式 ('jpg', 'png', 'webp')
        color (str): 图片颜色
    """
    try:
        from PIL import Image
        
        # 创建一个简单的彩色图片
        img = Image.new('RGB', (200, 300), color=color)
        
        if format_type.lower() == 'png':
            # 为 PNG 添加透明度测试
            img = img.convert('RGBA')
            # 在图片中央添加一个透明区域
            for x in range(50, 150):
                for y in range(100, 200):
                    img.putpixel((x, y), (255, 255, 255, 0))  # 透明像素
        
        img.save(file_path)
        print(f"✓ 创建测试图片: {file_path}")
        return True
        
    except ImportError:
        print("❌ 需要安装 Pillow 库: pip install Pillow")
        return False
    except Exception as e:
        print(f"❌ 创建图片失败: {e}")
        return False

def create_test_nfo(file_path):
    """
    创建测试 nfo 文件
    
    参数:
        file_path (str): nfo 文件路径
    """
    nfo_content = '''<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<movie>
    <plot>测试剧情</plot>
    <title>测试标题</title>
    <originaltitle>原始标题</originaltitle>
    <sorttitle>TEST-001</sorttitle>
    <javscraperid>TEST001</javscraperid>
</movie>'''
    
    with open(file_path, 'w', encoding='utf-8') as f:
        f.write(nfo_content)
    print(f"✓ 创建测试 nfo 文件: {file_path}")

def test_image_conversion():
    """
    测试图片格式转换功能
    """
    print("=== 图片格式转换功能测试 ===")
    
    # 创建临时目录
    with tempfile.TemporaryDirectory() as temp_dir:
        print(f"\n测试目录: {temp_dir}")
        
        # 测试不同格式的图片转换
        test_formats = [
            ('png', 'red'),
            ('webp', 'green'),
            ('jpg', 'blue')
        ]
        
        for format_type, color in test_formats:
            print(f"\n--- 测试 {format_type.upper()} 格式 ---")
            
            # 清理之前的文件
            for file in os.listdir(temp_dir):
                if file.startswith(('poster', 'fanart', 'thumb')):
                    os.remove(os.path.join(temp_dir, file))
            
            # 创建测试文件
            poster_file = os.path.join(temp_dir, f'poster.{format_type}')
            nfo_file = os.path.join(temp_dir, 'test.nfo')
            
            if not create_test_image(poster_file, format_type, color):
                continue
            
            create_test_nfo(nfo_file)
            
            # 执行图片处理
            print("\n执行图片处理...")
            copy_poster_to_fanart(nfo_file)
            
            # 验证结果
            poster_jpg = os.path.join(temp_dir, 'poster.jpg')
            fanart_jpg = os.path.join(temp_dir, 'fanart.jpg')
            thumb_jpg = os.path.join(temp_dir, 'thumb.jpg')
            
            results = []
            results.append(f"poster.jpg 存在: {os.path.exists(poster_jpg)}")
            results.append(f"fanart.jpg 存在: {os.path.exists(fanart_jpg)}")
            results.append(f"thumb.jpg 存在: {os.path.exists(thumb_jpg)}")
            
            if format_type != 'jpg':
                original_exists = os.path.exists(poster_file)
                results.append(f"原 {format_type} 文件已删除: {not original_exists}")
            
            print("\n验证结果:")
            for result in results:
                print(f"  ✓ {result}")
            
            # 验证文件大小
            if all(os.path.exists(f) for f in [poster_jpg, fanart_jpg, thumb_jpg]):
                sizes = [os.path.getsize(f) for f in [poster_jpg, fanart_jpg, thumb_jpg]]
                if len(set(sizes)) == 1:
                    print(f"  ✓ 三个文件大小一致: {sizes[0]} 字节")
                else:
                    print(f"  ❌ 文件大小不一致: {sizes}")
        
        print("\n=== 测试完成 ===")

if __name__ == "__main__":
    test_image_conversion()