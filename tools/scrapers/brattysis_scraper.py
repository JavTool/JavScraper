#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
brattysis.com 详情页刮削器
用法：
    python brattysis_scraper.py <video_url> <output_dir>
示例：
    python brattysis_scraper.py "https://brattysis.com/video/watch/91402/lil-step-sis-works-out-my-cock-s13e1" "G:/Jav/AdultEmpire.25.10.15.LifeSelector.Studios.XXX.1080p"
"""

import os
import re
import sys
import json
import ssl
import argparse
import xml.etree.ElementTree as ET
from xml.dom import minidom
from datetime import datetime

import time
import random
import requests
import urllib3
from bs4 import BeautifulSoup, NavigableString

# 禁用 SSL 警告
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

def _create_unverified_ssl_context():
    """创建一个禁用主机名检查和证书验证的 SSL 上下文"""
    context = ssl.create_default_context()
    context.check_hostname = False
    context.verify_mode = ssl.CERT_NONE
    return context

try:
    import cloudscraper
    _HAS_CLOUDSCRAPER = True
except ImportError:
    _HAS_CLOUDSCRAPER = False


STUDIO = "Bratty Sis"
DOMAIN = "www.brattysis.com"
COOKIE_DOMAIN = "www.brattysis.com"

HEADERS = {
    "User-Agent": (
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) "
        "AppleWebKit/537.36 (KHTML, like Gecko) "
        "Chrome/124.0.0.0 Safari/537.36"
    ),
    "Accept": "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8",
    "Accept-Language": "en-US,en;q=0.9",
    "Referer": f"https://{DOMAIN}/",
}


# ---------------------------------------------------------------------------
# 网络请求
# ---------------------------------------------------------------------------

def fetch_page(url: str, proxy: str = None, max_retries: int = 5) -> str:
    """获取页面 HTML 内容，优先使用 cloudscraper 绕过 CloudFlare。
    遇到 429 Too Many Requests 时指数退避重试。
    """
    proxies = {"http": proxy, "https": proxy} if proxy else None

    if _HAS_CLOUDSCRAPER:
        scraper = cloudscraper.create_scraper(
            browser={"browser": "chrome", "platform": "windows", "mobile": False},
            ssl_context=_create_unverified_ssl_context()
        )
        scraper.headers.update(HEADERS)
        scraper.cookies.set("18-plus-modal", "hidden", domain=COOKIE_DOMAIN)
        if proxies:
            scraper.proxies.update(proxies)
        session = scraper
    else:
        session = requests.Session()
        session.headers.update(HEADERS)
        session.cookies.set("18-plus-modal", "hidden", domain=COOKIE_DOMAIN)
        if proxies:
            session.proxies.update(proxies)

    for attempt in range(1, max_retries + 1):
        resp = session.get(url, timeout=30, allow_redirects=True, verify=False)
        if resp.status_code == 429:
            retry_after = int(resp.headers.get("Retry-After", 0))
            wait = retry_after if retry_after > 0 else (2 ** attempt + random.uniform(1, 3))
            print(f"[WARN] 429 Too Many Requests，第 {attempt}/{max_retries} 次重试，"
                  f"等待 {wait:.1f} 秒...")
            if attempt == max_retries:
                resp.raise_for_status()
            time.sleep(wait)
            continue
        resp.raise_for_status()
        return resp.text

    resp.raise_for_status()  # 不可达，保险用
    return resp.text


# ---------------------------------------------------------------------------
# 解析工具函数
# ---------------------------------------------------------------------------

def _parse_date(raw: str) -> str:
    """将各种日期字符串规范化为 YYYY-MM-DD"""
    if not raw:
        return ""
    m = re.match(r'(\d{4}-\d{2}-\d{2})', raw)
    if m:
        return m.group(1)
    # 格式: Oct 15 2025
    try:
        dt = datetime.strptime(raw.strip(), "%b %d %Y")
        return dt.strftime("%Y-%m-%d")
    except ValueError:
        pass
    # 格式: October 15, 2025
    try:
        dt = datetime.strptime(raw.strip(), "%B %d, %Y")
        return dt.strftime("%Y-%m-%d")
    except ValueError:
        pass
    return raw.strip()


def _parse_runtime(rt: str) -> str:
    """将 HH:MM:SS 或 '1 hrs. 18 mins.' 格式转为分钟字符串"""
    if not rt:
        return ""
    # 格式: 1 hrs. 18 mins.
    h_m = re.match(r'(?:(\d+)\s*hrs\.\s*)?(?:(\d+)\s*mins\.)?', rt.strip())
    if h_m and (h_m.group(1) or h_m.group(2)):
        hours = int(h_m.group(1) or 0)
        minutes = int(h_m.group(2) or 0)
        return str(hours * 60 + minutes)

    hms = re.match(r'(\d+):(\d+):(\d+)', str(rt))
    if hms:
        h, m, s = int(hms.group(1)), int(hms.group(2)), int(hms.group(3))
        return str(h * 60 + m + (1 if s >= 30 else 0))
    iso = re.match(r'PT(?:(\d+)H)?(?:(\d+)M)?', str(rt))
    if iso:
        hours = int(iso.group(1) or 0)
        minutes = int(iso.group(2) or 0)
        return str(hours * 60 + minutes)
    return str(rt)


# ---------------------------------------------------------------------------
# 主要解析逻辑
# ---------------------------------------------------------------------------

def scrape_brattysis(url: str, proxy: str = None) -> dict:
    """
    刮削 brattysis.com 视频详情页，返回结构化数据字典：
    {
        "title": str,
        "plot": str,
        "date": str,          # YYYY-MM-DD
        "actors": list[str],
        "genres": list[str],
        "cover": str,         # 封面图 URL
        "url": str,
        "studio": str,
        "runtime": str,       # 分钟数字符串
        "director": str,
    }
    """
    print(f"[INFO] 正在获取页面: {url}")
    html = fetch_page(url, proxy=proxy)
    soup = BeautifulSoup(html, "html.parser")

    data = {
        "title": "",
        "plot": "",
        "date": "",
        "actors": [],
        "genres": [],
        "tags": [],
        "cover": "",
        "url": url,
        "studio": STUDIO,
        "runtime": "",
        "director": "",
        "gallery_urls": [],
    }

    _parse_from_html(soup, data)

    return data


def _parse_from_html(soup: BeautifulSoup, data: dict):
    """直接从 HTML 标签中提取 brattysis.com 元数据"""

    # 标题 - 在 div.content-pane-title 中的 h2
    title_div = soup.find("div", class_="content-pane-title")
    if title_div:
        h2 = title_div.find("h2")
        if h2:
            data["title"] = " ".join(h2.get_text().split()).strip()

    # 封面图 - 优先从 video poster 获取 cover1280，其次 og:image
    video_tag = soup.find("video", class_="vjs-tech")
    if video_tag:
        poster = video_tag.get("poster", "").strip()
        if poster:
            data["cover"] = poster

    if not data["cover"]:
        og_img = soup.find("meta", property="og:image")
        if og_img:
            data["cover"] = og_img.get("content", "").strip()

    # 简介 - div.content-pane-description 中的 <p> 段落
    desc_div = soup.find("div", class_="content-pane-description")
    if desc_div:
        paragraphs = desc_div.find_all("p")
        if paragraphs:
            plot_parts = [" ".join(p.get_text().split()).strip() for p in paragraphs]
            data["plot"] = "\n".join(p for p in plot_parts if p)
        else:
            data["plot"] = " ".join(desc_div.get_text().split()).strip()

    # 日期 - div.content-pane-title 中的 span.date
    if title_div:
        date_span = title_div.find("span", class_="date")
        if date_span:
            data["date"] = _parse_date(date_span.get_text(strip=True))

    # 演员 - div.content-pane-performers 中的 a.content-pane-performer.model
    performers_div = soup.find("div", class_="content-pane-performers")
    if performers_div:
        links = performers_div.find_all("a", class_="content-pane-performer model")
        data["actors"] = [link.get_text(strip=True) for link in links]

    # 分类 / 标签 - div.categories 中的 a
    categories_div = soup.find("div", class_="categories")
    if categories_div:
        links = categories_div.find_all("a")
        raw_genres = [link.get_text(strip=True) for link in links]
        # 过滤掉站点自身分类
        filter_out = ["BrattySis.com"]
        data["genres"] = [g for g in raw_genres if g not in filter_out]
        data["tags"] = data["genres"][:]

    # 剧照 - div.content-pane-pick img，去掉 /tn/ 获取大图
    picks = soup.find_all("div", class_="content-pane-pick")
    gallery_urls = []
    for pick in picks:
        img = pick.find("img")
        if img:
            src = img.get("src", "").strip()
            if src:
                # 缩略图路径包含 /tn/，去掉后尝试获取原图
                if "/tn/" in src:
                    src = src.replace("/tn/", "/")
                gallery_urls.append(src)
    if gallery_urls:
        data["gallery_urls"] = gallery_urls


# ---------------------------------------------------------------------------
# NFO 生成
# ---------------------------------------------------------------------------

def build_nfo_xml(info: dict, dir_name: str = "") -> str:
    """根据 info 字典构建 Kodi/Emby 兼容的 NFO XML 字符串

    sorttitle 从 dir_name 中用正则提取 studio.YY.MM.DD 部分，
    originaltitle = sorttitle + " " + title（不含 [中字] 前缀）
    目录名带 -C 后缀时：title 加 [中字]，sorttitle 加 -C 后缀
    """
    root = ET.Element("movie")

    def sub(tag, text):
        el = ET.SubElement(root, tag)
        el.text = str(text) if text is not None else ""
        return el

    title = info.get("title", "")

    # 从目录名提取 Studio.YY.MM.DD 格式的前缀
    sorttitle_match = re.search(r'([A-Za-z]+\.\d{2}\.\d{2}\.\d{2})', dir_name)
    sorttitle = sorttitle_match.group(1) if sorttitle_match else ""

    # 目录名带 -C 后缀表示中文字幕
    is_chinese_sub = bool(re.search(r'-C(?:[^A-Za-z]|$)', dir_name))
    display_title = f"[中字] {title}" if is_chinese_sub else title
    effective_sorttitle = (f"{sorttitle}-C" if is_chinese_sub else sorttitle) if sorttitle else title

    # originaltitle 不含 [中字]，只是 sorttitle + 空格 + 原始 title
    originaltitle = f"{sorttitle} {title}".strip() if sorttitle else title

    # plot
    plot_el = ET.SubElement(root, "plot")
    plot_el.text = info.get("plot", "")

    sub("outline", "")
    sub("lockdata", "true")
    sub("dateadded", info.get("date", ""))
    sub("title", display_title)
    sub("originaltitle", originaltitle)

    # actors
    for actor_name in info.get("actors", []):
        actor_el = ET.SubElement(root, "actor")
        n = ET.SubElement(actor_el, "name")
        n.text = actor_name
        t = ET.SubElement(actor_el, "type")
        t.text = "Actor"

    date = info.get("date", "")
    year = date[:4] if len(date) >= 4 else ""
    sub("year", year)
    sub("sorttitle", effective_sorttitle)
    sub("mpaa", "XXX")
    sub("premiered", date)
    sub("releasedate", date)
    sub("runtime", info.get("runtime", ""))

    if info.get("director"):
        sub("director", info["director"])

    for genre in info.get("genres", []):
        sub("genre", genre)

    for tag in info.get("tags", []):
        sub("tag", tag)

    sub("studio", info.get("studio", STUDIO))

    # uniqueid: url
    uid_url = ET.SubElement(root, "uniqueid")
    uid_url.set("type", "BrattySisScraper-Url")
    uid_url.text = info.get("url", "")

    # uniqueid: json 摘要
    import json as _json
    json_str = _json.dumps({
        "OriginalTitle": originaltitle,
        "Cover": info.get("cover", ""),
        "Date": date,
    }, ensure_ascii=False)
    uid_json = ET.SubElement(root, "uniqueid")
    uid_json.set("type", "BrattySisScraper-Json")
    uid_json.text = json_str

    # 美化输出
    dom2 = minidom.parseString(ET.tostring(root, encoding="unicode"))
    pretty = dom2.toprettyxml(indent="  ", encoding="utf-8").decode("utf-8")
    return pretty


def save_nfo(info: dict, output_dir: str) -> str:
    """将 NFO 写入 <output_dir>/<dirname>.nfo"""
    dir_name = os.path.basename(output_dir.rstrip(os.sep))
    nfo_filename = f"{dir_name}.nfo"
    nfo_path = os.path.join(output_dir, nfo_filename)

    os.makedirs(output_dir, exist_ok=True)

    xml_str = build_nfo_xml(info, dir_name=dir_name)
    with open(nfo_path, "w", encoding="utf-8") as f:
        f.write(xml_str)

    print(f"[OK] NFO 已保存: {nfo_path}")
    return nfo_path


# ---------------------------------------------------------------------------
# 图片下载
# ---------------------------------------------------------------------------

def _make_http_session(proxy: str = None):
    """创建带 cookie 和代理的 HTTP Session"""
    if _HAS_CLOUDSCRAPER:
        s = cloudscraper.create_scraper(
            browser={"browser": "chrome", "platform": "windows", "mobile": False},
            ssl_context=_create_unverified_ssl_context()
        )
    else:
        s = requests.Session()
    s.headers.update(HEADERS)
    s.cookies.set("ageConfirmed", "true", domain=COOKIE_DOMAIN)
    if proxy:
        s.proxies.update({"http": proxy, "https": proxy})
    # 针对 requests.Session 的 verify 设置通常在 get 调用时
    return s


def download_cover(cover_url: str, output_dir: str, dir_name: str, proxy: str = None) -> bool:
    """下载封面图并保存为 jacket.jpg / folder.jpg / poster.jpg / {dir_name}-poster.jpg"""
    if not cover_url:
        print("[WARN] 未获取到封面图 URL，跳过下载")
        return False
    try:
        import shutil
        session = _make_http_session(proxy)
        resp = session.get(cover_url, timeout=30, verify=False)
        resp.raise_for_status()
        if len(resp.content) < 1000:
            print(f"[WARN] 封面图内容过小 ({len(resp.content)} bytes)，可能无效")
            return False

        jacket_path = os.path.join(output_dir, "jacket.jpg")
        with open(jacket_path, "wb") as f:
            f.write(resp.content)
        print(f"[OK] 封面图已保存: {jacket_path}")

        for fname in ("folder.jpg", "poster.jpg", f"{dir_name}-poster.jpg"):
            dst = os.path.join(output_dir, fname)
            shutil.copy2(jacket_path, dst)
            print(f"[OK] 已复制封面: {dst}")
        return True
    except Exception as e:
        print(f"[ERROR] 下载封面图失败: {e}")
        return False


def download_gallery(gallery_urls: list, dir_name: str, proxy: str = None):
    """下载剧照到 fanart_dir 目录，命名为 backdrop{n}.jpg"""
    if not gallery_urls:
        print("[WARN] 没有可用的剧照 URL，跳过")
        return

    os.makedirs(dir_name, exist_ok=True)
    session = _make_http_session(proxy)
    ok_count = 0

    for idx, img_url in enumerate(gallery_urls, start=1):
        print(f"[INFO] 开始下载剧照 {idx}/{len(gallery_urls)}: {img_url}")
        fname = f"backdrop{idx}.jpg"
        save_path = os.path.join(dir_name, fname)
        try:
            resp = session.get(img_url, timeout=30, verify=False)
            resp.raise_for_status()
            if len(resp.content) < 1000:
                print(f"[WARN] 剧照 {idx} 内容过小，跳过: {img_url}")
                continue
            with open(save_path, "wb") as f:
                f.write(resp.content)
            print(f"[OK] 剧照 {idx}/{len(gallery_urls)}: {save_path}")
            ok_count += 1
        except Exception as e:
            print(f"[ERROR] 下载剧照 {idx} 失败: {e}")

    print(f"[INFO] 剧照下载完成: {ok_count}/{len(gallery_urls)} 张，目录: {dir_name}")


# ---------------------------------------------------------------------------
# CLI 入口
# ---------------------------------------------------------------------------

def main():
    parser = argparse.ArgumentParser(
        description="brattysis.com 视频详情页刮削器，将元数据保存为 Kodi/Emby NFO 文件"
    )
    parser.add_argument(
        "url",
        help="brattysis 视频详情页 URL，例如 https://brattysis.com/video/watch/91402/lil-step-sis-works-out-my-cock-s13e1",
    )
    parser.add_argument("output_dir", help="输出目录，NFO 文件将以该目录名命名")
    parser.add_argument(
        "--no-cover",
        action="store_true",
        default=False,
        help="不下载封面图（默认会尝试下载）",
    )
    parser.add_argument(
        "--no-gallery",
        action="store_true",
        default=False,
        help="不下载剧照（默认会下载全部剧照到输出目录）",
    )
    parser.add_argument(
        "--proxy",
        default=None,
        help="HTTP/HTTPS 代理地址，例如 http://127.0.0.1:7890",
    )

    args = parser.parse_args()

    if args.proxy:
        print(f"[INFO] 使用代理: {args.proxy}")

    try:
        info = scrape_brattysis(args.url, proxy=args.proxy)
    except requests.exceptions.HTTPError as e:
        print(f"[ERROR] HTTP 错误: {e}")
        sys.exit(1)
    except requests.exceptions.ConnectionError as e:
        print(f"[ERROR] 连接失败: {e}")
        sys.exit(1)
    except Exception as e:
        print(f"[ERROR] 刮削失败: {e}")
        sys.exit(1)

    # 打印刮削结果摘要
    print("\n======= 刮削结果 =======")
    print(f"  标题  : {info['title']}")
    print(f"  日期  : {info['date']}")
    print(f"  时长  : {info['runtime']} 分钟" if info['runtime'] else "  时长  : (未提供)")
    print(f"  制片商: {info.get('studio', '')}")
    print(f"  导演  : {info.get('director', '')}" if info.get('director') else "  导演  : (未提供)")
    print(f"  演员  : {', '.join(info['actors'])}")
    print(f"  类型  : {', '.join(info['genres'])}")
    print(f"  标签  : {', '.join(info['tags'])}")
    print(f"  剧照  : {len(info.get('gallery_urls', []))} 张")
    print(f"  封面  : {info['cover']}")
    print(f"  简介  : {info['plot'][:80]}{'...' if len(info['plot']) > 80 else ''}")
    print("========================\n")

    # 写 NFO
    nfo_path = save_nfo(info, args.output_dir)

    dir_name = os.path.basename(args.output_dir.rstrip(os.sep))

    # 下载封面
    if not args.no_cover:
        download_cover(info["cover"], args.output_dir, dir_name, proxy=args.proxy)

    # 下载剧照
    if not args.no_gallery and info.get("gallery_urls"):
        fanart_dir = args.output_dir
        download_gallery(info.get("gallery_urls", []), fanart_dir, proxy=args.proxy)

    print(f"\n[DONE] 完成！NFO 路径: {nfo_path}")


if __name__ == "__main__":
    main()
