import os
import re
import argparse
import time
import xml.etree.ElementTree as ET
from urllib.parse import quote
import requests
from bs4 import BeautifulSoup
from datetime import datetime
import shutil
from pathlib import Path


class JacketCopyTool:
    def __init__(self, log_file: str = "jacket_download.log"):
        """
        初始化下载器
        
        Args:
            log_file: 日志文件路径
        """
        self.log_file = log_file
        self.session = requests.Session()
        self.session.headers.update({
            'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36'
        })
    def log_message(self, message: str):
        """记录日志消息"""
        timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        log_entry = f"[{timestamp}] {message}\n"
        
        # 输出到控制台
        print(message)
        
        # 写入日志文件
        try:
            with open(self.log_file, 'a', encoding='utf-8') as f:
                f.write(log_entry)
        except Exception as e:
            print(f"写入日志文件失败: {e}")

    def copy_jacket(source_dir: str, dest_dir: str):
        """复制 jacket.jpg 到目标目录"""
        source_jacket = os.path.join(source_dir, "jacket.jpg")
        dest_jacket = os.path.join(dest_dir, "jacket.jpg")
        if os.path.exists(source_jacket):
            shutil.copy2(source_jacket, dest_jacket)
            print(f"复制 {source_jacket} 到 {dest_jacket}")
        else:
            print(f"源文件不存在: {source_jacket}")

    def extract_studio_from_nfo(self, nfo_path: str) -> str:
        """从 NFO 文件中提取工作室信息"""
        try:
            tree = ET.parse(nfo_path)
            root = tree.getroot()
            
            # 查找 studio 字段
            studio_elem = root.find('studio')
            if studio_elem is not None and studio_elem.text:
                return studio_elem.text.strip()
                
        except Exception as e:
            self.log_message(f"提取工作室失败: {nfo_path}, 错误: {e}")
        
        return ""

    def process_directory(self, directory: str) -> dict:
        """处理指定目录下的所有 NFO 文件"""
        self.log_message(f"开始处理目录: {directory}")

        stats = {
            'total_nfo': 0,
            'caribbean_nfo': 0,
            'success_download': 0,
            'failed_download': 0,
            'already_exists': 0
        }

        try:
            # 递归遍历目录
            for root, dirs, files in os.walk(directory):
                for file in files:
                    if file.lower().endswith('.nfo'):
                        nfo_path = os.path.join(root, file)
                        stats['total_nfo'] += 1
                        
                        # 检查工作室
                        studio = self.extract_studio_from_nfo(nfo_path)
                        if studio == "カリビアンコム":
                            stats['caribbean_nfo'] += 1
                            
                            # 检查是否已存在 jacket.jpg
                            jacket_path = os.path.join(root, "jacket.jpg")
                            if os.path.exists(jacket_path):
                                # 复制 jacket.jpg 为 "文件夹名称-poster.jpg"
                                folder_name = os.path.basename(root)
                                poster_filename = f"{folder_name}-poster.jpg"
                                poster_path = os.path.join(root, poster_filename)
                                
                                try:
                                    shutil.copy2(jacket_path, poster_path)
                                    self.log_message(f"已复制 jacket.jpg 为 {poster_filename} 在目录: {root}")
                                except Exception as copy_error:
                                    self.log_message(f"复制文件失败: {copy_error}")
                                
                                stats['already_exists'] += 1
                                continue

        
        except Exception as e:
            self.log_message(f"处理目录时出错: {e}")

def main():
    """主函数"""
    FIXED_DIRECTORY = r"\\192.168.1.199\Porn\Uncensored"
    # parser = argparse.ArgumentParser(description='Caribbean.com 封面图片下载器')
    # parser.add_argument('directory',default='\\192.168.1.199\Porn\Uncensored\波多野結衣\[2013-12-27] - [122713-508] - [半沢直美 〜痴女の10倍返し!〜前編〜]', help='要处理的目录路径（支持网络路径）')
    # parser.add_argument('--log', default='jacket_download.log', help='日志文件路径')
    
    # args = parser.parse_args()
    
    # 检查目录是否存在
    if not os.path.exists(FIXED_DIRECTORY):
        print(f"错误: 目录不存在: {FIXED_DIRECTORY}")
        return
    
    # 创建下载器并处理目录
    downloader = JacketCopyTool('jacket_download.log')
    downloader.process_directory(FIXED_DIRECTORY)


if __name__ == "__main__":
    main()
