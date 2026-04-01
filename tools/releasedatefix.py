#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
遍历指定目录（需要支持网络路径，并递归遍历子目录）下的所有 nfo 文件
根据番号从 avbase 中搜索并提取详情页面的日期
并更新 nfo 中的 releasedate、tagline、year
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


class AVBaseReleaseUpdater:
    """AVBase 发行日期更新工具"""
    
    def __init__(self):
        self.session = requests.Session()
        self.session.headers.update({
            'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/90.0.4430.212 Safari/537.36',
            'Accept-Language': 'zh-CN,zh;q=0.9,en;q=0.8',
            'Referer': 'https://www.avbase.com/'
        })
        
    def extract_number_from_nfo(self, nfo_path: str) -> str:
        """从 NFO 文件中提取番号"""
        try:
            tree = ET.parse(nfo_path)
            root = tree.getroot()
            
            # 尝试多个字段提取番号
            for field in ['sorttitle', 'originaltitle', 'title']:
                elem = root.find(field)
                if elem is not None and elem.text:
                    # 提取番号：支持 ABC-123 或 ABC123 格式
                    text = elem.text.strip()
                    # 匹配字母-数字或字母数字组合
                    match = re.search(r'([A-Za-z]+[-]?\d+)', text)
                    if match:
                        return match.group(1)
            
            # 如果 NFO 中没有找到，尝试从文件名提取
            filename = os.path.basename(nfo_path)
            match = re.search(r'([A-Za-z]+[-]?\d+)', filename)
            if match:
                return match.group(1)
                
        except Exception as e:
            print(f"提取番号失败: {nfo_path}, 错误: {e}")
        
        return ""
    
    def search_avbase(self, keyword: str) -> str:
        """在 AVBase 中搜索视频并返回详情页 URL"""
        try:
            search_url = f"https://www.avbase.net/works?q={quote(keyword)}"
            print(f"搜索 URL: {search_url}")
            
            response = self.session.get(search_url, timeout=10)
            response.raise_for_status()
            
            soup = BeautifulSoup(response.content, 'html.parser')
            
            # 查找搜索结果链接
            search_results = soup.select('div.grow.flex.flex-col.border-l.border-light a')
            
            if search_results:
                href = search_results[0].get('href')
                if href:
                    # 确保是完整 URL
                    if href.startswith('/'):
                        href = 'https://www.avbase.net' + href
                    print(f"找到详情页: {href}")
                    return href
            
            print(f"未找到 {keyword} 的搜索结果")
            return ""
            
        except Exception as e:
            print(f"搜索失败: {keyword}, 错误: {e}")
            return ""
    
    def parse_detail_page(self, url: str) -> dict:
        """解析 AVBase 详情页面获取日期信息"""
        try:
            print(f"解析详情页: {url}")
            response = self.session.get(url, timeout=10)
            response.raise_for_status()
            
            soup = BeautifulSoup(response.content, 'html.parser')
            
            # 检查标题是否存在
            title_node = soup.select_one('h1.text-lg')
            if not title_node:
                print("页面格式异常，未找到标题")
                return {}
            
            # 解析信息字段
            info_dict = {}
            info_nodes = soup.select('div.w-full div.bg-base-100')
            
            for node in info_nodes:
                label_node = node.select_one('div.text-xs')
                value_node = node.select_one('div.text-sm')
                
                if label_node and value_node:
                    key = label_node.get_text(strip=True)
                    value = value_node.get_text(strip=True)
                    info_dict[key] = value
            
            # 提取发售日期（支持多种日期标签）
            release_date = ""
            for key, value in info_dict.items():
                if any(keyword in key for keyword in ["発売日", "发售日", "Release", "配信開始日", "配信开始日", "Stream Start"]):
                    release_date = value
                    break
            
            result = {
                'title': title_node.get_text(strip=True),
                'release_date': release_date,
                'info_dict': info_dict
            }
            
            print(f"解析结果: 标题={result['title']}, 发售日期={result['release_date']}")
            return result
            
        except Exception as e:
            print(f"解析详情页失败: {url}, 错误: {e}")
            return {}
    
    def parse_date(self, date_str: str) -> tuple:
        """解析日期字符串返回 (year, formatted_date)"""
        if not date_str:
            return "", ""
        
        try:
            # 尝试多种日期格式
            date_formats = [
                '%Y-%m-%d',
                '%Y/%m/%d', 
                '%Y.%m.%d',
                '%Y年%m月%d日',
                '%m/%d/%Y',
                '%d/%m/%Y'
            ]
            
            for fmt in date_formats:
                try:
                    parsed_date = datetime.strptime(date_str, fmt)
                    formatted_date = parsed_date.strftime('%Y-%m-%d')
                    year = str(parsed_date.year)
                    return year, formatted_date
                except ValueError:
                    continue
            
            # 如果都无法解析，尝试提取年份
            year_match = re.search(r'(\d{4})', date_str)
            if year_match:
                year = year_match.group(1)
                return year, date_str
            
        except Exception as e:
            print(f"日期解析失败: {date_str}, 错误: {e}")
        
        return "", date_str
    
    def update_nfo_release_info(self, nfo_path: str, release_date: str, title: str = "") -> bool:
        """更新 NFO 文件中的发行日期相关信息"""
        try:
            tree = ET.parse(nfo_path)
            root = tree.getroot()
            
            year, formatted_date = self.parse_date(release_date)
            
            # 同步或创建 premiered 字段，与 releasedate 保持一致
            premiered_elem = root.find('premiered')
            if premiered_elem is None:
                premiered_elem = ET.SubElement(root, 'premiered')
            premiered_elem.text = formatted_date
            
            # 更新或创建 releasedate 字段
            releasedate_elem = root.find('releasedate')
            if releasedate_elem is None:
                releasedate_elem = ET.SubElement(root, 'releasedate')
            releasedate_elem.text = formatted_date
            
            # 更新或创建 year 字段
            if year:
                year_elem = root.find('year')
                if year_elem is None:
                    year_elem = ET.SubElement(root, 'year')
                year_elem.text = year
            
            # 更新或创建 tagline 字段（可选，使用标题或日期）
            tagline_elem = root.find('tagline')
            if tagline_elem is None:
                tagline_elem = ET.SubElement(root, 'tagline')
            
            # 如果 tagline 为空，设置为发行日期
            if not tagline_elem.text or not tagline_elem.text.strip():
                tagline_text = title if title else f"发行日期: {formatted_date}"
                tagline_elem.text = tagline_text
            else:
                # 当 tagline 已存在时，如果其中包含日期或标签，则将日期标准化为 formatted_date
                orig_tagline = tagline_elem.text or ""
                updated_tagline = orig_tagline
                label_keywords = [
                    "発売日", "发售日", "配信開始日", "配信开始日", "Release", "Stream Start", "发行日期", "发行日"
                ]
                date_patterns = [
                    r"(\d{4})[./-](\d{1,2})[./-](\d{1,2})",
                    r"(\d{4})年(\d{1,2})月(\d{1,2})日?"
                ]
                replaced = False
                for pat in date_patterns:
                    if re.search(pat, updated_tagline):
                        updated_tagline = re.sub(pat, formatted_date, updated_tagline, count=1)
                        replaced = True
                        break
                if not replaced and any(lbl in orig_tagline for lbl in label_keywords):
                    for lbl in label_keywords:
                        if lbl in orig_tagline:
                            updated_tagline = f"{lbl} {formatted_date}"
                            break
                if updated_tagline != orig_tagline:
                    tagline_elem.text = updated_tagline
            
            # 更新 outline 字段中的日期（若存在），使其与 releasedate 保持一致
            outline_elem = root.find('outline')
            if outline_elem is not None and formatted_date:
                orig_outline = outline_elem.text or ""
                updated_outline = orig_outline
                # 支持的标签关键词
                label_keywords = [
                    "発売日", "发售日", "配信開始日", "配信开始日", "Release", "Stream Start", "发行日期", "发行日"
                ]
                contains_label = any(lbl in orig_outline for lbl in label_keywords)
                # 替换常见日期格式为 YYYY-MM-DD
                date_patterns = [
                    r"(\d{4})[./-](\d{1,2})[./-](\d{1,2})",
                    r"(\d{4})年(\d{1,2})月(\d{1,2})日?"
                ]
                replaced = False
                for pat in date_patterns:
                    if re.search(pat, updated_outline):
                        updated_outline = re.sub(pat, formatted_date, updated_outline, count=1)
                        replaced = True
                        break
                # 如果包含标签但没有日期，则用标签 + 格式化日期填充
                if not replaced and contains_label:
                    for lbl in label_keywords:
                        if lbl in orig_outline:
                            updated_outline = f"{lbl} {formatted_date}"
                            break
                if updated_outline != orig_outline:
                    outline_elem.text = updated_outline
            
            # 备份原文件
            backup_path = nfo_path + '.bak'
            if not os.path.exists(backup_path):
                with open(nfo_path, 'rb') as src, open(backup_path, 'wb') as dst:
                    dst.write(src.read())
            
            # 写入更新后的内容
            tree.write(nfo_path, encoding='utf-8', xml_declaration=True)
            
            print(f"✓ 已更新: {nfo_path}")
            print(f"  发行日期: {formatted_date}")
            if year:
                print(f"  年份: {year}")
            
            return True
            
        except Exception as e:
            print(f"更新 NFO 失败: {nfo_path}, 错误: {e}")
            return False
    
    def update_nfo_metatube_ids(self, nfo_path: str, avbase_url: str, enable_fix: bool = True) -> bool:
        """检查并修复 NFO 文件中的 uniqueid 和 metatubeid 字段，使其与 AVBASE URL 一致
        
        Args:
            nfo_path: NFO 文件路径
            avbase_url: AVBASE 详情页 URL
            enable_fix: 是否启用修复功能
            
        Returns:
            bool: 是否需要修复或已修复成功
        """
        try:
            tree = ET.parse(nfo_path)
            root = tree.getroot()
            
            # 从 AVBASE URL 提取 studio:code 部分
            # 例如: https://www.avbase.net/works/sodcreate:START-273 -> sodcreate:START-273
            avbase_id = self._extract_avbase_id_from_url(avbase_url)
            if not avbase_id:
                print(f"无法从 URL 提取 AVBASE ID: {avbase_url}")
                return False
            
            # 组装 metatube ID，对冒号进行 URL 编码
            encoded_avbase_id = avbase_id.replace(":", "%3A")
            metatube_id = f"AVBASE:{encoded_avbase_id}"
            
            needs_fix = False
            
            # 检查 uniqueid 字段
            uniqueid_elem = root.find('.//uniqueid[@type="metatube"]')
            if uniqueid_elem is None:
                print(f"缺少 uniqueid[type=metatube] 字段")
                needs_fix = True
            elif uniqueid_elem.text != metatube_id:
                print(f"uniqueid 不一致: 当前={uniqueid_elem.text}, 期望={metatube_id}")
                needs_fix = True
            
            # 检查 metatubeid 字段
            metatubeid_elem = root.find('metatubeid')
            if metatubeid_elem is None:
                print(f"缺少 metatubeid 字段")
                needs_fix = True
            elif metatubeid_elem.text != metatube_id:
                print(f"metatubeid 不一致: 当前={metatubeid_elem.text}, 期望={metatube_id}")
                needs_fix = True
            
            if not needs_fix:
                print(f"✓ metatube ID 字段已正确: {metatube_id}")
                return False
            
            if not enable_fix:
                print(f"检测到不一致但未启用修复功能")
                return True
            
            # 执行修复
            # 修复 uniqueid
            if uniqueid_elem is None:
                uniqueid_elem = ET.SubElement(root, 'uniqueid')
                uniqueid_elem.set('type', 'metatube')
            uniqueid_elem.text = metatube_id
            
            # 修复 metatubeid
            if metatubeid_elem is None:
                metatubeid_elem = ET.SubElement(root, 'metatubeid')
            metatubeid_elem.text = metatube_id
            
            # 备份原文件
            backup_path = nfo_path + '.bak'
            if not os.path.exists(backup_path):
                with open(nfo_path, 'rb') as src, open(backup_path, 'wb') as dst:
                    dst.write(src.read())
            
            # 写入更新后的内容
            tree.write(nfo_path, encoding='utf-8', xml_declaration=True)
            
            print(f"✓ 已修复 metatube ID: {metatube_id}")
            return True
            
        except Exception as e:
            print(f"更新 metatube ID 失败: {nfo_path}, 错误: {e}")
            return False
    
    def _extract_avbase_id_from_url(self, avbase_url: str) -> str:
        """从 AVBASE URL 提取 studio:code 部分
        
        Args:
            avbase_url: AVBASE 详情页 URL，例如 https://www.avbase.net/works/sodcreate:START-273
            
        Returns:
            str: studio:code 部分，例如 sodcreate:START-273
        """
        try:
            # 匹配 URL 模式: https://www.avbase.net/works/{studio:code}
            # 支持格式如: sodcreate:START-273 或 prestige:ABW-230
            pattern = r'https://www\.avbase\.net/works/(.+?)(?:\?|$)'
            match = re.search(pattern, avbase_url)
            
            if match:
                studio_code = match.group(1)
                return studio_code
            
            print(f"无法解析 AVBASE URL 格式: {avbase_url}")
            return ""
            
        except Exception as e:
            print(f"提取 metatube ID 失败: {e}")
            return ""
    
    def process_nfo_file(self, nfo_path: str, fix_metatube_ids: bool = False) -> bool:
        """处理单个 NFO 文件
        
        Args:
            nfo_path: NFO 文件路径
            fix_metatube_ids: 是否启用 metatube ID 修复功能
        """
        print(f"\n处理文件: {nfo_path}")
        
        # 1. 提取番号
        number = self.extract_number_from_nfo(nfo_path)
        if not number:
            print(f"无法提取番号，跳过: {nfo_path}")
            return False
        
        print(f"提取到番号: {number}")
        
        # 2. 搜索 AVBase
        detail_url = self.search_avbase(number)
        if not detail_url:
            print(f"未找到搜索结果，跳过: {number}")
            return False
        
        # 3. 解析详情页
        detail_info = self.parse_detail_page(detail_url)
        if not detail_info.get('release_date'):
            print(f"未找到发行日期信息，跳过: {detail_url}")
            return False
        
        # 4. 更新 NFO
        success = self.update_nfo_release_info(
            nfo_path, 
            detail_info['release_date'], 
            detail_info.get('title', '')
        )
        
        # 5. 检查并修复 metatube ID（可选）
        if fix_metatube_ids:
            self.update_nfo_metatube_ids(nfo_path, detail_url, enable_fix=True)
        else:
            # 仅检查不修复
            self.update_nfo_metatube_ids(nfo_path, detail_url, enable_fix=False)
        
        # 添加延迟避免请求过快
        time.sleep(1)
        
        return success
    
    def process_directory(self, directory_path: str, fix_metatube_ids: bool = False) -> dict:
        """处理目录中的所有 NFO 文件"""
        print(f"开始处理目录: {directory_path}")
        
        # 保留原始路径，避免使用 abspath 破坏 UNC 网络路径
        base_path = os.path.normpath(directory_path) if directory_path else directory_path
        if base_path and base_path.startswith('\\\\'):
            print("检测到网络路径 (UNC): 将直接遍历共享目录")
        
        if not os.path.exists(base_path):
            print(f"错误: 目录不存在 - {base_path}")
            return {'total': 0, 'success': 0, 'failed': 0, 'files': []}
        
        stats = {'total': 0, 'success': 0, 'failed': 0, 'files': []}
        
        # 递归遍历目录（直接使用原始/UNC 路径）
        for cur_root, dirs, files in os.walk(base_path):
            for file in files:
                if file.lower().endswith('.nfo'):
                    nfo_path = os.path.join(cur_root, file)
                    stats['total'] += 1
                    stats['files'].append(nfo_path)
                    
                    try:
                        success = self.process_nfo_file(nfo_path, fix_metatube_ids)
                        if success:
                            stats['success'] += 1
                        else:
                            stats['failed'] += 1
                    except Exception as e:
                        print(f"处理文件异常: {nfo_path}, 错误: {e}")
                        stats['failed'] += 1
        
        # 输出统计信息
        print(f"\n处理完成!")
        print(f"总文件数: {stats['total']}")
        print(f"成功更新: {stats['success']}")
        print(f"失败/跳过: {stats['failed']}")
        
        return stats


