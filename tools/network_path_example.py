#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
网络路径使用示例
"""

import os
import argparse
from madouposter import process_nfo_files

def validate_network_path(network_path):
    """
    验证网络路径是否可访问
    
    参数:
        network_path (str): 网络路径
        
    返回:
        bool: 路径是否可访问
    """
    print(f"验证网络路径: {network_path}")
    
    if not network_path.startswith(r"\\"):
        print("✗ 错误: 不是有效的 UNC 网络路径格式")
        print("正确格式: \\\\服务器IP\\共享文件夹\\路径")
        return False
    
    if not os.path.exists(network_path):
        print("✗ 网络路径不可访问")
        print("可能的原因:")
        print("  1. 网络连接问题")
        print("  2. 权限不足")
        print("  3. 路径不存在")
        print("  4. 需要提供网络凭据")
        return False
    
    print("✓ 网络路径可访问")
    return True

def preview_network_path(network_path, max_files=10):
    """
    预览网络路径中的文件
    
    参数:
        network_path (str): 网络路径
        max_files (int): 最大显示文件数
    """
    print(f"\n预览网络路径内容 (最多显示 {max_files} 个文件):")
    
    try:
        file_count = 0
        nfo_count = 0
        
        for root, dirs, files in os.walk(network_path):
            for file in files:
                file_count += 1
                file_path = os.path.join(root, file)
                
                if file.lower().endswith('.nfo'):
                    nfo_count += 1
                    print(f"  📄 NFO: {file_path}")
                elif file.lower().endswith('.jpg'):
                    print(f"  🖼️  IMG: {file_path}")
                else:
                    print(f"  📁 FILE: {file_path}")
                
                if file_count >= max_files:
                    break
            
            if file_count >= max_files:
                break
        
        print(f"\n统计信息:")
        print(f"  总文件数: {file_count} (显示前 {min(file_count, max_files)} 个)")
        print(f"  NFO 文件数: {nfo_count}")
        
        return nfo_count > 0
        
    except Exception as e:
        print(f"✗ 预览网络路径时出错: {e}")
        return False

def process_network_path(network_path, dry_run=False):
    """
    处理网络路径中的 nfo 文件
    
    参数:
        network_path (str): 网络路径
        dry_run (bool): 是否为试运行模式
    """
    if not validate_network_path(network_path):
        return False
    
    has_nfo = preview_network_path(network_path)
    
    if not has_nfo:
        print("\n✗ 网络路径中未找到 nfo 文件")
        return False
    
    if dry_run:
        print("\n🔍 试运行模式，不会实际修改文件")
        return True
    
    # 确认是否要处理
    print("\n⚠️  注意: 此操作将直接修改网络路径中的 nfo 文件")
    response = input("是否继续处理？(y/N): ")
    
    if response.lower() != 'y':
        print("操作已取消")
        return False
    
    print("\n开始处理网络路径中的 nfo 文件...")
    try:
        process_nfo_files(network_path)
        print("\n✅ 网络路径处理完成")
        return True
    except Exception as e:
        print(f"\n✗ 处理网络路径时出错: {e}")
        return False

def main():
    """
    主函数
    """
    parser = argparse.ArgumentParser(
        description='处理网络路径中的 nfo 文件',
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
示例用法:
  python network_path_example.py "\\\\192.168.1.199\\Jav\\China\\果冻传媒\\何苗"
  python network_path_example.py "\\\\192.168.1.199\\Jav\\China\\果冻传媒\\何苗" --dry-run
  python network_path_example.py "\\\\192.168.1.199\\Jav\\China\\果冻传媒\\何苗" --preview-only
        """
    )
    
    parser.add_argument('network_path', help='网络路径 (UNC 格式)')
    parser.add_argument('--dry-run', action='store_true', help='试运行模式，不实际修改文件')
    parser.add_argument('--preview-only', action='store_true', help='仅预览，不处理文件')
    parser.add_argument('--max-preview', type=int, default=10, help='预览时最大显示文件数 (默认: 10)')
    
    args = parser.parse_args()
    
    print("=== 网络路径 NFO 文件处理工具 ===")
    print(f"目标路径: {args.network_path}")
    
    if args.preview_only:
        if validate_network_path(args.network_path):
            preview_network_path(args.network_path, args.max_preview)
    else:
        process_network_path(args.network_path, args.dry_run)

if __name__ == "__main__":
    main()