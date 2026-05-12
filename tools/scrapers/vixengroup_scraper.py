#!/usr/bin/env python3
# -*- coding: utf-8 -*-
r"""
Vixen Group Scraper
整合 Vixen、Blacked、Tushy、BlackedRaw、TushyRaw 等刮削器，
根据域名判断视频来源并生成相应的 NFO 文件。

工作流程：
1. 打开浏览器，用户手动登录 VixenPlus
2. 刮削视频页面获取基本数据（标题、演员、剧情、封面等）
3. 去 VixenPlus 搜索同一视频，获取 genre/tag
4. 根据 tag_convert.json 转换标签，未映射的删除
5. 生成 NFO 文件，下载封面和剧照（可用 --no-download 关闭）

用法：
    python vixengroup_scraper.py <video_url> <output_dir>
示例：
    python vixengroup_scraper.py "https://members.vixenplus.com/videos/im-not-leaving" "G:\Downloads\Deeper.20.03.14.Rae.Lil.Black.I'm.Not.Leaving.XXX.1080p.HEVC.x265.PRT" --no-download --type=episode
"""

import os
import re
import sys
import json
import time
import shutil
import xml.etree.ElementTree as ET
from datetime import datetime
from urllib.parse import urlparse
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
# 域名 → 工作室名
# ---------------------------------------------------------------------------
DOMAIN_STUDIO = {
    "vixen.com": "Vixen",
    "blacked.com": "Blacked",
    "tushy.com": "Tushy",
    "blackedraw.com": "Blacked Raw",
    "tushyraw.com": "Tushy Raw",
    "deeper.com": "Deeper",
    "slayed.com": "Slayed",
}

# 工作室名 → 品牌站域名（反查表，用于 vixenplus.com URL 跳转到品牌站）
STUDIO_TO_DOMAIN = {v.lower(): k for k, v in DOMAIN_STUDIO.items()}

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))


# ---------------------------------------------------------------------------
# 主刮削器类
# ---------------------------------------------------------------------------

