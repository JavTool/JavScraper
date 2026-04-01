#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
测试网络路径支持
"""

import os
from madouposter import process_nfo_files

def test_network_path_support():
    """
    测试网络路径支持
    """
    # 测试网络路径
    network_path = r"F:\111\333\downloaded"
    
    print(f"测试网络路径: {network_path}")
    
    # 检查路径是否存在
    if os.path.exists(network_path):
        print("✓ 网络路径可访问")
        
        # 测试 os.walk 是否能正常遍历网络路径
        try:
            file_count = 0
            nfo_count = 0
            
            for root, dirs, files in os.walk(network_path):
                for file in files:
                    file_count += 1
                    if file.lower().endswith('.nfo'):
                        nfo_count += 1
                        print(f"找到 nfo 文件: {os.path.join(root, file)}")
                    
                    # 限制输出，避免过多文件
                    if file_count > 20:
                        break
                
                if file_count > 20:
                    break
            
            print(f"总文件数: {file_count} (最多显示 20 个)")
            print(f"nfo 文件数: {nfo_count}")
            
            if nfo_count > 0:
                print("✓ 网络路径中找到 nfo 文件，可以进行处理")
                
                # 询问是否要实际处理文件
                response = input("是否要实际处理这些 nfo 文件？(y/N): ")
                if response.lower() == 'y':
                    process_nfo_files(network_path)
                else:
                    print("跳过实际处理")
            else:
                print("网络路径中未找到 nfo 文件")
                
        except Exception as e:
            print(f"遍历网络路径时出错: {e}")
            
    else:
        print("✗ 网络路径不可访问")
        print("可能的原因:")
        print("1. 网络连接问题")
        print("2. 权限不足")
        print("3. 路径不存在")
        print("4. 需要提供网络凭据")

if __name__ == "__main__":
    test_network_path_support()