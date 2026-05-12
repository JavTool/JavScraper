'''
遍历指定目录中的媒体文件为其生成 emby 的 nfo 文件
例如：\\192.168.1.199\Porn\Fellatio Japan\Aoi Shino\Fellatio-Japan 227 Aoi Shino\Fellatio-Japan 227 Aoi Shino.mp4
获取该文件父目录的父目录名称即为演员名称“Aoi Shino”，“Aoi Shino”的上级目录“Fellatio Japan”为工作室名称
标题（title）为“Fellatio-Japan 227 Aoi Shino”
原始标题（originalTitle）为“Fellatio-Japan 227 Aoi Shino”
排序标题（sorttitle）为“Fellatio-Japan 227” 可通过正则提取成番号
年份、月份、日期 可根据用户指定填入，如果不指定则不处理
时长（runtime）为“120”
家长分级（mpaa）为“XXX”
tag 和 genre 添加分别一个 工作室名称

'''

import os
import re
import xml.etree.ElementTree as ET
from pathlib import Path

class EmbyTool:
    def __init__(self, directory, year=None, month=None, day=None, runtime=120, mpaa="XXX"):
        self.directory = Path(directory)
        self.year = year
        self.month = month
        self.day = day
        self.runtime = runtime
        self.mpaa = mpaa
        self.video_extensions = {'.mp4', '.avi', '.mkv', '.wmv', '.mov', '.flv'}

    def scan_and_generate_nfo(self):
        for root, dirs, files in os.walk(self.directory):
            for file in files:
                if Path(file).suffix.lower() in self.video_extensions:
                    file_path = Path(root) / file
                    self.generate_nfo_for_file(file_path)

    def generate_nfo_for_file(self, file_path):
        # 解析路径
        parts = file_path.parts
        if len(parts) < 4:
            return  # 不够深度，跳过

        # 假设路径结构：.../工作室/演员/文件名.mp4
        studio = parts[-4]  # 文件父目录的父目录
        actor = parts[-3]   # 文件父目录
        filename = file_path.stem  # 无扩展名文件名
        title = filename
        # 提取排序标题（番号）和演员名称，假设格式如 "Fellatio-Japan 227 Aoi Shino"
        match = re.match(r'^([A-Za-z-]+ \d+)\s+(.+)$', filename)
        if match:
            sort_title = match.group(1)
            title = match.group(2)
        else:
            sort_title = re.match(r'^([A-Za-z-]+-\d+)\s+(.+)$', filename).group(1) 
            title = re.match(r'^([A-Za-z-]+-\d+)\s+(.+)$', filename).group(2) 

        # 创建 nfo 文件路径
        nfo_path = file_path.with_suffix('.nfo')

        # 创建 XML 结构
        movie = ET.Element("movie")

        ET.SubElement(movie, "title").text = title
        ET.SubElement(movie, "originaltitle").text = filename
        ET.SubElement(movie, "sorttitle").text = sort_title

        if self.year:
            ET.SubElement(movie, "year").text = str(self.year)
        if self.month and self.day:
            premiered = f"{self.year:04d}-{self.month:02d}-{self.day:02d}" if self.year else f"{self.month:02d}-{self.day:02d}"
            ET.SubElement(movie, "premiered").text = premiered

        ET.SubElement(movie, "runtime").text = str(self.runtime)
        ET.SubElement(movie, "mpaa").text = self.mpaa

        # 添加 genre 和 tag
        ET.SubElement(movie, "genre").text = studio
        ET.SubElement(movie, "tag").text = studio

        # 添加演员
        actor_elem = ET.SubElement(movie, "actor")
        ET.SubElement(actor_elem, "name").text = actor

        # 写入文件
        tree = ET.ElementTree(movie)
        tree.write(nfo_path, encoding='utf-8', xml_declaration=True)

# 使用示例
if __name__ == "__main__":
    tool = EmbyTool(r"\\192.168.1.199\Porn\Uncensored\Tera Link", year=2023, month=10, day=15, runtime=120)
    tool.scan_and_generate_nfo()