class VixenGroupScraper:
    """Vixen Group 刮削器 — 根据 URL 域名自动识别品牌"""

    def __init__(self, tag_convert_file="tag_convert.json"):
        self.tag_mapping = self._load_tag_convert(tag_convert_file)
        self.driver = None
        self.wait = None

    # ── 标签转换 ──────────────────────────────────────────────────────────────

    @staticmethod
    def _load_tag_convert(filename) -> dict:
        path = os.path.join(SCRIPT_DIR, filename)
        if not os.path.exists(path):
            print(f"[WARN] 标签映射文件不存在: {path}")
            return {}
        with open(path, 'r', encoding='utf-8') as f:
            raw = json.load(f)
        return {k.strip().lower(): v.strip() for k, v in raw.items()}

    def _apply_tag_convert(self, tags: list) -> list:
        """映射到的保留并替换，未映射的删除，去重"""
        if not self.tag_mapping:
            print("[WARN] 标签映射表为空")
            return []
        result, seen, removed = [], set(), []
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

    # ── 浏览器控制 ────────────────────────────────────────────────────────────

    def setup_driver(self):
        options = Options()
        options.add_argument("--disable-blink-features=AutomationControlled")
        options.add_experimental_option("excludeSwitches", ["enable-automation"])
        options.add_experimental_option('useAutomationExtension', False)
        self.driver = webdriver.Chrome(options=options)
        self.driver.execute_script(
            "Object.defineProperty(navigator, 'webdriver', {get: () => undefined})"
        )
        self.wait = WebDriverWait(self.driver, 10)

    def _wait_for_members_page(self, timeout=120):
        """等待浏览器跳转到 VixenPlus 会员首页（URL 包含 members.vixenplus.com 且不含 login/join）"""
        print("[LOGIN] 等待跳转到 members.vixenplus.com ...")
        try:
            WebDriverWait(self.driver, timeout).until(
                lambda d: "members.vixenplus.com" in d.current_url
                and "login" not in d.current_url.lower()
                and "join" not in d.current_url.lower()
                and "challenge" not in d.current_url.lower()
            )
            print(f"[LOGIN] 已跳转到: {self.driver.current_url}")
            return True
        except TimeoutException:
            print(f"[WARN] 等待跳转超时，当前 URL: {self.driver.current_url}")
            input("如果登录未完成，请手动完成后按 Enter 键继续...")
            return False

    def login(self):
        """自动登录 VixenPlus：从环境变量 VIXENPLUS_USER / VIXENPLUS_PASS 读取凭据，
        处理登录后 Cloudflare 挑战页面，未配置则回退到手动登录"""
        username = os.environ.get("VIXENPLUS_USER", "")
        password = os.environ.get("VIXENPLUS_PASS", "")

        # ── 第一步：打开页面 ────────────────────────────────────────────────────
        print("[LOGIN] 正在打开 VixenPlus 会员网站...")
        self.driver.get("https://members.vixenplus.com")
        time.sleep(1)

        current_url = self.driver.current_url

        # 已经在会员首页（已登录状态）
        if "members.vixenplus.com" in current_url and "login" not in current_url.lower():
            print("[LOGIN] 已处于登录状态")
            return

        # ── 第二步：检测登录页面，自动填入凭据 ─────────────────────────────────
        if "login.vixen.com" in current_url or "login" in current_url.lower():
            if not username or not password:
                print("[WARN] 未配置环境变量 VIXENPLUS_USER / VIXENPLUS_PASS，请手动登录")
                input("登录完成后按 Enter 键继续...")
                self._wait_for_members_page()
                return

            print(f"[LOGIN] 检测到登录页面，自动填入凭据: {username}")
            try:
                email_input = self.wait.until(EC.presence_of_element_located(
                    (By.CSS_SELECTOR, 'input[type="email"], input[name="email"], input[placeholder*="mail"]')
                ))
                email_input.clear()
                email_input.send_keys(username)
                print("[LOGIN] 已填入用户名")
            except TimeoutException:
                print("[WARN] 未找到用户名输入框，请在浏览器中手动登录")
                input("登录完成后按 Enter 键继续...")
                self._wait_for_members_page()
                return

            try:
                pwd_input = self.driver.find_element(
                    By.CSS_SELECTOR, 'input[type="password"], input[name="password"]'
                )
                pwd_input.clear()
                pwd_input.send_keys(password)
                print("[LOGIN] 已填入密码")
            except NoSuchElementException:
                print("[WARN] 未找到密码输入框，请在浏览器中手动登录")
                input("登录完成后按 Enter 键继续...")
                self._wait_for_members_page()
                return

            # 等待用户手动点击登录按钮
            print("[LOGIN] 凭据已填入，请在浏览器中点击登录按钮...")
            input("点击登录按钮后按 Enter 键继续...")
        else:
            # 不确定在哪个页面，让用户手动处理
            print(f"[LOGIN] 当前页面: {current_url}")
            input("请在浏览器中完成登录，完成后按 Enter 键继续...")

        # ── 第三步：检测登录后的 Cloudflare 挑战页面 ─────────────────────────────
        time.sleep(2)
        current_url = self.driver.current_url
        page_src = self.driver.page_source.lower()
        is_cloudflare = ("challenge" in current_url.lower()
                         or "cf-challenge" in page_src
                         or "cloudflare" in page_src
                         or "checking your browser" in page_src)
        if is_cloudflare:
            print("[LOGIN] 检测到 Cloudflare 挑战页面，请在浏览器中完成验证...")
            input("完成 Cloudflare 验证后按 Enter 键继续...")
            time.sleep(2)

        # ── 第四步：等待跳转到会员首页 ──────────────────────────────────────────
        self._wait_for_members_page()

    def close(self):
        if self.driver:
            self.driver.quit()

    # ── URL 域名识别 ──────────────────────────────────────────────────────────

    @staticmethod
    def get_studio_from_url(url: str) -> str:
        """根据 URL 域名判断工作室名称"""
        domain = urlparse(url).netloc.lower()
        domain = re.sub(r'^www\.', '', domain)
        for site_domain, studio in DOMAIN_STUDIO.items():
            if site_domain in domain:
                return studio
        return ""

    # ── 页面刮削 ──────────────────────────────────────────────────────────────

    def scrape_video_page(self, url):
        """刮削视频详情页，获取基本数据"""
        print(f"[INFO] 正在刮削: {url}")
        self.driver.get(url)
        time.sleep(0.5)
        self.wait.until(EC.presence_of_element_located((By.TAG_NAME, 'body')))

        html = self.driver.page_source
        soup = BeautifulSoup(html, 'html.parser')

        data = {
            "title": "",
            "plot": "",
            "date": "",
            "actors": [],
            "genres": [],
            "cover": "",
            "url": url,
            "studio": "",
            "runtime": "",
            "director": "",
            "gallery_urls": [],
        }

        next_data = self._extract_next_data(html)
        if next_data:
            self._parse_from_next_data(next_data, data)

        for jld in self._extract_json_ld(html):
            self._parse_from_json_ld(jld, data)

        self._parse_from_html(soup, data)

        # studio: 优先页面 Brand，其次域名识别
        if not data["studio"]:
            data["studio"] = self.get_studio_from_url(url)

        return data

    # ── VixenPlus 搜索匹配（获取 genre/tag） ──────────────────────

    def search_brand_site_for_detail_url(self, brand_domain, title, actor_names=None):
        """在品牌站点搜索页（例如 https://www.tushy.com/search?q=xxx）匹配视频详情页 URL

        DOM 结构（与 VixenPlus 不同）：
          [data-test-component="VideoList"]
            └─ [data-test-component="VideoThumbnailContainer"] × n
                 ├─ [data-test-component="TitleLink"]                 ← 标题 + href
                 └─ [data-test-component="Models"] > a                  ← 演员名（可多个）

        匹配规则（_normalize_text 已自动去除标点）：
          1) 标题精确匹配 + 演员命中
          2) 标题包含匹配 + 演员命中
          3) 演员列表为空时，仅做标题匹配
        """
        actor_names = actor_names or []
        query = f"{actor_names[0]} {title}" if actor_names else title
        search_url = f"https://www.{brand_domain}/search?q={query.replace(' ', '%20')}"
        print(f"[BRAND] 搜索: {search_url}")
        self.driver.get(search_url)
        time.sleep(1)

        try:
            self.wait.until(EC.presence_of_element_located(
                (By.CSS_SELECTOR,
                 '[data-test-component="VideoList"] [data-test-component="TitleLink"]')
            ))
        except TimeoutException:
            print("[BRAND] 搜索结果加载超时")
            return ""

        cards = self.driver.find_elements(
            By.CSS_SELECTOR,
            '[data-test-component="VideoList"] [data-test-component="VideoThumbnailContainer"]'
        )
        if not cards:
            print("[BRAND] 未找到搜索结果")
            return ""

        # 预采集所有候选：(标题, URL, [演员名])
        candidates = []
        for card in cards:
            try:
                title_link = card.find_element(
                    By.CSS_SELECTOR, '[data-test-component="TitleLink"]'
                )
            except NoSuchElementException:
                continue
            cand_title = title_link.text.strip() or title_link.get_attribute('title') or ""
            cand_url = title_link.get_attribute('href') or ""
            actor_els = card.find_elements(
                By.CSS_SELECTOR, '[data-test-component="Models"] a'
            )
            cand_actors = [
                (e.text.strip() or e.get_attribute('title') or "").strip()
                for e in actor_els
            ]
            cand_actors = [a for a in cand_actors if a]
            candidates.append((cand_title, cand_url, cand_actors))

        print(f"[BRAND] 搜到 {len(candidates)} 个候选结果")

        q_title = self._normalize_text(title)
        query_actors_norm = [
            self._normalize_text(a) for a in actor_names if self._normalize_text(a)
        ]

        def actor_hit(cand_actors):
            """任一演员名（去标点后）在候选演员中命中即算命中；无演员时视为命中"""
            if not query_actors_norm:
                return True
            cand_actors_norm = [self._normalize_text(a) for a in cand_actors]
            for qa in query_actors_norm:
                for ca in cand_actors_norm:
                    if qa and ca and (qa == ca or qa in ca or ca in qa):
                        return True
            return False

        # 第一轮：标题精确匹配 + 演员命中
        for cand_title, cand_url, cand_actors in candidates:
            c_title = self._normalize_text(cand_title)
            if q_title and c_title and q_title == c_title and actor_hit(cand_actors):
                print(f"[BRAND] 精确匹配: {cand_title} | 演员: {cand_actors} -> {cand_url}")
                return cand_url

        # 第二轮：标题包含匹配 + 演员命中
        for cand_title, cand_url, cand_actors in candidates:
            c_title = self._normalize_text(cand_title)
            if (q_title and c_title
                    and (q_title in c_title or c_title in q_title)
                    and actor_hit(cand_actors)):
                print(f"[BRAND] 模糊匹配: {cand_title} | 演员: {cand_actors} -> {cand_url}")
                return cand_url

        print("[BRAND] 未找到匹配视频")
        return ""

    def resolve_vixenplus_to_brand_url(self, video_url):
        """如果传入的 video_url 是 vixenplus.com 域名，先在 VixenPlus 刷取获取 studio，
        然后去对应品牌站搜索匹配真正的详情页 URL。匹配失败或非 vixenplus.com
        域名返回原 URL。
        """
        host = urlparse(video_url).netloc.lower()
        host = re.sub(r'^www\.', '', host)
        if "vixenplus.com" not in host:
            return video_url

        print(f"[INFO] 检测到 vixenplus.com URL，准备根据 STUDIO 跳转到品牌站")
        vp_data = self.scrape_video_page(video_url)
        studio_raw = (vp_data.get("studio") or "").strip()
        if not studio_raw:
            print("[WARN] VixenPlus 页面未获取到 studio，使用原 URL 继续")
            return video_url

        brand_domain = STUDIO_TO_DOMAIN.get(studio_raw.lower())
        if not brand_domain:
            print(f"[WARN] 未知 studio: {studio_raw}，未能映射到品牌站域名，使用原 URL 继续")
            return video_url

        print(f"[INFO] STUDIO={studio_raw} -> 品牌站: {brand_domain}")
        detail_url = self.search_brand_site_for_detail_url(
            brand_domain,
            vp_data.get("title", ""),
            vp_data.get("actors", []),
        )
        if not detail_url:
            print("[WARN] 品牌站未找到匹配详情页，使用原 URL 继续")
            return video_url

        print(f"[INFO] 将使用品牌站 URL 继续刷取: {detail_url}")
        return detail_url

    # ── VixenPlus 搜索匹配（获取 genre/tag） ──────────────────────────────────

    def search_vixenplus_for_tags(self, title, actor_names=None):
        """在 VixenPlus 搜索同一视频，获取 genre/tag"""
        actor_names = actor_names or []
        query = f"{actor_names[0]} {title}" if actor_names else title
        search_url = f"https://members.vixenplus.com/search?q={query.replace(' ', '%20')}"
        print(f"[VIXEN+] 搜索: {search_url}")
        self.driver.get(search_url)
        time.sleep(0.5)

        try:
            self.wait.until(EC.presence_of_element_located(
                (By.CSS_SELECTOR, '[data-test-component="VideoThumbnail"]')
            ))
        except TimeoutException:
            print("[VIXEN+] 搜索结果加载超时")
            return []

        results = self.driver.find_elements(
            By.CSS_SELECTOR, '[data-test-component="VideoThumbnail"]'
        )
        if not results:
            print("[VIXEN+] 未找到搜索结果")
            return []

        # 遍历搜索结果，找标题匹配的
        for result in results:
            try:
                title_link = result.find_element(
                    By.CSS_SELECTOR, '[data-test-component="VideoThumbnailTitleLink"]'
                )
            except NoSuchElementException:
                continue
            candidate_title = title_link.text.strip()
            video_url = title_link.get_attribute('href')

            q = self._normalize_text(title)
            c = self._normalize_text(candidate_title)
            if q and c and q == c:
                print(f"[VIXEN+] 标题匹配: {candidate_title}")
                return self._scrape_vixenplus_tags(video_url)

        # 精确匹配失败，尝试包含匹配
        for result in results:
            try:
                title_link = result.find_element(
                    By.CSS_SELECTOR, '[data-test-component="VideoThumbnailTitleLink"]'
                )
            except NoSuchElementException:
                continue
            candidate_title = title_link.text.strip()
            video_url = title_link.get_attribute('href')

            q = self._normalize_text(title)
            c = self._normalize_text(candidate_title)
            if q and c and (q in c or c in q):
                print(f"[VIXEN+] 模糊匹配: {candidate_title}")
                return self._scrape_vixenplus_tags(video_url)

        print("[VIXEN+] 未找到匹配视频")
        return []

    def _scrape_vixenplus_tags(self, url):
        """刮取 VixenPlus 视频页面的 genre/tag"""
        print(f"[VIXEN+] 获取标签: {url}")
        self.driver.get(url)
        time.sleep(0.5)

        html = self.driver.page_source
        soup = BeautifulSoup(html, 'html.parser')
        return self._extract_tags(soup)

    def _extract_tags(self, soup):
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
            print(f"[WARN] 提取标签时出错: {e}")
        return tags

    # ── NFO 生成 ──────────────────────────────────────────────────────────────

    @staticmethod
    def _has_subtitle_file(directory):
        """检查目录中是否存在字幕文件（.srt / .ass / .ssa / .sub / .sup / .vtt）"""
        subtitle_exts = ('.srt', '.ass', '.ssa', '.sub', '.sup', '.vtt')
        if not os.path.isdir(directory):
            return False
        for f in os.listdir(directory):
            if f.lower().endswith(subtitle_exts):
                return True
        return False

    def generate_nfo(self, data, output_dir, media_type="movie"):
        """生成 NFO 文件

        Args:
            media_type: "movie" 或 "episode"，决定 NFO 根节点
                        movie → <movie>，episode → <episodedetails>
        """
        os.makedirs(output_dir, exist_ok=True)
        dir_name = os.path.basename(output_dir.rstrip(os.sep))

        root_tag = "episodedetails" if media_type == "episode" else "movie"
        root = ET.Element(root_tag)

        def sub(tag, text):
            el = ET.SubElement(root, tag)
            el.text = str(text) if text is not None else ""
            return el

        title = data.get("title", "")

        # 从目录名提取 sorttitle（如 Blacked.23.01.21）
        sorttitle_match = re.search(r'([A-Za-z]+\.\d{2}\.\d{2}\.\d{2})', dir_name)
        sorttitle = sorttitle_match.group(1) if sorttitle_match else ""

        # 中文字幕检测：检查目录名 -C 标记 或 目录中是否存在字幕文件
        is_chinese_sub_by_name = bool(re.search(r'-C(?:[^A-Za-z]|$)', dir_name))
        is_chinese_sub_by_file = self._has_subtitle_file(output_dir)
        is_chinese_sub = is_chinese_sub_by_name or is_chinese_sub_by_file
        if is_chinese_sub_by_file and not is_chinese_sub_by_name:
            print(f"[INFO] 检测到字幕文件，标记为中字")
        display_title = f"[中字] {title}" if is_chinese_sub else title
        effective_sorttitle = (
            (f"{sorttitle}-C" if is_chinese_sub else sorttitle)
            if sorttitle else title
        )
        originaltitle = f"{sorttitle} {title}".strip()

        # plot
        plot_el = ET.SubElement(root, "plot")
        plot_el.text = data.get("plot", "")

        sub("outline", "")
        sub("lockdata", "true")
        sub("lockedfields", "Name|OriginalTitle|SortName|Overview|OfficialRating|Cast|Studios")
        sub("dateadded", datetime.now().strftime("%Y-%m-%d %H:%M:%S"))
        sub("title", display_title)
        sub("originaltitle", originaltitle)

        # actors
        for actor_name in data.get("actors", []):
            actor_el = ET.SubElement(root, "actor")
            ET.SubElement(actor_el, "name").text = actor_name
            ET.SubElement(actor_el, "type").text = "Actor"

        date = data.get("date", "")
        year = date[:4] if len(date) >= 4 else ""
        sub("year", year)
        sub("sorttitle", effective_sorttitle)
        sub("mpaa", "XXX")
        sub("premiered", date)
        sub("releasedate", date)
        sub("runtime", data.get("runtime", ""))

        if data.get("director"):
            sub("director", data["director"])

        # genres + tags 同步
        for genre in data.get("genres", []):
            sub("genre", genre)
            sub("tag", genre)

        sub("studio", data.get("studio", ""))

        # uniqueid
        uid_url = ET.SubElement(root, "uniqueid")
        uid_url.set("type", "VixenScraper-Url")
        uid_url.text = data.get("url", "")

        json_str = json.dumps({
            "OriginalTitle": originaltitle,
            "Cover": data.get("cover", ""),
            "Date": date,
        }, ensure_ascii=False)
        uid_json = ET.SubElement(root, "uniqueid")
        uid_json.set("type", "VixenScraper-Json")
        uid_json.text = json_str

        # 写入
        nfo_path = os.path.join(output_dir, f"{dir_name}.nfo")
        ET.indent(root, space="  ")
        tree = ET.ElementTree(root)
        tree.write(nfo_path, encoding="utf-8", xml_declaration=True)
        print(f"[OK] NFO 已生成: {nfo_path}")
        return nfo_path

    # ── 图片下载（使用浏览器 cookies） ────────────────────────────────────────

    def _make_request_session(self):
        session = requests.Session()
        session.headers.update({
            "User-Agent": self.driver.execute_script("return navigator.userAgent"),
            "Referer": self.driver.current_url,
        })
        for cookie in self.driver.get_cookies():
            session.cookies.set(cookie['name'], cookie['value'],
                                domain=cookie.get('domain', ''))
        return session

    def download_cover(self, cover_url, output_dir):
        if not cover_url:
            print("[WARN] 未获取到封面图 URL，跳过下载")
            return False
        try:
            session = self._make_request_session()
            resp = session.get(cover_url, timeout=30)
            resp.raise_for_status()
            if len(resp.content) < 1000:
                print(f"[WARN] 封面图过小 ({len(resp.content)} bytes)")
                return False
            dir_name = os.path.basename(output_dir.rstrip(os.sep))
            jacket_path = os.path.join(output_dir, "jacket.jpg")
            with open(jacket_path, "wb") as f:
                f.write(resp.content)
            print(f"[OK] 封面图已保存: {jacket_path}")
            for fname in ("folder.jpg", "poster.jpg", f"{dir_name}-poster.jpg"):
                shutil.copy2(jacket_path, os.path.join(output_dir, fname))
                print(f"[OK] 已复制封面: {fname}")
            return True
        except Exception as e:
            print(f"[ERROR] 下载封面图失败: {e}")
            return False

    def download_gallery(self, gallery_urls, output_dir):
        if not gallery_urls:
            print("[WARN] 没有可用的剧照 URL，跳过")
            return
        session = self._make_request_session()
        ok_count = 0
        for idx, img_url in enumerate(gallery_urls, start=1):
            save_path = os.path.join(output_dir, f"backdrop{idx}.jpg")
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

    def run(self, video_url, output_dir, download=True, media_type="movie"):
        """主流程
    
        Args:
            media_type: "movie" 或 "episode"
        """
        self.setup_driver()
        try:
            self.login()

            # ── 如果传入的是 vixenplus.com URL，先跳转到品牌站详情页 ──────────
            video_url = self.resolve_vixenplus_to_brand_url(video_url)

            # ── 第一步：刮削视频页面 ──────────────────────────────────────────
            data = self.scrape_video_page(video_url)

            # ── 第二步：去 VixenPlus 搜索获取 genre/tag ────────────────────────
            vixenplus_tags = self.search_vixenplus_for_tags(
                data.get("title", ""), data.get("actors", [])
            )
            if vixenplus_tags:
                print(f"[VIXEN+] 原始标签 ({len(vixenplus_tags)} 个): {vixenplus_tags}")
                data["genres"] = self._apply_tag_convert(vixenplus_tags)
            else:
                # 没有从 VixenPlus 获取到，用原始页面标签
                if data["genres"]:
                    print(f"[TAG] 使用原始页面标签 ({len(data['genres'])} 个): "
                          f"{data['genres']}")
                    data["genres"] = self._apply_tag_convert(data["genres"])
                else:
                    data["genres"] = []

            # ── 第三步：生成 NFO ──────────────────────────────────────────────
            self.generate_nfo(data, output_dir, media_type=media_type)
            # ── 第四步：下载封面和剧照 ────────────────────────────────────────
            if download:
                self.download_cover(data.get("cover", ""), output_dir)
                self.download_gallery(data.get("gallery_urls", []), output_dir)

            # 摘要
            print(f"\n{'='*50}")
            print(f"  标题  : {data['title']}")
            print(f"  工作室: {data['studio']}")
            print(f"  日期  : {data['date']}")
            print(f"  时长  : {data['runtime']} 分钟")
            print(f"  导演  : {data.get('director', '')}")
            print(f"  演员  : {', '.join(data['actors'])}")
            print(f"  标签  : {', '.join(data['genres'])}")
            print(f"  封面  : {data['cover']}")
            print(f"{'='*50}\n")

        finally:
            self.close()

    # ── 解析辅助方法 ──────────────────────────────────────────────────────────

    @staticmethod
    def _normalize_text(text):
        if not text:
            return ""
        result = text.lower()
        result = re.sub(r'[^0-9a-z\u4e00-\u9fff]+', ' ', result)
        return re.sub(r'\s+', ' ', result).strip()

    @staticmethod
    def _extract_next_data(html):
        match = re.search(
            r'<script[^>]+id=["\']__NEXT_DATA__["\'][^>]*>(.*?)</script>', html, re.S
        )
        if match:
            try:
                return json.loads(match.group(1))
            except json.JSONDecodeError:
                pass
        return {}

    @staticmethod
    def _extract_json_ld(html):
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
    def _parse_date(raw):
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

    def _deep_find(self, obj, key, depth=4):
        if depth <= 0:
            return None
        if isinstance(obj, dict):
            if key in obj:
                return obj[key]
            for v in obj.values():
                r = self._deep_find(v, key, depth - 1)
                if r is not None:
                    return r
        elif isinstance(obj, list):
            for item in obj:
                r = self._deep_find(item, key, depth - 1)
                if r is not None:
                    return r
        return None

    def _parse_from_next_data(self, next_data, data):
        """从 Next.js __NEXT_DATA__ JSON 中提取字段"""
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

            if not data["title"]:
                data["title"] = video_obj.get("title") or video_obj.get("name") or ""

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

    def _parse_from_json_ld(self, jld, data):
        """从 JSON-LD 数据中补充字段"""
        schema_type = jld.get("@type", "")
        if schema_type not in ("Movie", "VideoObject", "TVEpisode", "CreativeWork"):
            return

        if not data["title"]:
            data["title"] = jld.get("name") or jld.get("headline") or ""

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

    def _parse_from_html(self, soup, data):
        """从 HTML 标签中提取元数据，始终提取 Brand 作为 studio"""
        if not data["title"]:
            og_title = soup.find("meta", property="og:title")
            if og_title:
                data["title"] = og_title.get("content", "").strip()
            if not data["title"]:
                h1 = soup.find("h1")
                if h1:
                    data["title"] = h1.get_text(strip=True)

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

        # actors — 遇到 "&" 后停止（后面是男演员）
        perf_links = soup.find_all(
            "a", href=re.compile(r'/(pornstars|models|performers?)/')
        )
        if perf_links:
            parent = perf_links[0].parent
            female_actors = []
            for child in parent.children:
                if hasattr(child, 'get_text'):
                    text = child.get_text(strip=True)
                    # if text == '&':
                    #     break
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
    parser = argparse.ArgumentParser(
        description="Vixen Group Scraper — 根据域名自动识别品牌并刮削",
        epilog="""
            使用示例:
            python vixengroup_scraper.py "https://www.blacked.com/videos/..." "\\\\server\\path\\Blacked.23.01.21..."
            python vixengroup_scraper.py "https://www.vixen.com/videos/..." "/path/to/Vixen.24.05.10..." --no-download

            工作流程:
            1. 打开浏览器，手动登录 VixenPlus
            2. 刮削视频页面（支持 Vixen/Blacked/Tushy/BlackedRaw/TushyRaw）
            3. 去 VixenPlus 搜索同一视频获取 genre/tag
            4. 用 tag_convert.json 转换标签（未映射的删除）
            5. 生成 NFO，下载封面和剧照
        """,
        formatter_class=argparse.RawDescriptionHelpFormatter
    )
    parser.add_argument("video_url", help="视频页面 URL")
    parser.add_argument("output_dir", help="输出目录路径")
    parser.add_argument(
        "--no-download",
        action="store_true",
        default=False,
        help="不下载封面和剧照（默认下载）"
    )
    parser.add_argument(
        "--type",
        choices=["movie", "episode"],
        default="movie",
        dest="media_type",
        help="媒体类型: movie (NFO根节点<movie>) 或 episode (NFO根节点<episodedetails>)，默认 movie"
    )
    parser.add_argument(
        "--tag-convert",
        default="tag_convert.json",
        help="标签映射文件 (默认: tag_convert.json)"
    )

    args = parser.parse_args()

    print("Vixen Group Scraper")
    print("=" * 50)
    print(f"视频 URL: {args.video_url}")
    print(f"输出目录: {args.output_dir}")
    print(f"下载图片: {'否' if args.no_download else '是'}")
    print(f"媒体类型: {'剧集(episode)' if args.media_type == 'episode' else '电影(movie)'}")
    print()

    scraper = VixenGroupScraper(tag_convert_file=args.tag_convert)
    try:
        scraper.run(
            video_url=args.video_url,
            output_dir=args.output_dir,
            download=not args.no_download,
            media_type=args.media_type,
        )
    except KeyboardInterrupt:
        print("\n[INFO] 用户中断，正在退出...")
        sys.exit(0)
    except Exception as e:
        print(f"[ERROR] 刮削失败: {e}")
        sys.exit(1)


if __name__ == "__main__":
    main()