def main():
    """主函数"""
    # 固定目录设置
    FIXED_DIRECTORY = r"\\192.168.1.199\Porn\Censored"
    
    # parser = argparse.ArgumentParser(description='批量更新 NFO 文件的发行日期信息')
    # parser.add_argument('--fix-metatube-ids', action='store_true', help='启用 metatube ID 修复功能')
    # parser.add_argument('--dry-run', action='store_true', help='仅测试不实际修改文件')
    
    # args = parser.parse_args()
    
    print(f"处理目录: {FIXED_DIRECTORY}")
    
    # if args.dry_run:
    #     print("*** DRY RUN 模式 - 不会修改任何文件 ***")
    
    # if args.fix_metatube_ids:
    #     print("*** 已启用 metatube ID 修复功能 ***")
    # else:
    #     print("*** metatube ID 仅检查不修复（使用 --fix-metatube-ids 启用修复）***")

    updater = AVBaseReleaseUpdater()
    
    try:
        # stats = updater.process_directory(FIXED_DIRECTORY, fix_metatube_ids = False)
        stats = updater.process_directory(FIXED_DIRECTORY, fix_metatube_ids = True)
        
        if stats['total'] == 0:
            print("未找到任何 .nfo 文件")
        
    except KeyboardInterrupt:
        print("\n用户中断操作")
    except Exception as e:
        print(f"程序执行出错: {e}")


if __name__ == "__main__":
    main()