#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
遍历指定目录（需要支持网络路径，并递归遍历子目录）下的所有 nfo 文件
读取 nfo 中的 originaltitle、sorttitle 和 studio 字段，从 originaltitle、sorttitle 提取番号，从 studio 提取工作室 ，若工作室为 "一本道" 则继续执行以下操作
根据番号从 1pondo 中 下载封面图片
并保存到该目录下的 jacket.jpg 文件，复制为 folder.jpg 和 poster.jpg
若不存在 jacket.jpg 文件，则记录了到指定日志文件中

"""

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


class JacketDownloader:
    """Caribbean.com 封面图片下载器"""
    
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
    
    def extract_number_from_nfo(self, nfo_path: str) -> str:
        """从 NFO 文件中提取番号"""
        try:
            tree = ET.parse(nfo_path)
            root = tree.getroot()
            
            # 尝试从 originaltitle 和 sorttitle 字段提取番号
            for field in ['originaltitle', 'sorttitle']:
                elem = root.find(field)
                if elem is not None and elem.text:
                    text = elem.text.strip()
                    # 提取番号：支持 123456-789 格式（Caribbean.com 格式）
                    match = re.search(r'(\d{6}_\d{3})', text)
                    if match:
                        return match.group(1)
                    
                    # 也支持其他格式作为备选
                    match = re.search(r'([A-Za-z]+[-]?\d+)', text)
                    if match:
                        return match.group(1)
            
            # 如果 NFO 中没有找到，尝试从文件名提取
            filename = os.path.basename(nfo_path)
            match = re.search(r'(\d{6}_\d{3})', filename)
            if match:
                return match.group(1)
                
        except Exception as e:
            self.log_message(f"提取番号失败: {nfo_path}, 错误: {e}")
        
        return ""
    
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
    
    def download_caribbean_jacket(self, number: str, save_dir: str) -> bool:
        """从 1pondo 下载封面图片"""
        try:
            # 1pondo 封面图片 URL 格式
            # 例如：123456-789 -> https://www.1pondo.tv/dyn/dla/images/movies/050825_001/jacket/jacket.jpg
            jacket_url = f"https://www.1pondo.tv/dyn/dla/images/movies/{number}/jacket/jacket.jpg"
            
            self.log_message(f"尝试下载封面: {jacket_url}")
            
            # 下载图片
            response = self.session.get(jacket_url, timeout=30)
            response.raise_for_status()
            
            # 检查是否是有效的图片内容
            if len(response.content) < 1000:  # 太小可能是错误页面
                self.log_message(f"下载的图片文件太小，可能无效: {len(response.content)} bytes")
                return False
            
            # 保存为 jacket.jpg
            jacket_path = os.path.join(save_dir, "jacket.jpg")
            with open(jacket_path, 'wb') as f:
                f.write(response.content)
            
            self.log_message(f"成功下载封面: {jacket_path}")
            
            # 复制为 folder.jpg 和 poster.jpg
            folder_path = os.path.join(save_dir, "folder.jpg")
            poster_path = os.path.join(save_dir, "poster.jpg")
            
            try:
                shutil.copy2(jacket_path, folder_path)
                self.log_message(f"复制封面为: {folder_path}")
            except Exception as e:
                self.log_message(f"复制 folder.jpg 失败: {e}")
            
            try:
                shutil.copy2(jacket_path, poster_path)
                self.log_message(f"复制封面为: {poster_path}")
            except Exception as e:
                self.log_message(f"复制 poster.jpg 失败: {e}")
            
            folder_name = os.path.basename(save_dir)
            poster_filename = f"{folder_name}-poster.jpg"
            poster_path = os.path.join(save_dir, poster_filename)
            
            try:
                shutil.copy2(jacket_path, poster_path)
                self.log_message(f"已复制 jacket.jpg 为 {poster_filename} 在目录: {save_dir}")
            except Exception as copy_error:
                self.log_message(f"复制文件失败: {copy_error}")

            return True
            
        except requests.exceptions.RequestException as e:
            self.log_message(f"下载封面失败 (网络错误): {number}, 错误: {e}")
            return False
        except Exception as e:
            self.log_message(f"下载封面失败: {number}, 错误: {e}")
            return False
    
    def process_nfo_file(self, nfo_path: str) -> bool:
        """处理单个 NFO 文件"""
        self.log_message(f"处理文件: {nfo_path}")
        
        # 1. 提取工作室信息
        studio = self.extract_studio_from_nfo(nfo_path)
        if not studio:
            self.log_message(f"无法提取工作室信息，跳过: {nfo_path}")
            return False
        
        # 2. 检查是否为 1pondo
        if studio != "一本道":
            self.log_message(f"工作室不是 一本道 ({studio})，跳过: {nfo_path}")
            return False
        
        # 3. 提取番号
        number = self.extract_number_from_nfo(nfo_path)
        if not number:
            self.log_message(f"无法提取番号，跳过: {nfo_path}")
            return False
        
        self.log_message(f"提取到番号: {number}, 工作室: {studio}")
        
        # 4. 检查是否已存在 jacket.jpg
        nfo_dir = os.path.dirname(nfo_path)
        jacket_path = os.path.join(nfo_dir, "jacket.jpg")
        
        if os.path.exists(jacket_path):
            self.log_message(f"jacket.jpg 已存在，跳过下载: {jacket_path}")
            return True
        
        # 5. 下载封面图片
        success = self.download_caribbean_jacket(number, nfo_dir)
        
        if not success:
            self.log_message(f"下载失败，记录到日志: {nfo_path}")
        
        # 添加延迟避免请求过快
        time.sleep(1)
        
        return success
    
    def process_directory(self, directory: str) -> dict:
        """处理指定目录下的所有 NFO 文件"""
        self.log_message(f"开始处理目录: {directory}")
        
        stats = {
            'total_nfo': 0,
            'onepondo_nfo': 0,
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
                        if studio == "一本道":
                            stats['onepondo_nfo'] += 1
                            
                            # 检查是否已存在 jacket.jpg
                            jacket_path = os.path.join(root, "jacket.jpg")
                            if os.path.exists(jacket_path):
                                stats['already_exists'] += 1
                                continue
                            
                            # 处理文件
                            if self.process_nfo_file(nfo_path):
                                stats['success_download'] += 1
                            else:
                                stats['failed_download'] += 1
        
        except Exception as e:
            self.log_message(f"处理目录时出错: {e}")
        
        # 输出统计信息
        self.log_message("\n=== 处理完成 ===")
        self.log_message(f"总 NFO 文件数: {stats['total_nfo']}")
        self.log_message(f"一本道 NFO 文件数: {stats['onepondo_nfo']}")
        self.log_message(f"已存在封面的文件数: {stats['already_exists']}")
        self.log_message(f"成功下载封面数: {stats['success_download']}")
        self.log_message(f"下载失败数: {stats['failed_download']}")
        
        return stats


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
    downloader = JacketDownloader('jacket_download.log')
    downloader.process_directory(FIXED_DIRECTORY)


if __name__ == "__main__":
    main()
