import os
import re
import requests
from bs4 import BeautifulSoup
from typing import List, Optional, Dict
from urllib.parse import urljoin


class SubtitleCat:
    """字幕猫网站字幕下载工具"""
    
    BASE_URL = "https://subtitlecat.com"
    SEARCH_URL = urljoin(BASE_URL, "/index.php?search=")
    
    def __init__(self):
        """初始化下载工具"""
        self.session = requests.Session()
        self.headers = {
            "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36",
            "Accept": "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8",
            "Accept-Language": "en-US,en;q=0.5"
        }
        self.session.headers.update(self.headers)
    
    def search_subtitles(self, title: str, language: str = "English") -> List[Dict]:
        """搜索字幕
        
        Args:
            title: 视频标题
            language: 字幕语言，默认为英语
            
        Returns:
            List[Dict]: 字幕信息列表，每个字典包含字幕详细信息
        """
        try:
            search_url = f"{self.SEARCH_URL}{title}"
            response = self.session.get(search_url)
            response.raise_for_status()
            
            soup = BeautifulSoup(response.text, "html.parser")
            subtitle_items = soup.find_all("tr")
            
            results = []
            for item in subtitle_items:
                # 跳过表头
                if item.find("th"):
                    continue
                    
                # 检查标题是否包含搜索关键词
                item_title = item.text.strip().lower()
                if title.lower() not in item_title:
                    continue
                    
                link = item.find("a")
                if not link:
                    continue
                    
                # 提取字幕信息
                title = link.text.strip()
                download_url = urljoin(self.BASE_URL, link["href"])
                downloads = item.find_all("td")[2].text.strip().split()[0]
                languages = item.find_all("td")[3].text.strip().split()[0]
                is_chinese = "Chinese" in title or "中文" in title or "zh" in title.lower()
                
                subtitle_info = {
                    "title": title,
                    "download_url": download_url,
                    "downloads": int(downloads),
                    "languages": int(languages),
                    "is_chinese": is_chinese
                }
                results.append(subtitle_info)
            
            # 按中文优先和下载量排序
            results.sort(key=lambda x: (-x["is_chinese"], -x["downloads"]))
            return results
            
        except Exception as e:
            print(f"搜索字幕时出错: {str(e)}")
            return []
    
    def download_subtitle(self, download_url: str, save_path: str) -> bool:
        """下载字幕文件
        
        Args:
            download_url: 字幕下载链接
            save_path: 保存路径
            
        Returns:
            bool: 下载是否成功
        """
        try:
            # 确保保存目录存在
            os.makedirs(os.path.dirname(save_path), exist_ok=True)
            
            # 获取下载页面
            response = self.session.get(download_url)
            response.raise_for_status()
            
            # 解析页面内容
            soup = BeautifulSoup(response.text, "html.parser")
            
            # 查找简体中文下载链接
            # 查找所有sub-single元素
            sub_singles = soup.find_all("div", class_="sub-single")
            cn_sub = None
            # 遍历查找中文字幕
            for sub in sub_singles:
                spans = sub.find_all("span")
                if len(spans) > 1 and "Chinese (Simplified)" in spans[1].text.strip():
                    cn_sub = sub
                    break
            if cn_sub:
                download_link = cn_sub.find("a", id="download_zh-CN")
                if download_link:
                    direct_download_link = download_link["href"]
                else:
                    # 如果中文字幕不可用，尝试其他语言（韩语、日语、英语）
                    for lang in ["Korean", "Japanese", "English"]:
                        other_sub = soup.find("div", class_="sub-single", string=lambda text: text and lang in text)
                        if other_sub:
                            download_link = other_sub.find("a", class_="green-link")
                            if download_link:
                                direct_download_link = download_link["href"]
                                break
            else:
                raise Exception("未找到可用的字幕")
                
            direct_download_link = urljoin(self.BASE_URL, direct_download_link)
            
            # 打印下载地址
            print(f"正在从以下地址下载字幕: {direct_download_link}")
            
            # 下载字幕文件
            subtitle_response = self.session.get(direct_download_link)
            subtitle_response.raise_for_status()
            
            # 检查字幕文件大小
            content_length = len(subtitle_response.content)
            if content_length > 80 * 1024:  # 80KB
                print(f"字幕文件过大（{content_length/1024:.1f}KB），可能是垃圾字幕，跳过下载")
                return False
            
            # 保存文件
            with open(save_path, "wb") as f:
                f.write(subtitle_response.content)
            
            print(f"字幕已下载到: {save_path}")
            return True
            
        except Exception as e:
            print(f"下载字幕时出错: {str(e)}")
            return False
    
    def search_and_download(self, title: str, save_dir: str, language: str = "English") -> bool:
        """搜索并下载字幕
        
        Args:
            title: 视频标题
            save_dir: 保存目录
            language: 字幕语言，默认为英语
            
        Returns:
            bool: 是否成功下载至少一个字幕
        """
        # 搜索字幕
        subtitles = self.search_subtitles(title, language)
        if not subtitles:
            print(f"未找到符合条件的字幕: {title} ({language})")
            return False
        
        # 确保保存目录存在
        os.makedirs(save_dir, exist_ok=True)
        
        # 下载找到的第一个字幕
        subtitle = subtitles[0]
        # 获取媒体文件名（不包含扩展名）
        media_files = [f for f in os.listdir(save_dir) if f.lower().endswith(('.mp4', '.mkv', '.avi', '.wmv', '.mov', '.flv'))]
        if media_files:
            base_name = os.path.splitext(media_files[0])[0]
        else:
            base_name = title
            
        # 根据字幕语言设置文件名
        if language.lower() == "chinese":
            file_name = f"{base_name}.Chi.srt"
        else:
            file_name = f"{base_name}.{language}.srt"
            
        file_name = re.sub(r'[<>:"/\\|?*]', '_', file_name)
        save_path = os.path.join(save_dir, file_name)
        
        return self.download_subtitle(subtitle["download_url"], save_path)


    def batch_download_missing_subtitles(self, root_dir: str) -> None:
        """遍历目录下载缺失的字幕
        
        Args:
            root_dir: 要遍历的根目录
        """
        media_extensions = ('.mp4', '.mkv', '.avi', '.wmv', '.mov', '.flv')
        subtitle_extensions = ('.srt', '.ass')
        
        for dirpath, dirnames, filenames in os.walk(root_dir):
            # 检查当前目录是否有媒体文件
            has_media = any(f.lower().endswith(media_extensions) for f in filenames)
            if not has_media:
                continue
                
            # 检查是否已有字幕文件
            has_subtitle = any(f.lower().endswith(subtitle_extensions) for f in filenames)
            if has_subtitle:
                continue
                
            # 查找nfo文件并提取番号
            nfo_files = [f for f in filenames if f.lower().endswith('.nfo')]
            if not nfo_files:
                continue
                
            try:
                nfo_path = os.path.join(dirpath, nfo_files[0])
                with open(nfo_path, 'r', encoding='utf-8') as f:
                    content = f.read()
                    # 查找originaltitle标签
                    match = re.search(r'<originaltitle>(.*?)</originaltitle>', content)
                    if not match:
                        continue
                        
                    # 获取番号（第一个空格前的内容）
                    code = match.group(1).split()[0]
                    print(f"正在处理目录: {dirpath}")
                    print(f"找到番号: {code}")
                    
                    # 下载字幕
                    self.search_and_download(code, dirpath, "Chinese")
                    
            except Exception as e:
                print(f"处理目录 {dirpath} 时出错: {str(e)}")
                continue

# 使用示例
def example_usage():
    # 初始化下载工具
    downloader = SubtitleCat()
    
    # 搜索并下载字幕
    title = "CWPBD-120"
    save_dir = "/subtitles"
    language = "Chinese"
    
    success = downloader.search_and_download(title, save_dir, language)
    if success:
        print("字幕下载完成！")
    else:
        print("字幕下载失败！")


if __name__ == "__main__":
    # 运行示例
    downloader = SubtitleCat()
    # 指定要遍历的根目录
    root_dir = r"\\192.168.1.199\Porn\Uncensored"
    downloader.batch_download_missing_subtitles(root_dir)