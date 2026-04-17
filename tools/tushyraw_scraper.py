#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
tushyraw.com 详情页刮削器
用法：
    python tushyrawscraper.py <video_url> <output_dir>
示例：
    python tushyrawscraper.py "https://www.tushyraw.com/videos/caught-in-the-moment" "G:/Jav/tushyraw.19.05.14.Caught.In.The.Moment.XXX.1080p"
"""

import os
import re
import sys
import json
import argparse
import xml.etree.ElementTree as ET
from xml.dom import minidom
from datetime import datetime

import requests
from bs4 import BeautifulSoup, NavigableString

try:
    import cloudscraper
    _HAS_CLOUDSCRAPER = True
except ImportError:
    _HAS_CLOUDSCRAPER = False


STUDIO = "tushyRaw"
DOMAIN = "www.tushyraw.com"
COOKIE_DOMAIN = ".tushyraw.com"

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

def fetch_page(url: str, proxy: str = None) -> str:
    """获取页面 HTML 内容，优先使用 cloudscraper 绕过 CloudFlare"""
    proxies = {"http": proxy, "https": proxy} if proxy else None

    if _HAS_CLOUDSCRAPER:
        scraper = cloudscraper.create_scraper(
            browser={"browser": "chrome", "platform": "windows", "mobile": False}
        )
        scraper.headers.update(HEADERS)
        scraper.cookies.set("consent", "1", domain=COOKIE_DOMAIN)
        if proxies:
            scraper.proxies.update(proxies)
        resp = scraper.get(url, timeout=30, allow_redirects=True)
        resp.raise_for_status()
        return resp.text
    else:
        session = requests.Session()
        session.headers.update(HEADERS)
        session.cookies.set("consent", "1", domain=COOKIE_DOMAIN)
        if proxies:
            session.proxies.update(proxies)
        resp = session.get(url, timeout=30, allow_redirects=True)
        resp.raise_for_status()
        return resp.text


# ---------------------------------------------------------------------------
# 解析工具函数
# ---------------------------------------------------------------------------

def _extract_next_data(html: str) -> dict:
    """从 Next.js __NEXT_DATA__ script 标签提取 JSON 数据"""
    match = re.search(r'<script[^>]+id=["\']__NEXT_DATA__["\'][^>]*>(.*?)</script>', html, re.S)
    if match:
        try:
            return json.loads(match.group(1))
        except json.JSONDecodeError:
            pass
    return {}


def _extract_json_ld(html: str) -> list:
    """提取所有 JSON-LD 数据"""
    results = []
    for m in re.finditer(r'<script[^>]+type=["\']application/ld\+json["\'][^>]*>(.*?)</script>', html, re.S):
        try:
            results.append(json.loads(m.group(1)))
        except json.JSONDecodeError:
            pass
    return results


def _parse_date(raw: str) -> str:
    """将各种日期字符串规范化为 YYYY-MM-DD"""
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


def _parse_runtime(rt: str) -> str:
    """将 HH:MM:SS 或 ISO PT30M 格式转为分钟字符串"""
    if not rt:
        return ""
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


def _deep_find(obj, key: str, depth: int = 4):
    """在嵌套字典中深度查找指定 key"""
    if depth <= 0:
        return None
    if isinstance(obj, dict):
        if key in obj:
            return obj[key]
        for v in obj.values():
            result = _deep_find(v, key, depth - 1)
            if result is not None:
                return result
    elif isinstance(obj, list):
        for item in obj:
            result = _deep_find(item, key, depth - 1)
            if result is not None:
                return result
    return None


# ---------------------------------------------------------------------------
# 主要解析逻辑
# ---------------------------------------------------------------------------

def scrape_tushyraw(url: str, proxy: str = None) -> dict:
    """
    刮削 tushyraw.com 视频详情页，返回结构化数据字典：
    {
        "title": str,
        "plot": str,
        "date": str,          # YYYY-MM-DD
        "actors": list[str],  # 只含 & 前面的女演员
        "genres": list[str],
        "cover": str,         # 封面图 URL（最高分辨率横图）
        "url": str,
        "studio": str,
        "runtime": str,       # 分钟数字符串
        "director": str,
        "gallery_urls": list, # 剧照原图 URL 列表
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
        "cover": "",
        "url": url,
        "studio": STUDIO,
        "runtime": "",
        "director": "",
        "gallery_urls": [],
    }

    # ── 1. 优先从 __NEXT_DATA__ 中解析 ──────────────────────────────────────
    next_data = _extract_next_data(html)
    if next_data:
        _parse_from_next_data(next_data, data)

    # ── 2. 若关键字段缺失，补充从 JSON-LD 解析 ───────────────────────────────
    json_ld_list = _extract_json_ld(html)
    for jld in json_ld_list:
        _parse_from_json_ld(jld, data)

    # ── 3. 兜底：用 BeautifulSoup 直接解析 HTML（演员始终用此结果覆盖）────────
    _parse_from_html(soup, data)

    return data


