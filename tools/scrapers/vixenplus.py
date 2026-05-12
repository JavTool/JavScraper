#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
VixenPlus 自动刮削器 - 使用 Selenium 模拟真实用户操作
用户手动登录后自动刮削影片数据

使用方法：
1. 安装依赖：pip install selenium beautifulsoup4 lxml requests
2. 下载 ChromeDriver 并放到 PATH 中
3. 运行：python vixenplus.py <media_dir>

工作流程：
1. 遍历指定目录下的所有 NFO 文件
2. 筛选以 Blacked、BlackedRaw、Tushy、TushyRaw、Vixen 开头的目录
3. 解析 NFO 中的 originaltitle，提取标题进行搜索
4. 使用 Selenium 打开浏览器，等待用户手动登录
5. 自动搜索并爬取视频详情（必须同时匹配演员名和完整标题）
6. 更新 NFO 文件（保留原有标题），下载封面图和剧照

示例：
python vixenplus.py "\\192.168.1.199\Jav\Western"
"""

import os
import re
import sys
import json
import time
import shutil
import xml.etree.ElementTree as ET
from datetime import datetime
from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
from selenium.webdriver.chrome.options import Options
from selenium.common.exceptions import TimeoutException, NoSuchElementException
from bs4 import BeautifulSoup
import argparse
import requests


# ---------------------------------------------------------------------------
# 日志工具
# ---------------------------------------------------------------------------

class TeeStream:
    """将输出同时写入控制台和日志文件"""
    def __init__(self, stream, log_file):
        self.stream = stream
        self.log_file = log_file

    def write(self, message):
        self.stream.write(message)
        try:
            self.log_file.write(message)
        except Exception:
            pass

    def flush(self):
        self.stream.flush()
        try:
            self.log_file.flush()
        except Exception:
            pass


def setup_console_logging():
    """设置控制台日志同时写入文件"""
    log_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'vixensplus.log')
    log_file = open(log_path, 'a', encoding='utf-8', errors='replace')
    sys.stdout = TeeStream(sys.stdout, log_file)
    sys.stderr = TeeStream(sys.stderr, log_file)
    print(f"日志记录到: {log_path}")
    return log_file


# ---------------------------------------------------------------------------
# 主刮削器类
# ---------------------------------------------------------------------------

class VixenPlusScraper:
    """VixenPlus 自动刮削器 - 基于 Selenium 浏览器自动化"""

    def __init__(self, media_dir, tag_convert_file="tag_convert.json"):
        self.media_dir = media_dir
        self.tag_convert_file = os.path.join(os.path.dirname(os.path.abspath(__file__)), tag_convert_file)
        self.driver = None
        self.wait = None
        self.tag_mapping = self.load_tag_convert()

    # ── 浏览器控制 ────────────────────────────────────────────────────────────

    def setup_driver(self):
        """设置 Selenium WebDriver"""
        options = Options()
        options.add_argument("--disable-blink-features=AutomationControlled")
        options.add_experimental_option("excludeSwitches", ["enable-automation"])
        options.add_experimental_option('useAutomationExtension', False)
        self.driver = webdriver.Chrome(options=options)
        self.driver.execute_script(
            "Object.defineProperty(navigator, 'webdriver', {get: () => undefined})"
        )
        self.wait = WebDriverWait(self.driver, 10)

    def login(self):
        """自动登录 VixenPlus：从环境变量 VIXENPLUS_USER / VIXENPLUS_PASS 读取凭据，
        未配置则回退到手动登录"""
        username = os.environ.get("VIXENPLUS_USER", "")
        password = os.environ.get("VIXENPLUS_PASS", "")

        if not username or not password:
            print("[WARN] 未配置环境变量 VIXENPLUS_USER / VIXENPLUS_PASS，回退手动登录")
            print("正在打开浏览器，请手动登录 VixenPlus 会员网站...")
            self.driver.get("https://members.vixenplus.com")
            input("登录完成后按 Enter 键继续...")
            return

        print(f"[INFO] 使用环境变量凭据自动登录: {username}")
        self.driver.get("https://members.vixenplus.com")
        time.sleep(2)

        # 查找并填写邮箱/用户名
        try:
            email_input = self.wait.until(EC.presence_of_element_located(
                (By.CSS_SELECTOR, 'input[type="email"], input[name="email"], input[placeholder*="mail"]')
            ))
            email_input.clear()
            email_input.send_keys(username)
            print("[LOGIN] 已填入用户名")
        except TimeoutException:
            print("[WARN] 未找到用户名输入框，回退手动登录")
            input("请在浏览器中手动登录，完成后按 Enter 键继续...")
            return

        # 查找并填写密码
        try:
            pwd_input = self.driver.find_element(
                By.CSS_SELECTOR, 'input[type="password"], input[name="password"]'
            )
            pwd_input.clear()
            pwd_input.send_keys(password)
            print("[LOGIN] 已填入密码")
        except NoSuchElementException:
            print("[WARN] 未找到密码输入框，回退手动登录")
            input("请在浏览器中手动登录，完成后按 Enter 键继续...")
            return

        # 点击登录按钮
        try:
            login_btn = self.driver.find_element(
                By.CSS_SELECTOR, 'button[type="submit"], button[data-test-component="LoginButton"]'
            )
            login_btn.click()
            print("[LOGIN] 已点击登录按钮")
        except NoSuchElementException:
            from selenium.webdriver.common.keys import Keys
            pwd_input.send_keys(Keys.RETURN)
            print("[LOGIN] 已通过回车提交")

        # 等待登录完成
        time.sleep(3)
        print("[LOGIN] 自动登录完成")

    # ── 标签替换 ──────────────────────────────────────────────────────────────

    def load_tag_convert(self) -> dict:
        """加载 tag_convert.json 映射表，key 统一为小写方便查找"""
        if not os.path.exists(self.tag_convert_file):
            print(f"[WARN] 标签映射文件不存在: {self.tag_convert_file}")
            return {}
        with open(self.tag_convert_file, 'r', encoding='utf-8') as f:
            raw = json.load(f)
        # key 统一小写，便于大小写不敏感匹配
        return {k.strip().lower(): v.strip() for k, v in raw.items()}

    def apply_tag_convert(self, tags: list) -> list:
        """根据 tag_convert.json 替换标签：映射到的保留，未映射的删除，去重"""
        if not self.tag_mapping:
            print("[WARN] 标签映射表为空，跳过替换")
            return []
        result = []
        seen = set()
        removed = []
        for tag in tags:
            key = tag.strip().lower()
            if key in self.tag_mapping:
                converted = self.tag_mapping[key]
                if converted not in seen:
                    seen.add(converted)
                    result.append(converted)
            else:
                removed.append(tag)
        if removed:
            print(f"[TAG] 已删除未映射标签 ({len(removed)} 个): {removed}")
        if result:
            print(f"[TAG] 替换后标签 ({len(result)} 个): {result}")
        return result

    # ── NFO 解析 ──────────────────────────────────────────────────────────────

    def parse_nfo(self, nfo_path):
        """解析 NFO 文件，提取 originaltitle、title、studio 和演员"""
        tree = ET.parse(nfo_path)
        root = tree.getroot()

        def _text(elem):
            return elem.text.strip() if elem is not None and elem.text else None

        originaltitle = _text(root.find('originaltitle'))
        title = _text(root.find('title'))
        studio = _text(root.find('studio'))

        actors = []
        for actor_elem in root.findall('actor'):
            name_elem = actor_elem.find('name')
            if name_elem is not None and name_elem.text:
                actors.append(name_elem.text.strip())

        return originaltitle, title, studio, actors

    def extract_title_from_originaltitle(self, originaltitle):
        """从 originaltitle 中提取标题部分
        格式: Blacked.21.05.29 Vacay Part 2 → Vacay Part 2
        """
        parts = originaltitle.split(' ', 1)
        return parts[1] if len(parts) == 2 else originaltitle

    # ── 搜索与匹配 ────────────────────────────────────────────────────────────

    @staticmethod
    def normalize_text(text):
        """规范化文本：转小写，去除标点符号，合并空白"""
        if not text:
            return ""
        result = text.lower()
        result = re.sub(r'[^0-9a-z\u4e00-\u9fff]+', ' ', result)
        return re.sub(r'\s+', ' ', result).strip()

    def titles_exact_match(self, query_title, candidate_title):
        """标题完整匹配（去除标点符号后精确比较）"""
        q = self.normalize_text(query_title)
        c = self.normalize_text(candidate_title)
        return bool(q and c and q == c)

    def actor_in_candidates(self, query_actors, candidate_actors):
        """检查搜索演员是否在候选演员列表中（至少一个匹配）"""
        if not query_actors or not candidate_actors:
            return False
        candidate_text = self.normalize_text(' '.join(candidate_actors))
        for actor in query_actors:
            actor_text = self.normalize_text(actor)
            if actor_text and actor_text in candidate_text:
                return True
        return False

    def search_video(self, title, actor_names=None):
        """在网站上搜索视频（必须同时匹配演员名和完整标题）"""
        actor_names = actor_names or []
        query = f"{actor_names[0]} {title}" if actor_names else title
        search_url = f"https://members.vixenplus.com/search?q={query.replace(' ', '%20')}"
        print(f"搜索 URL: {search_url}")
        self.driver.get(search_url)
        time.sleep(0.5)

        try:
            self.wait.until(EC.presence_of_element_located(
                (By.CSS_SELECTOR, '[data-test-component="VideoThumbnail"]')
            ))
        except TimeoutException:
            print("搜索结果加载超时")
            return None

        results = self.driver.find_elements(
            By.CSS_SELECTOR, '[data-test-component="VideoThumbnail"]'
        )
        if not results:
            print("未找到搜索结果")
            return None

        matched_urls = []

        for result in results:
            try:
                title_link = result.find_element(
                    By.CSS_SELECTOR, '[data-test-component="VideoThumbnailTitleLink"]'
                )
            except NoSuchElementException:
                continue
            candidate_title = title_link.text.strip()
            video_url = (
                title_link.get_attribute('href')
                or result.find_element(By.CSS_SELECTOR, 'a').get_attribute('href')
            )

            candidate_actors = []
            try:
                model_container = result.find_element(
                    By.CSS_SELECTOR, '[data-test-component="Models"]'
                )
                for actor_link in model_container.find_elements(By.TAG_NAME, 'a'):
                    actor_text = actor_link.text.strip()
                    if actor_text:
                        candidate_actors.append(actor_text)
            except NoSuchElementException:
                pass

            # 必须同时满足：标题完整匹配 + 演员名匹配
            title_ok = self.titles_exact_match(title, candidate_title)
            actor_ok = self.actor_in_candidates(actor_names, candidate_actors)

            if title_ok and actor_ok:
                print(f"  [MATCH] 标题+演员均匹配: {candidate_title} | "
                      f"演员: {', '.join(candidate_actors)}")
                matched_urls.append(video_url)
            elif title_ok:
                print(f"  [TITLE_ONLY] 标题匹配但演员不匹配: {candidate_title} | "
                      f"演员: {', '.join(candidate_actors)}")
            elif actor_ok:
                print(f"  [ACTOR_ONLY] 演员匹配但标题不匹配: {candidate_title}")

        if matched_urls:
            print(f"找到 {len(matched_urls)} 个完全匹配，使用第一个: {matched_urls[0]}")
            return matched_urls[0]

        print("未找到同时匹配标题和演员的视频")
        return None

    # ── 页面刮削 ──────────────────────────────────────────────────────────────

    def scrape_video_page(self, url):
        """爬取视频页面，提取数据（不提取 title，title 保留原 NFO 值）"""
        print(f"访问视频页面: {url}")
        self.driver.get(url)
        time.sleep(0.5)

        self.wait.until(EC.presence_of_element_located((By.TAG_NAME, 'body')))

        html = self.driver.page_source
        soup = BeautifulSoup(html, 'html.parser')

        data = {
            "plot": "",
            "date": "",
            "actors": [],
            "genres": [],
            "cover": "",
            "url": url,
            "studio": "",       # 从页面 Brand 字段刮削
            "runtime": "",
            "director": "",
            "gallery_urls": [],
        }

        # 1. 优先从 __NEXT_DATA__ 解析
        next_data = self._extract_next_data(html)
        if next_data:
            self._parse_from_next_data(next_data, data)

        # 2. 补充从 JSON-LD 解析
        for jld in self._extract_json_ld(html):
            self._parse_from_json_ld(jld, data)

        # 3. HTML 解析（包含 Brand 提取）
        self._parse_from_html(soup, data)

        # 4. 提取标签并用 tag_convert.json 替换（未映射的删除）
        tags = self.extract_tags(soup)
        if tags:
            print(f"[TAG] 原始标签 ({len(tags)} 个): {tags}")
            data['genres'] = self.apply_tag_convert(tags)
        else:
            data['genres'] = []

        return data

    def extract_tags(self, soup):
        """从页面提取标签"""
        tags = []
        try:
            tag_div = soup.find('div', {'data-test-component': 'VideoCategories'})
            if tag_div:
                for link in tag_div.find_all('a'):
                    tag_text = link.get_text(strip=True)
                    if tag_text:
                        tags.append(tag_text)
        except Exception as e:
            print(f"提取标签时出错: {e}")
        return tags

    # ── NFO 更新（保留原有标题） ──────────────────────────────────────────────

    def update_nfo(self, data, nfo_path):
        """更新 NFO 文件（保留原有 title，仅更新其他字段）"""
        tree = ET.parse(nfo_path)
        root = tree.getroot()

        def _set(tag, text):
            """设置子元素文本，不存在则创建"""
            elem = root.find(tag)
            if elem is not None:
                elem.text = str(text) if text is not None else ""
            else:
                elem = ET.SubElement(root, tag)
                elem.text = str(text) if text is not None else ""

        # plot
        if data.get("plot"):
            _set("plot", data["plot"])

        # date / premiered / releasedate / year
        if data.get("date"):
            _set("premiered", data["date"])
            _set("releasedate", data["date"])
            _set("dateadded", data["date"])
            _set("year", data["date"][:4] if len(data["date"]) >= 4 else "")

        # runtime
        if data.get("runtime"):
            _set("runtime", data["runtime"])

        # studio（从页面 Brand 字段刮削）
        if data.get("studio"):
            _set("studio", data["studio"])

        # director
        if data.get("director"):
            _set("director", data["director"])

        # actors — 先删后增
        if data.get("actors"):
            for elem in list(root.findall("actor")):
                root.remove(elem)
            for actor_name in data["actors"]:
                actor_el = ET.SubElement(root, "actor")
                ET.SubElement(actor_el, "name").text = actor_name
                ET.SubElement(actor_el, "type").text = "Actor"

        # genres + tags — 同步更新
        if data.get("genres"):
            for elem in list(root.findall("genre")):
                root.remove(elem)
            for elem in list(root.findall("tag")):
                root.remove(elem)
            for genre in data["genres"]:
                ET.SubElement(root, "genre").text = genre
                ET.SubElement(root, "tag").text = genre

        # uniqueid URL
        url_uid = root.find(".//uniqueid[@type='VixenScraper-Url']")
        if url_uid is not None:
            url_uid.text = data.get("url", "")
        else:
            uid = ET.SubElement(root, "uniqueid")
            uid.set("type", "VixenScraper-Url")
            uid.text = data.get("url", "")

        # 备份原文件（仅首次）
        bak_path = nfo_path + ".bak"
        if not os.path.exists(bak_path):
            shutil.copy2(nfo_path, bak_path)

        # 写入
        ET.indent(root, space="  ")
        tree.write(nfo_path, encoding="utf-8", xml_declaration=True)
        print(f"[OK] NFO 已更新: {nfo_path}")

    # ── 图片下载（使用浏览器 cookies） ────────────────────────────────────────

    def _make_request_session(self):
        """从 Selenium 浏览器中提取 cookies 创建 requests 会话"""
        session = requests.Session()
        session.headers.update({
            "User-Agent": self.driver.execute_script("return navigator.userAgent"),
            "Referer": self.driver.current_url,
        })
        for cookie in self.driver.get_cookies():
            session.cookies.set(cookie['name'], cookie['value'],
                                domain=cookie.get('domain', ''))
        return session

    def download_cover(self, cover_url, media_dir):
        """下载封面图并保存为 jacket.jpg / folder.jpg / poster.jpg"""
        if not cover_url:
            print("[WARN] 未获取到封面图 URL，跳过下载")
            return False
        try:
            session = self._make_request_session()
            resp = session.get(cover_url, timeout=30)
            resp.raise_for_status()
            if len(resp.content) < 1000:
                print(f"[WARN] 封面图过小 ({len(resp.content)} bytes)，可能无效")
                return False

            dir_name = os.path.basename(media_dir.rstrip(os.sep))
            jacket_path = os.path.join(media_dir, "jacket.jpg")
            with open(jacket_path, "wb") as f:
                f.write(resp.content)
            print(f"[OK] 封面图已保存: {jacket_path}")

            for fname in ("folder.jpg", "poster.jpg", f"{dir_name}-poster.jpg"):
                shutil.copy2(jacket_path, os.path.join(media_dir, fname))
                print(f"[OK] 已复制封面: {fname}")
            return True
        except Exception as e:
            print(f"[ERROR] 下载封面图失败: {e}")
            return False

    def download_gallery(self, gallery_urls, media_dir):
        """下载剧照到 media_dir 目录"""
        if not gallery_urls:
            print("[WARN] 没有可用的剧照 URL，跳过")
            return
        session = self._make_request_session()
        ok_count = 0
        for idx, img_url in enumerate(gallery_urls, start=1):
            save_path = os.path.join(media_dir, f"backdrop{idx}.jpg")
            try:
                resp = session.get(img_url, timeout=30)
                resp.raise_for_status()
                if len(resp.content) < 1000:
                    print(f"[WARN] 剧照 {idx} 过小，跳过")
                    continue
                with open(save_path, "wb") as f:
                    f.write(resp.content)
                print(f"[OK] 剧照 {idx}/{len(gallery_urls)}: {save_path}")
                ok_count += 1
            except Exception as e:
                print(f"[ERROR] 下载剧照 {idx} 失败: {e}")
        print(f"[INFO] 剧照下载完成: {ok_count}/{len(gallery_urls)} 张")

    # ── 主流程 ────────────────────────────────────────────────────────────────

    def run(self, media_dir=None):
        """主运行方法"""
        target_dir = media_dir or self.media_dir
        self.setup_driver()
        try:
            self.login()
            self.scrape_from_directory(target_dir)
        finally:
            if self.driver:
                self.driver.quit()

    def scrape_from_directory(self, directory):
        """遍历目录，处理 NFO 文件"""
        nfo_files = []
        for root, dirs, files in os.walk(directory):
            for file in files:
                if file.lower().endswith('.nfo'):
                    nfo_path = os.path.join(root, file)
                    dir_name = os.path.basename(root)
                    if re.match(r'^(Blacked|BlackedRaw|Tushy|TushyRaw|Vixen)',
                                dir_name, re.IGNORECASE):
                        nfo_files.append(nfo_path)

        print(f"找到 {len(nfo_files)} 个符合条件的 NFO 文件")

        for nfo_path in nfo_files:
            print(f"\n{'='*60}")
            print(f"处理: {nfo_path}")
            originaltitle, title_from_nfo, original_studio, actors = self.parse_nfo(nfo_path)
            if not originaltitle and not title_from_nfo:
                print("跳过: 无法解析 originaltitle 或 title")
                continue

            # 提取用于搜索的标题
            if 'hotel vixen 2' in nfo_path.lower() and title_from_nfo:
                search_title = title_from_nfo
            elif originaltitle:
                search_title = self.extract_title_from_originaltitle(originaltitle)
            else:
                search_title = title_from_nfo

            if not search_title:
                print("跳过: 无法提取标题")
                continue

            video_url = self.search_video(search_title, actor_names=actors)
            if not video_url:
                print("跳过: 未找到视频")
                continue

            data = self.scrape_video_page(video_url)

            # 更新 NFO（保留原有标题）
            self.update_nfo(data, nfo_path)

            # 下载封面和剧照到 NFO 所在目录
            nfo_dir = os.path.dirname(nfo_path)
            # self.download_cover(data.get("cover", ""), nfo_dir)
            # self.download_gallery(data.get("gallery_urls", []), nfo_dir)

            time.sleep(0.5)

    # ── 解析辅助方法 ──────────────────────────────────────────────────────────

    def _extract_next_data(self, html: str) -> dict:
        match = re.search(
            r'<script[^>]+id=["\']__NEXT_DATA__["\'][^>]*>(.*?)</script>', html, re.S
        )
        if match:
            try:
                return json.loads(match.group(1))
            except json.JSONDecodeError:
                pass
        return {}

    def _extract_json_ld(self, html: str) -> list:
        results = []
        for m in re.finditer(
            r'<script[^>]+type=["\']application/ld\+json["\'][^>]*>(.*?)</script>',
            html, re.S
        ):
            try:
                results.append(json.loads(m.group(1)))
            except json.JSONDecodeError:
                pass
        return results

    @staticmethod
    def _parse_date(raw: str) -> str:
        if not raw:
            return ""
        m = re.match(r'(\d{4}-\d{2}-\d{2})', raw)
        if m:
            return m.group(1)
        try:
            dt = datetime.strptime(raw.strip(), "%B %d, %Y")
            return dt.strftime("%Y-%m-%d")
        except ValueError:
            pass
        return raw.strip()

    def _deep_find(self, obj, key: str, depth: int = 4):
        if depth <= 0:
            return None
        if isinstance(obj, dict):
            if key in obj:
                return obj[key]
            for v in obj.values():
                result = self._deep_find(v, key, depth - 1)
                if result is not None:
                    return result
        elif isinstance(obj, list):
            for item in obj:
                result = self._deep_find(item, key, depth - 1)
                if result is not None:
                    return result
        return None

    def _parse_from_next_data(self, next_data: dict, data: dict):
        """从 Next.js __NEXT_DATA__ JSON 中提取字段（不提取 title）"""
        try:
            page_props = next_data.get("props", {}).get("pageProps", {})
            video_obj = (
                page_props.get("video")
                or page_props.get("videoData")
                or page_props.get("data", {}).get("video")
                or self._deep_find(page_props, "video")
            )
            if not video_obj or not isinstance(video_obj, dict):
                return

            # 不提取 title — 保留原有 NFO 中的标题

            if not data["plot"]:
                data["plot"] = (
                    video_obj.get("description")
                    or video_obj.get("plot")
                    or video_obj.get("synopsis")
                    or ""
                )

            if not data["date"]:
                raw_date = (
                    video_obj.get("releaseDate")
                    or video_obj.get("publishDate")
                    or video_obj.get("date")
                    or video_obj.get("datePublished")
                    or ""
                )
                data["date"] = self._parse_date(raw_date)

            if not data["runtime"]:
                rt = (video_obj.get("runLength")
                      or video_obj.get("runLengthFormatted")
                      or video_obj.get("duration") or "")
                if rt:
                    hms = re.match(r'(\d+):(\d+):(\d+)', str(rt))
                    if hms:
                        h, m, s = int(hms.group(1)), int(hms.group(2)), int(hms.group(3))
                        data["runtime"] = str(h * 60 + m + (1 if s >= 30 else 0))
                    else:
                        m2 = re.match(r'PT(?:(\d+)H)?(?:(\d+)M)?', str(rt))
                        if m2:
                            hours = int(m2.group(1) or 0)
                            minutes = int(m2.group(2) or 0)
                            data["runtime"] = str(hours * 60 + minutes)
                        else:
                            data["runtime"] = str(rt)

            if not data["actors"]:
                performers = (
                    video_obj.get("modelsSlugged")
                    or video_obj.get("performers")
                    or video_obj.get("actors")
                    or video_obj.get("models")
                    or []
                )
                if isinstance(performers, list):
                    for p in performers:
                        if isinstance(p, dict):
                            name = p.get("name") or p.get("slug") or p.get("slugged") or ""
                            if name:
                                data["actors"].append(name)
                        elif isinstance(p, str):
                            data["actors"].append(p)

            if not data.get("director"):
                directors = video_obj.get("directors") or []
                if isinstance(directors, list) and directors:
                    names = [d.get("name", "") for d in directors
                             if isinstance(d, dict) and d.get("name")]
                    if names:
                        data["director"] = ", ".join(names)

            if not data["genres"]:
                tags = (
                    video_obj.get("tags")
                    or video_obj.get("categories")
                    or video_obj.get("genres")
                    or []
                )
                if isinstance(tags, list):
                    for t in tags:
                        if isinstance(t, dict):
                            name = t.get("name") or t.get("slug") or ""
                            if name:
                                data["genres"].append(name)
                        elif isinstance(t, str):
                            data["genres"].append(t)

            if not data["cover"]:
                images = video_obj.get("images", {})
                if isinstance(images, dict):
                    img_list = images.get("listing") or images.get("main") or []
                    best_url, best_w = "", 0
                    for img in img_list:
                        if isinstance(img, dict):
                            w = img.get("width", 0) or 0
                            src = img.get("src") or ""
                            if w > best_w and src:
                                best_w, best_url = w, src
                    if best_url:
                        data["cover"] = best_url

                if not data["cover"]:
                    vi = video_obj.get("videoImage") or {}
                    if isinstance(vi, dict):
                        data["cover"] = vi.get("src") or ""

                if not data["cover"]:
                    sd = (next_data.get("props", {}).get("pageProps", {})
                          .get("structuredData") or {})
                    data["cover"] = sd.get("thumbnailUrl") or ""

            if not data["gallery_urls"]:
                gallery_images = page_props.get("galleryImages", [])
                if isinstance(gallery_images, list) and gallery_images:
                    for gi in gallery_images:
                        if isinstance(gi, dict):
                            src = gi.get("src") or ""
                            if src:
                                data["gallery_urls"].append(src)

            if not data["gallery_urls"]:
                carousel = video_obj.get("carousel", [])
                if isinstance(carousel, list):
                    for item in carousel:
                        if isinstance(item, dict):
                            main_list = item.get("main") or []
                            if main_list and isinstance(main_list, list):
                                src = (main_list[0].get("src") or ""
                                       if isinstance(main_list[0], dict) else "")
                                if src:
                                    data["gallery_urls"].append(src)

        except Exception as e:
            print(f"[WARN] 解析 __NEXT_DATA__ 时出错: {e}")

    def _parse_from_json_ld(self, jld: dict, data: dict):
        """从 JSON-LD 数据中补充字段（不提取 title）"""
        schema_type = jld.get("@type", "")
        if schema_type not in ("Movie", "VideoObject", "TVEpisode", "CreativeWork"):
            return

        # 不提取 title — 保留原有 NFO 中的标题

        if not data["plot"]:
            data["plot"] = jld.get("description") or ""

        if not data["date"]:
            raw = (jld.get("datePublished")
                   or jld.get("uploadDate")
                   or jld.get("dateCreated") or "")
            data["date"] = self._parse_date(raw)

        if not data["actors"]:
            for role_key in ("actor", "actors", "performer", "performers"):
                persons = jld.get(role_key, [])
                if isinstance(persons, dict):
                    persons = [persons]
                if isinstance(persons, list):
                    for p in persons:
                        if isinstance(p, dict):
                            n = p.get("name") or ""
                            if n:
                                data["actors"].append(n)
                        elif isinstance(p, str):
                            data["actors"].append(p)
                if data["actors"]:
                    break

        if not data["genres"]:
            for key in ("genre", "keywords"):
                val = jld.get(key, [])
                if isinstance(val, str):
                    val = [v.strip() for v in val.split(",") if v.strip()]
                if isinstance(val, list):
                    for g in val:
                        if isinstance(g, str) and g:
                            data["genres"].append(g)
                if data["genres"]:
                    break

        if not data["cover"]:
            img = jld.get("image") or jld.get("thumbnailUrl") or ""
            if isinstance(img, dict):
                img = img.get("url") or img.get("contentUrl") or ""
            if isinstance(img, list) and img:
                i0 = img[0]
                img = ((i0.get("url") or i0.get("contentUrl") or "")
                       if isinstance(i0, dict) else str(i0))
            data["cover"] = str(img)

        if not data["runtime"]:
            rt = jld.get("duration") or ""
            if rt:
                m = re.match(r'PT(?:(\d+)H)?(?:(\d+)M)?', str(rt))
                if m:
                    hours = int(m.group(1) or 0)
                    minutes = int(m.group(2) or 0)
                    data["runtime"] = str(hours * 60 + minutes)

    def _parse_from_html(self, soup, data: dict):
        """从 HTML 标签中提取元数据（不提取 title，始终提取 Brand 作为 studio）"""
        # 不提取 title — 保留原有 NFO 中的标题

        if not data["plot"]:
            og_desc = soup.find("meta", property="og:description")
            if og_desc:
                data["plot"] = og_desc.get("content", "").strip()
            if not data["plot"]:
                meta_desc = soup.find("meta", attrs={"name": "description"})
                if meta_desc:
                    data["plot"] = meta_desc.get("content", "").strip()

        if not data["cover"]:
            og_img = soup.find("meta", property="og:image")
            if og_img:
                data["cover"] = og_img.get("content", "").strip()

        if not data["date"]:
            time_tag = soup.find("time")
            if time_tag:
                raw = time_tag.get("datetime") or time_tag.get_text(strip=True)
                data["date"] = self._parse_date(raw)

        # actors — 从 performer 链接中提取，遇到 "&" 后停止（后面是男演员）
        perf_links = soup.find_all(
            "a", href=re.compile(r'/(pornstars|models|performers?)/')
        )
        if perf_links:
            parent = perf_links[0].parent
            female_actors = []
            for child in parent.children:
                if hasattr(child, 'get_text'):
                    text = child.get_text(strip=True)
                    if text == '&':
                        break
                    if (child.name == 'a'
                            and re.search(r'/(pornstars|models|performers?)/',
                                          child.get('href', ''))):
                        if text and text not in female_actors:
                            female_actors.append(text)
            if female_actors:
                data["actors"] = female_actors
            elif not data["actors"]:
                for a in perf_links:
                    name = a.get_text(strip=True)
                    if name and name not in data["actors"]:
                        data["actors"].append(name)

        # genres
        if not data["genres"]:
            for a in soup.find_all("a", href=re.compile(r'/(categories|tags|genres?)/')):
                name = a.get_text(strip=True)
                if name and name not in data["genres"]:
                    data["genres"].append(name)

        # studio — 始终从页面 Brand 字段刮削（无条件覆盖）
        brand_div = soup.find('div', {'data-test-component': 'BrandSection'})
        if brand_div:
            brand_link = brand_div.find('a', {'data-test-component': 'BrandLink'})
            if brand_link:
                brand_name = brand_link.get_text(strip=True)
                if brand_name:
                    data["studio"] = brand_name
                    print(f"[INFO] 从页面 Brand 字段获取 studio: {brand_name}")


# ---------------------------------------------------------------------------
# CLI 入口
# ---------------------------------------------------------------------------

def main():
    setup_console_logging()

    parser = argparse.ArgumentParser(
        description="VixenPlus 自动刮削器 - 使用 Selenium 模拟真实用户操作",
        epilog="""
            使用示例:
            python vixenplus.py "\\\\192.168.1.199\\Jav\\Western"
            python vixenplus.py /path/to/media --tag-convert tag_convert.json

            工作流程:
            1. 脚本会打开浏览器，请手动登录 VixenPlus 会员网站
            2. 登录完成后按 Enter 键继续
            3. 脚本自动遍历目录，搜索并刮削匹配的视频数据
            4. 更新 NFO 文件（保留原有标题），下载封面和剧照
        """,
        formatter_class=argparse.RawDescriptionHelpFormatter
    )
    parser.add_argument(
        "media_dir",
        help="包含 NFO 文件的媒体目录路径"
    )
    parser.add_argument(
        "--tag-convert",
        default="tag_convert.json",
        help="标签映射文件路径 (默认: tag_convert.json)"
    )

    args = parser.parse_args()

    print("VixenPlus 自动刮削器")
    print("=" * 50)
    print(f"媒体目录: {args.media_dir}")
    print()

    scraper = VixenPlusScraper(media_dir=args.media_dir, tag_convert_file=args.tag_convert)
    scraper.run()


if __name__ == "__main__":
    main()