def _parse_from_next_data(next_data: dict, data: dict):
    """从 Next.js 的 __NEXT_DATA__ JSON 中提取字段"""
    try:
        page_props = next_data.get("props", {}).get("pageProps", {})

        video_obj = (
            page_props.get("video")
            or page_props.get("videoData")
            or page_props.get("data", {}).get("video")
            or _deep_find(page_props, "video")
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
            data["date"] = _parse_date(raw_date)

        if not data["runtime"]:
            rt = (
                video_obj.get("runLength")
                or video_obj.get("runLengthFormatted")
                or video_obj.get("duration")
                or ""
            )
            data["runtime"] = _parse_runtime(rt)

        # 演员 — 使用 modelsSlugged 字段（兜底，最终由 HTML 解析覆盖）
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

        # 导演
        if not data.get("director"):
            directors = video_obj.get("directors") or []
            if isinstance(directors, list) and directors:
                names = [d.get("name", "") for d in directors if isinstance(d, dict) and d.get("name")]
                if names:
                    data["director"] = ", ".join(names)

        # 标签/类型
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

        # 封面图 — 优先选最高分辨率的 mainLandscape
        if not data["cover"]:
            images = video_obj.get("images", {})
            if isinstance(images, dict):
                img_list = images.get("listing") or images.get("main") or []
                best_url = ""
                best_w = 0
                for img in img_list:
                    if isinstance(img, dict):
                        w = img.get("width", 0) or 0
                        src = img.get("src") or ""
                        if w > best_w and src:
                            best_w = w
                            best_url = src
                if best_url:
                    data["cover"] = best_url

            if not data["cover"]:
                vi = video_obj.get("videoImage") or {}
                if isinstance(vi, dict):
                    data["cover"] = vi.get("src") or ""

            if not data["cover"]:
                sd = page_props.get("structuredData") or {}
                data["cover"] = sd.get("thumbnailUrl") or ""

        # 剧照原图 — 优先用 pageProps.galleryImages
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
                            src = main_list[0].get("src") or "" if isinstance(main_list[0], dict) else ""
                            if src:
                                data["gallery_urls"].append(src)

    except Exception as e:
        print(f"[WARN] 解析 __NEXT_DATA__ 时出错: {e}")


def _parse_from_json_ld(jld: dict, data: dict):
    """从 JSON-LD 数据中补充字段"""
    schema_type = jld.get("@type", "")
    if schema_type not in ("Movie", "VideoObject", "TVEpisode", "CreativeWork"):
        return

    if not data["title"]:
        data["title"] = jld.get("name") or jld.get("headline") or ""

    if not data["plot"]:
        data["plot"] = jld.get("description") or ""

    if not data["date"]:
        raw = (
            jld.get("datePublished")
            or jld.get("uploadDate")
            or jld.get("dateCreated")
            or ""
        )
        data["date"] = _parse_date(raw)

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
            img = (i0.get("url") or i0.get("contentUrl") or "") if isinstance(i0, dict) else str(i0)
        data["cover"] = str(img)

    if not data["runtime"]:
        data["runtime"] = _parse_runtime(jld.get("duration") or "")


def _parse_from_html(soup: BeautifulSoup, data: dict):
    """直接从 HTML 标签中提取元数据（兜底策略，演员部分始终覆盖）"""
    # title
    if not data["title"]:
        og_title = soup.find("meta", property="og:title")
        if og_title:
            data["title"] = og_title.get("content", "").strip()
        if not data["title"]:
            h1 = soup.find("h1")
            if h1:
                data["title"] = h1.get_text(strip=True)

    # description / plot
    if not data["plot"]:
        og_desc = soup.find("meta", property="og:description")
        if og_desc:
            data["plot"] = og_desc.get("content", "").strip()
        if not data["plot"]:
            meta_desc = soup.find("meta", attrs={"name": "description"})
            if meta_desc:
                data["plot"] = meta_desc.get("content", "").strip()

    # cover image
    if not data["cover"]:
        og_img = soup.find("meta", property="og:image")
        if og_img:
            data["cover"] = og_img.get("content", "").strip()

    # date
    if not data["date"]:
        time_tag = soup.find("time")
        if time_tag:
            raw = time_tag.get("datetime") or time_tag.get_text(strip=True)
            data["date"] = _parse_date(raw)

    # actors — 从包含 performer 链接的父容器中按顺序收集，遇到 "&" span 即停
    perf_links = soup.find_all("a", href=re.compile(r'/(pornstars|models|performers?)/'))
    if perf_links:
        parent = perf_links[0].parent
        female_actors = []
        for child in parent.children:
            if hasattr(child, 'get_text'):
                text = child.get_text(strip=True)
                if text == '&':
                    break
                if child.name == 'a' and re.search(r'/(pornstars|models|performers?)/', child.get('href', '')):
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


# ---------------------------------------------------------------------------
# NFO 生成
# ---------------------------------------------------------------------------

def build_nfo_xml(info: dict, dir_name: str = "") -> str:
    """根据 info 字典构建 Kodi/Emby 兼容的 NFO XML 字符串

    sorttitle 从 dir_name 中用正则提取 tushyraw.YY.MM.DD 部分，
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

    sub("studio", info.get("studio", STUDIO))

    # uniqueid: url
    uid_url = ET.SubElement(root, "uniqueid")
    uid_url.set("type", "tushyrawScraper-Url")
    uid_url.text = info.get("url", "")

    # uniqueid: json 摘要
    import json as _json
    json_str = _json.dumps({
        "OriginalTitle": originaltitle,
        "Cover": info.get("cover", ""),
        "Date": date,
    }, ensure_ascii=False)
    uid_json = ET.SubElement(root, "uniqueid")
    uid_json.set("type", "tushyrawScraper-Json")
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
            browser={"browser": "chrome", "platform": "windows", "mobile": False}
        )
    else:
        s = requests.Session()
    s.headers.update(HEADERS)
    s.cookies.set("consent", "1", domain=COOKIE_DOMAIN)
    if proxy:
        s.proxies.update({"http": proxy, "https": proxy})
    return s


def download_cover(cover_url: str, output_dir: str, dir_name: str, proxy: str = None) -> bool:
    """下载封面图并保存为 jacket.jpg / folder.jpg / poster.jpg / {dir_name}-poster.jpg"""
    if not cover_url:
        print("[WARN] 未获取到封面图 URL，跳过下载")
        return False
    try:
        import shutil
        session = _make_http_session(proxy)
        resp = session.get(cover_url, timeout=30)
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
        fname = f"backdrop{idx}.jpg"
        save_path = os.path.join(dir_name, fname)
        try:
            resp = session.get(img_url, timeout=30)
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
        description="tushyraw.com 视频详情页刮削器，将元数据保存为 Kodi/Emby NFO 文件"
    )
    parser.add_argument(
        "url",
        help="tushyraw 视频详情页 URL，例如 https://www.tushyraw.com/videos/caught-in-the-moment",
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
        help="不下载剧照（默认会下载全部剧照到 extrafanart/）",
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
        info = scrape_tushyraw(args.url, proxy=args.proxy)
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
    print(f"  时长  : {info['runtime']} 分钟")
    print(f"  导演  : {info.get('director', '')}")
    print(f"  演员  : {', '.join(info['actors'])}")
    print(f"  标签  : {', '.join(info['genres'])}")
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
    if not args.no_gallery:
        fanart_dir = args.output_dir
        download_gallery(info.get("gallery_urls", []), fanart_dir, proxy=args.proxy)

    print(f"\n[DONE] 完成！NFO 路径: {nfo_path}")


if __name__ == "__main__":
    main()
