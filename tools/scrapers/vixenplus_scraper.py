#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
VixenPlus.com 详情页刮削器（需要登录 Cookie）
用法：
    python vixenplus_scraper.py <video_url> <output_dir> [--proxy http://...]
示例：
    python vixenplus_scraper.py "https://www.vixenplus.com/videos/some-video" "G:/222"
"""

import os
import re
import sys
import json
import shutil
import argparse
import xml.etree.ElementTree as ET
from xml.dom import minidom
from datetime import datetime
from http.cookiejar import MozillaCookieJar

import requests
from bs4 import BeautifulSoup

try:
    import cloudscraper
    _HAS_CLOUDSCRAPER = True
except ImportError:
    _HAS_CLOUDSCRAPER = False


# ---------------------------------------------------------------------------
# 站点常量
# ---------------------------------------------------------------------------

STUDIO = "VixenPlus"
BASE_DOMAIN = "vixenplus.com"
BASE_URL = "https://www.vixenplus.com"

HEADERS = {
    "User-Agent": (
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) "
        "AppleWebKit/537.36 (KHTML, like Gecko) "
        "Chrome/124.0.0.0 Safari/537.36"
    ),
    "Accept": "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8",
    "Accept-Language": "en-US,en;q=0.9",
    "Referer": BASE_URL + "/",
}

# ---------------------------------------------------------------------------
# 标签替换
# ---------------------------------------------------------------------------

# tag_convert.json 与本脚本同目录
_TAG_CONVERT_PATH = os.path.join(os.path.dirname(os.path.abspath(__file__)), "tag_convert.json")


def load_tag_convert(path: str = None) -> dict:
    """加载标签替换字典，key=英文原始标签（小写），value=替换后标签"""
    fpath = path or _TAG_CONVERT_PATH
    if not os.path.exists(fpath):
        print(f"[WARN] tag_convert.json 未找到: {fpath}")
        return {}
    with open(fpath, encoding="utf-8") as f:
        raw = json.load(f)
    # 统一 key 为小写，便于大小写不敏感匹配
    return {k.lower().strip(): v for k, v in raw.items()}


# 模块级缓存，避免每次刮削重复加载
_TAG_CONVERT: dict = None


def _get_tag_convert() -> dict:
    global _TAG_CONVERT
    if _TAG_CONVERT is None:
        _TAG_CONVERT = load_tag_convert()
    return _TAG_CONVERT


def apply_tag_convert(tags: list) -> list:
    """
    对标签列表进行替换：
    - 若标签在 tag_convert.json 中有对应映射，则替换为映射值
    - 匹配大小写不敏感
    - 未匹配的标签保留原值
    - 去重保持顺序
    """
    mapping = _get_tag_convert()
    seen = set()
    result = []
    for tag in tags:
        converted = mapping.get(tag.lower().strip(), tag)
        if converted not in seen:
            seen.add(converted)
            result.append(converted)
    return result


# ---------------------------------------------------------------------------
# 未匹配 NFO 日志
# ---------------------------------------------------------------------------

_NO_MATCH_LOG = os.path.join(os.path.dirname(os.path.abspath(__file__)), "vixenplus_no_match.log")


def log_no_match(nfo_path: str, reason: str = ""):
    """将未匹配到影片信息的 NFO 路径记录到日志文件"""
    import datetime as _dt
    ts = _dt.datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    line = f"[{ts}] {nfo_path}"
    if reason:
        line += f"  # {reason}"
    with open(_NO_MATCH_LOG, "a", encoding="utf-8") as f:
        f.write(line + "\n")
    print(f"[LOG] 未匹配记录已写入: {_NO_MATCH_LOG}")

# ---------------------------------------------------------------------------
# Cookie —— 从此处粘贴，或通过 --cookie-string 命令行参数传入
# ---------------------------------------------------------------------------

COOKIE_STRING = (
    "nats_landing=No%2BLanding%2BPage%2BURL; consent=0; "
    "_joinnow_prot=5f78b3a3-bbbd-4a21-a483-a029180a13f4; "
    "_gaexp_front_server=; _ga=GA1.1.948601088.1777381982; "
    "last_video_performer_page_viewed=homepage; "
    "redirect_to_joinform_button=header_shelf_join_now; "
    "__cuid=847f8ce1bb944570a5e29a7782c1d8f6; "
    "cf_clearance=0dJEuHGMh0znN1Z_oh5xjyoHp1ZX03Uo7wUD_UleAwM-1777382185-1.2.1.1-"
    "4bRrBTiHyC1sV6qHoNSe19Oudg_JqK1mT3HKu.rMWVd9SJyP2pxKp6FhqYVu0TJG.OMqV.ugnjHH0QvrT5lmZJr0CmdHjtvjuJqRa1KibNyck351VEhGYE2ubWsd3YSkJbVvCzzBH902TOlK83xREuKP89JlANSGVd6GxME.hjZAbwIsGaKwgRzbCJiaTfbHUdZXu7Z.Jg5zpaTWei2W81gK4PQF582zh4dWPcuMcysSX3BBrvd4unb5dceQ77XER6DU3XrGsEPsL4fG.qOD61a40lQkOM8VWnrun5bPZqnitTwhfG.vYRmUVfkLgDP46em3YbI0MVYW7Ja3WX1Y7Q; "
    "fesid=s%3Aa6Z9QfxnlG1E6A2EhxhP8uF2xgbcUSAY.Gr8X0EiKgjHpWQ18L5oUYtRS2Jmh9KjnmKbEP9rusDg; "
    "ujc=vxnp-initial%2Cvxnp-upgrade; "
    "aff_code_multi=MTgzNC4xMDIuMTYuNTMuMTk0LjAuMC4wLjA; "
    "vuid=0cc756e9-add9-42b0-a5f8-1d116a336981; "
    "sid=s%3A0_nmgBmg38UA8FkPhm80UszxEat_Q0E_.5NG9UwGYsJ%2BExRGxpjyp%2BhiXP3oIbE4jzmOUqlcdj64; "
    "access_token=eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCIsImtpZCI6Il9CYkwyTFNtckhfeC1Fb0trWGI1WUpDajFWWkcxT0JRWjR4V1RnZmRuS3MifQ."
    "eyJzdWIiOiI2OWYwYjNjNDAwMDEwMDAyMTdlN2M1ZWQiLCJ2YWxpZF9mcm9tIjoxNzc3Mzk0Nzk4OTQyLCJoYXNoIjoiMWZpUVRIMnZ1X1hJVWZCOFdFM3ZLTVBfTDZ6SE82el9XSkVFalViSUxldyIsImF1dGhfdGltZSI6MTc3NzM4MjQ0MywibGVnYWN5SWQiOiIxY2FiOWMzZS01OTY0LTQzOGItOWJlOS1lYjQzOGQ0YmJlZmUiLCJzY29wZXMiOnt9LCJkaXNwbGF5TmFtZSI6IndlaXNlbnpjaGFybGVzIiwic3VicyI6W3sicyI6InR1c2h5IiwidCI6InRyaWFsIiwidXVpZCI6IjUyY2RiMGJhLTEwNjAtNGU3Mi1hNDQyLTlhYjAzMWQ1NWZjNCIsIm1zIjp0cnVlLCJhYiI6ZmFsc2UsImFkIjpmYWxzZSwiYWRlIjpmYWxzZSwiaWUiOmZhbHNlLCJibiI6IlJHTkFUSVZFIiwiYnAiOiJWWE5CSUxMIiwibWQiOiJyZXN0cmljdGVkIn0seyJzIjoiYmxhY2tlZCIsInQiOiJ0cmlhbCIsInV1aWQiOiIyZjA0ZTJjZi1hNTdjLTQwY2ItYTY4ZC0yYTc2OWYwZWMxNmYiLCJtcyI6dHJ1ZSwiYWIiOmZhbHNlLCJhZCI6ZmFsc2UsImFkZSI6ZmFsc2UsImllIjpmYWxzZSwiYm4iOiJSR05BVElWRSIsImJwIjoiVlhOQklMTCIsIm1kIjoicmVzdHJpY3RlZCJ9XSwicGNzIjpbXSwiYXRfaGFzaCI6IjYzYlJwY2pYWTVDdV9xeldMU1R2dlEiLCJzaWQiOiIwZjJkMGRjMS05ZmVhLTQzOTEtYmEwMS1kMzE3ODQyZmNlZmUiLCJhdWQiOiJ2aXhlbnBsdXMiLCJleHAiOjE3NzczOTUzOTgsImlhdCI6MTc3NzM5NDc5OCwiaXNzIjoiaHR0cHM6Ly9sb2dpbi52aXhlbi5jb20ifQ.placeholder; "
    "refresh_token=7SHf2g2kpL~kVXP32XEFlM2VLkx"
)


# ---------------------------------------------------------------------------
# 工具函数
# ---------------------------------------------------------------------------

def _parse_cookie_string(cookie_str: str) -> dict:
    """将 Cookie 字符串解析为字典"""
    cookies = {}
    for part in cookie_str.split(";"):
        part = part.strip()
        if "=" in part:
            k, _, v = part.partition("=")
            cookies[k.strip()] = v.strip()
    return cookies


def _parse_date(raw: str) -> str:
    """将各种日期字符串规范化为 YYYY-MM-DD"""
    if not raw:
        return ""
    m = re.match(r"(\d{4}-\d{2}-\d{2})", raw)
    if m:
        return m.group(1)
    for fmt in ("%B %d, %Y", "%b %d, %Y", "%m/%d/%Y"):
        try:
            return datetime.strptime(raw.strip(), fmt).strftime("%Y-%m-%d")
        except ValueError:
            pass
    return raw.strip()


def _parse_runtime(rt) -> str:
    """将时长字段解析为分钟数字符串"""
    if not rt:
        return ""
    rt = str(rt)
    # HH:MM:SS
    hms = re.match(r"(\d+):(\d+):(\d+)", rt)
    if hms:
        h, m, s = int(hms.group(1)), int(hms.group(2)), int(hms.group(3))
        return str(h * 60 + m + (1 if s >= 30 else 0))
    # PT1H30M / PT30M
    iso = re.match(r"PT(?:(\d+)H)?(?:(\d+)M)?", rt)
    if iso:
        return str(int(iso.group(1) or 0) * 60 + int(iso.group(2) or 0))
    # 纯数字（秒）
    if rt.isdigit():
        return str(int(rt) // 60)
    return rt


def _deep_find(obj, key: str, depth: int = 5):
    """在嵌套字典/列表中深度查找指定 key 的值"""
    if depth <= 0:
        return None
    if isinstance(obj, dict):
        if key in obj:
            return obj[key]
        for v in obj.values():
            r = _deep_find(v, key, depth - 1)
            if r is not None:
                return r
    elif isinstance(obj, list):
        for item in obj:
            r = _deep_find(item, key, depth - 1)
            if r is not None:
                return r
    return None


# ---------------------------------------------------------------------------
# 网络请求
# ---------------------------------------------------------------------------

def _build_session(cookie_str: str, proxy: str = None) -> requests.Session:
    """构建带完整 Cookie 和请求头的 Session"""
    if _HAS_CLOUDSCRAPER:
        session = cloudscraper.create_scraper(
            browser={"browser": "chrome",
                     "platform": "windows", "mobile": False}
        )
    else:
        session = requests.Session()

    session.headers.update(HEADERS)

    # 注入 Cookie
    cookies = _parse_cookie_string(cookie_str)
    for k, v in cookies.items():
        session.cookies.set(k, v, domain=f".{BASE_DOMAIN}")
        session.cookies.set(k, v, domain=f"www.{BASE_DOMAIN}")

    if proxy:
        session.proxies.update({"http": proxy, "https": proxy})

    return session


def fetch_page(url: str, cookie_str: str, proxy: str = None) -> str:
    """获取页面 HTML，注入登录 Cookie"""
    session = _build_session(cookie_str, proxy)
    resp = session.get(url, timeout=30, allow_redirects=True)
    resp.raise_for_status()
    print(f"[INFO] 页面响应: {resp.status_code} {resp.reason} (URL: {resp.url})")
    print(f"[INFO] 页面内容长度: {len(resp.text)} 字符")
    
    # 保存 HTML 到本地文件用于调试
    html_filename = "_debug_fetched_page.html"
    try:
        with open(html_filename, "w", encoding="utf-8") as f:
            f.write(resp.text)
        print(f"[DEBUG] HTML 已保存到: {html_filename}")
    except Exception as e:
        print(f"[WARN] 保存 HTML 文件失败: {e}")
    
    print(f"[INFO] 页面内容:\n{resp.text}")

    return resp.text


# ---------------------------------------------------------------------------
# 解析：__NEXT_DATA__ JSON
# ---------------------------------------------------------------------------

def _extract_next_data(html: str) -> dict:
    m = re.search(
        r'<script[^>]+id=["\']__NEXT_DATA__["\'][^>]*>(.*?)</script>', html, re.S)
    if m:
        try:
            return json.loads(m.group(1))
        except json.JSONDecodeError:
            pass
    return {}


def _extract_json_ld(html: str) -> list:
    results = []
    for m in re.finditer(
        r'<script[^>]+type=["\']application/ld\+json["\'][^>]*>(.*?)</script>', html, re.S
    ):
        try:
            results.append(json.loads(m.group(1)))
        except json.JSONDecodeError:
            pass
    return results


# ---------------------------------------------------------------------------
# 字段提取
# ---------------------------------------------------------------------------

def _parse_from_next_data(next_data: dict, data: dict):
    """从 __NEXT_DATA__ 提取所有字段，VixenPlus 与 Vixen 结构相同"""
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

        # title
        if not data["title"]:
            data["title"] = video_obj.get(
                "title") or video_obj.get("name") or ""

        # plot
        if not data["plot"]:
            data["plot"] = (
                video_obj.get("description")
                or video_obj.get("plot")
                or video_obj.get("synopsis")
                or ""
            )

        # date
        if not data["date"]:
            raw = (
                video_obj.get("releaseDate")
                or video_obj.get("publishDate")
                or video_obj.get("date")
                or video_obj.get("datePublished")
                or ""
            )
            data["date"] = _parse_date(raw)

        # runtime
        if not data["runtime"]:
            data["runtime"] = _parse_runtime(
                video_obj.get("runLength")
                or video_obj.get("runLengthFormatted")
                or video_obj.get("duration")
            )

        # actors — VixenPlus 用 modelsSlugged 或 models
        if not data["actors"]:
            performers = (
                video_obj.get("modelsSlugged")
                or video_obj.get("models")
                or video_obj.get("performers")
                or video_obj.get("actors")
                or []
            )
            if isinstance(performers, list):
                for p in performers:
                    name = (
                        p.get("name") or p.get(
                            "modelName") or p.get("slug") or ""
                        if isinstance(p, dict) else str(p)
                    )
                    if name and name not in data["actors"]:
                        data["actors"].append(name)

        # director
        if not data["director"]:
            directors = video_obj.get("directors") or []
            if isinstance(directors, list) and directors:
                names = [d.get("name", "") for d in directors if isinstance(
                    d, dict) and d.get("name")]
                if names:
                    data["director"] = ", ".join(names)

        # genres / tags —— 优先提取 tags，其次 categories
        if not data["genres"]:
            tag_sources = (
                video_obj.get("tags")
                or video_obj.get("categories")
                or video_obj.get("genres")
                or []
            )
            if isinstance(tag_sources, list):
                for t in tag_sources:
                    name = (
                        t.get("name") or t.get("slug") or t.get("label") or ""
                        if isinstance(t, dict) else str(t)
                    )
                    if name and name not in data["genres"]:
                        data["genres"].append(name)

        # cover —— 优先 listing > main，宽度最大
        if not data["cover"]:
            images = video_obj.get("images", {})
            if isinstance(images, dict):
                for key in ("listing", "main", "poster", "thumbnail"):
                    img_list = images.get(key) or []
                    best_url, best_w = "", 0
                    for img in img_list:
                        if isinstance(img, dict):
                            w = img.get("width", 0) or 0
                            src = img.get("src") or ""
                            if w > best_w and src:
                                best_w, best_url = w, src
                    if best_url:
                        data["cover"] = best_url
                        break

            if not data["cover"]:
                vi = video_obj.get("videoImage") or {}
                data["cover"] = (vi.get("src") or "") if isinstance(
                    vi, dict) else ""

        # gallery —— carousel 或 pageProps.galleryImages
        if not data["gallery_urls"]:
            gallery_images = page_props.get("galleryImages") or []
            for gi in gallery_images:
                if isinstance(gi, dict):
                    src = gi.get("src") or ""
                    if src:
                        data["gallery_urls"].append(src)

        if not data["gallery_urls"]:
            carousel = video_obj.get("carousel") or []
            for item in carousel:
                if isinstance(item, dict):
                    main_list = item.get("main") or []
                    if main_list and isinstance(main_list[0], dict):
                        src = main_list[0].get("src") or ""
                        if src:
                            data["gallery_urls"].append(src)

    except Exception as e:
        print(f"[WARN] 解析 __NEXT_DATA__ 出错: {e}")


def _parse_from_json_ld(jld: dict, data: dict):
    """从 JSON-LD 数据补充字段"""
    if jld.get("@type") not in ("Movie", "VideoObject", "TVEpisode", "CreativeWork"):
        return

    if not data["title"]:
        data["title"] = jld.get("name") or jld.get("headline") or ""

    if not data["plot"]:
        data["plot"] = jld.get("description") or ""

    if not data["date"]:
        data["date"] = _parse_date(
            jld.get("datePublished") or jld.get(
                "uploadDate") or jld.get("dateCreated") or ""
        )

    if not data["actors"]:
        for role_key in ("actor", "actors", "performer", "performers"):
            persons = jld.get(role_key, [])
            if isinstance(persons, dict):
                persons = [persons]
            for p in persons:
                n = (p.get("name") or "") if isinstance(p, dict) else str(p)
                if n and n not in data["actors"]:
                    data["actors"].append(n)
            if data["actors"]:
                break

    if not data["genres"]:
        for key in ("genre", "keywords"):
            val = jld.get(key, [])
            if isinstance(val, str):
                val = [v.strip() for v in val.split(",") if v.strip()]
            for g in val:
                if isinstance(g, str) and g and g not in data["genres"]:
                    data["genres"].append(g)
            if data["genres"]:
                break

    if not data["cover"]:
        img = jld.get("image") or jld.get("thumbnailUrl") or ""
        if isinstance(img, dict):
            img = img.get("url") or img.get("contentUrl") or ""
        elif isinstance(img, list) and img:
            i0 = img[0]
            img = (i0.get("url") or i0.get("contentUrl")
                   or "") if isinstance(i0, dict) else str(i0)
        data["cover"] = str(img)

    if not data["runtime"]:
        data["runtime"] = _parse_runtime(jld.get("duration") or "")


def _parse_from_html(soup: BeautifulSoup, data: dict):
    """HTML 直接解析（兜底策略）"""
    if not data["title"]:
        og = soup.find("meta", property="og:title")
        data["title"] = og.get("content", "").strip() if og else ""
        if not data["title"]:
            h1 = soup.find("h1")
            if h1:
                data["title"] = h1.get_text(strip=True)

    if not data["plot"]:
        og = soup.find("meta", property="og:description")
        data["plot"] = og.get("content", "").strip() if og else ""
        if not data["plot"]:
            m = soup.find("meta", attrs={"name": "description"})
            if m:
                data["plot"] = m.get("content", "").strip()

    if not data["cover"]:
        og = soup.find("meta", property="og:image")
        if og:
            data["cover"] = og.get("content", "").strip()

    if not data["date"]:
        t = soup.find("time")
        if t:
            data["date"] = _parse_date(
                t.get("datetime") or t.get_text(strip=True))

    # actors —— 从 pornstars/models/performers 链接中收集，遇到 & 停止（& 后是男优）
    if not data["actors"]:
        perf_links = soup.find_all("a", href=re.compile(
            r"/(pornstars|models|performers?)/"))
        if perf_links:
            parent = perf_links[0].parent
            female = []
            for child in parent.children:
                if hasattr(child, "get_text"):
                    text = child.get_text(strip=True)
                    if text == "&":
                        break
                    if child.name == "a" and re.search(
                        r"/(pornstars|models|performers?)/", child.get("href", "")
                    ):
                        if text and text not in female:
                            female.append(text)
            data["actors"] = female if female else [
                a.get_text(strip=True) for a in perf_links
                if a.get_text(strip=True) and a.get_text(strip=True) not in data["actors"]
            ]

    # genres —— 优先从 /tags/ 链接中提取，其次 /categories/
    if not data["genres"]:
        # VixenPlus 标签元素：<a href="/tags/..."> 或 data-* 属性
        tag_links = soup.find_all("a", href=re.compile(r"/tags/"))
        for a in tag_links:
            name = a.get_text(strip=True)
            if name and name not in data["genres"]:
                data["genres"].append(name)

    if not data["genres"]:
        for a in soup.find_all("a", href=re.compile(r"/(categories|genres?)/")):
            name = a.get_text(strip=True)
            if name and name not in data["genres"]:
                data["genres"].append(name)

    # 从 data-category / data-tag 属性兜底提取标签
    if not data["genres"]:
        for el in soup.find_all(attrs={"data-category": True}):
            name = el.get("data-category", "").strip()
            if name and name not in data["genres"]:
                data["genres"].append(name)
        for el in soup.find_all(attrs={"data-tag": True}):
            name = el.get("data-tag", "").strip()
            if name and name not in data["genres"]:
                data["genres"].append(name)


# ---------------------------------------------------------------------------
# 主刮削入口
# ---------------------------------------------------------------------------

def scrape_vixenplus(url: str, cookie_str: str = None, proxy: str = None) -> dict:
    """
    刮削 VixenPlus 视频详情页，返回结构化数据：
    {
        title, plot, date, actors, genres, cover,
        url, studio, runtime, director, gallery_urls
    }
    """
    cookie_str = cookie_str or COOKIE_STRING
    print(f"[INFO] 正在获取页面: {url}")
    html = fetch_page(url, cookie_str=cookie_str, proxy=proxy)

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

    # ⏹️ 断点：HTML 获取完成，检查 html 长度和初始 data
    # breakpoint()  # 取消注释此行以启用该位置的断点

    # 1. __NEXT_DATA__ JSON（最完整）
    print("[INFO] 开始提取 __NEXT_DATA__...")
    next_data = _extract_next_data(html)
    if next_data:
        print("[INFO] 找到 __NEXT_DATA__，开始解析...")
        _parse_from_next_data(next_data, data)

    # 2. JSON-LD 补充
    print("[INFO] 开始提取 JSON-LD...")
    print(html)
    for jld in _extract_json_ld(html):
        _parse_from_json_ld(jld, data)

    # 3. HTML 兜底
    print("[INFO] 开始 HTML 解析...")
    _parse_from_html(soup, data)

    # ⏹️ 断点：所有解析完成，检查最终 data 内容
    # breakpoint()  # 取消注释此行以启用该位置的断点

    # 4. 标签替换：使用 tag_convert.json 映射
    mapping = _get_tag_convert()
    print(f"[TAG] tag_convert.json 共 {len(mapping)} 条映射, 路径: {_TAG_CONVERT_PATH}")
    print(f"[TAG] 替换前 genres ({len(data['genres'])} 个): {data['genres']}")
    if data["genres"]:
        original_genres = data["genres"][:]
        data["genres"] = apply_tag_convert(data["genres"])
        converted = [(o, n) for o, n in zip(original_genres, data["genres"]) if o != n]
        print(f"[TAG] 替换后 genres ({len(data['genres'])} 个): {data['genres']}")
        if converted:
            print(f"[TAG] 标签替换 {len(converted)} 项: " +
                  ", ".join(f"{o}→{n}" for o, n in converted[:5]) +
                  ("..." if len(converted) > 5 else ""))
        else:
            print("[TAG] 无标签命中映射（所有标签保持原值）")
    else:
        print("[TAG] genres 为空，跳过替换")

    return data


# ---------------------------------------------------------------------------
# NFO 构建
# ---------------------------------------------------------------------------

def build_nfo_xml(info: dict, dir_name: str = "") -> str:
    root = ET.Element("movie")

    def sub(tag, text):
        el = ET.SubElement(root, tag)
        el.text = str(text) if text is not None else ""
        return el

    title = info.get("title", "")

    # sorttitle: 从目录名提取 Studio.YY.MM.DD 格式
    m = re.search(r"([A-Za-z]+\.\d{2}\.\d{2}\.\d{2})", dir_name)
    sorttitle = m.group(1) if m else ""

    is_chinese = bool(re.search(r"-C(?:[^A-Za-z]|$)", dir_name))
    display_title = f"[中字] {title}" if is_chinese else title
    effective_sorttitle = (
        f"{sorttitle}-C" if is_chinese else sorttitle) if sorttitle else title

    originaltitle = f"{sorttitle} {title}".strip() if sorttitle else title

    plot_el = ET.SubElement(root, "plot")
    plot_el.text = info.get("plot", "")

    sub("outline", "")
    sub("lockdata", "true")
    sub("dateadded", info.get("date", ""))
    sub("title", display_title)
    sub("originaltitle", originaltitle)

    for actor_name in info.get("actors", []):
        actor_el = ET.SubElement(root, "actor")
        ET.SubElement(actor_el, "name").text = actor_name
        ET.SubElement(actor_el, "type").text = "Actor"

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

    for tag in info.get("genres", []):
        sub("tag", tag)

    sub("studio", info.get("studio", STUDIO))

    uid = ET.SubElement(root, "uniqueid")
    uid.set("type", "VixenPlusScraper-Url")
    uid.text = info.get("url", "")

    uid2 = ET.SubElement(root, "uniqueid")
    uid2.set("type", "VixenPlusScraper-Json")
    uid2.text = json.dumps({
        "OriginalTitle": originaltitle,
        "Cover": info.get("cover", ""),
        "Date": date,
    }, ensure_ascii=False)

    dom = minidom.parseString(ET.tostring(root, encoding="unicode"))
    return dom.toprettyxml(indent="  ", encoding="utf-8").decode("utf-8")


def save_nfo(info: dict, output_dir: str) -> str:
    dir_name = os.path.basename(output_dir.rstrip(os.sep))
    nfo_path = os.path.join(output_dir, f"{dir_name}.nfo")
    os.makedirs(output_dir, exist_ok=True)
    with open(nfo_path, "w", encoding="utf-8") as f:
        f.write(build_nfo_xml(info, dir_name=dir_name))
    print(f"[OK] NFO 已保存: {nfo_path}")
    return nfo_path


# ---------------------------------------------------------------------------
# 图片下载
# ---------------------------------------------------------------------------

def _make_session(cookie_str: str, proxy: str = None) -> requests.Session:
    return _build_session(cookie_str, proxy)


def download_cover(cover_url: str, output_dir: str, dir_name: str,
                   cookie_str: str = None, proxy: str = None) -> bool:
    if not cover_url:
        print("[WARN] 无封面 URL，跳过")
        return False
    try:
        session = _make_session(cookie_str or COOKIE_STRING, proxy)
        resp = session.get(cover_url, timeout=30)
        resp.raise_for_status()
        if len(resp.content) < 1000:
            print(f"[WARN] 封面过小 ({len(resp.content)} bytes)，可能无效")
            return False
        jacket = os.path.join(output_dir, "jacket.jpg")
        with open(jacket, "wb") as f:
            f.write(resp.content)
        print(f"[OK] 封面: {jacket}")
        for fname in ("folder.jpg", "poster.jpg", f"{dir_name}-poster.jpg"):
            shutil.copy2(jacket, os.path.join(output_dir, fname))
        return True
    except Exception as e:
        print(f"[ERROR] 下载封面失败: {e}")
        return False


def download_gallery(gallery_urls: list, output_dir: str,
                     cookie_str: str = None, proxy: str = None):
    if not gallery_urls:
        print("[WARN] 无剧照 URL，跳过")
        return
    session = _make_session(cookie_str or COOKIE_STRING, proxy)
    ok = 0
    for idx, url in enumerate(gallery_urls, 1):
        path = os.path.join(output_dir, f"backdrop{idx}.jpg")
        try:
            resp = session.get(url, timeout=30)
            resp.raise_for_status()
            if len(resp.content) < 1000:
                print(f"[WARN] 剧照 {idx} 过小，跳过")
                continue
            with open(path, "wb") as f:
                f.write(resp.content)
            print(f"[OK] 剧照 {idx}/{len(gallery_urls)}: {path}")
            ok += 1
        except Exception as e:
            print(f"[ERROR] 剧照 {idx} 失败: {e}")
    print(f"[INFO] 剧照完成: {ok}/{len(gallery_urls)} 张")


# ---------------------------------------------------------------------------
# CLI
# ---------------------------------------------------------------------------

def main():
    parser = argparse.ArgumentParser(
        description="VixenPlus.com 视频详情页刮削器（需要登录 Cookie）"
    )
    parser.add_argument("url", help="VixenPlus 视频详情页 URL")
    parser.add_argument("output_dir", default="\\192.168.1.199\Jav\Western\Hotel Vixen", help="输出目录")
    parser.add_argument("--cookie-string", default=None,
                        help="覆盖内置 Cookie 字符串（格式同浏览器复制的 Cookie）")
    parser.add_argument("--proxy", default=None,
                        help="代理地址，例如 http://127.0.0.1:7890")
    parser.add_argument("--no-cover", action="store_true", help="不下载封面")
    parser.add_argument("--no-gallery", action="store_true", help="不下载剧照")

    args = parser.parse_args()
    cookie_str = args.cookie_string or COOKIE_STRING

    try:
        info = scrape_vixenplus(
            args.url, cookie_str=cookie_str, proxy=args.proxy)
    except requests.exceptions.HTTPError as e:
        print(f"[ERROR] HTTP 错误: {e}")
        sys.exit(1)
    except Exception as e:
        print(f"[ERROR] 刮削失败: {e}")
        sys.exit(1)

    print("\n======= 刮削结果 =======")
    print(f"  标题  : {info['title']}")
    print(f"  日期  : {info['date']}")
    print(f"  时长  : {info['runtime']} 分钟")
    print(f"  导演  : {info.get('director', '')}")
    print(f"  演员  : {', '.join(info['actors'])}")
    print(f"  标签  : {', '.join(info['genres'])}")
    print(f"  剧照  : {len(info.get('gallery_urls', []))} 张")
    print(f"  封面  : {info['cover']}")
    print(
        f"  简介  : {info['plot'][:100]}{'...' if len(info['plot']) > 100 else ''}")
    print("========================\n")

    # 未匹配检测：标题、剧情、演员全部为空时记录日志
    _nfo_chk = os.path.join(args.output_dir, os.path.basename(args.output_dir.rstrip(os.sep)) + ".nfo")
    if not info["title"] and not info["plot"] and not info["actors"]:
        log_no_match(_nfo_chk, reason=f"url={args.url} 刮削失败：关键字段全空")

    nfo_path = save_nfo(info, args.output_dir)
    dir_name = os.path.basename(args.output_dir.rstrip(os.sep))

    if not args.no_cover:
        download_cover(info["cover"], args.output_dir, dir_name,
                       cookie_str=cookie_str, proxy=args.proxy)

    if not args.no_gallery:
        download_gallery(info.get("gallery_urls", []), args.output_dir,
                         cookie_str=cookie_str, proxy=args.proxy)

    print(f"\n[DONE] 完成！NFO: {nfo_path}")


# ---------------------------------------------------------------------------
# 调试入口
# ---------------------------------------------------------------------------

def debug_mode():
    """
    调试模式：直接在 VSCode 中运行此脚本进行调试
    可在此设置测试参数（URL、Cookie、代理等）
    
    === 如何调试 ===
    1. 在关键行设置断点（Ctrl+K Ctrl+B 或点击行号左侧）
    2. 按 F5 启动调试或运行: python vixenplus_scraper.py
    3. 使用 F10(单步) F11(进入) Shift+F11(跳出)
    """
    import sys
    
    # ★★★ 在此设置调试参数 ★★★
    test_url = "https://members.vixenplus.com/videos/hospitality"
    test_output_dir = "./_debug_output"
    test_proxy = None  # 例如 "http://127.0.0.1:7890"
    
    # 调试 Cookie（可选，默认使用脚本内的 COOKIE_STRING）
    test_cookie = None
    
    # ================================
    
    print("\n" + "=" * 70)
    print("  VixenPlus Scraper - 调试模式")
    print("=" * 70 + "\n")
    
    try:
        # 1. 准备调试信息
        print(f"[DEBUG] 测试 URL: {test_url}")
        print(f"[DEBUG] 输出目录: {test_output_dir}")
        print(f"[DEBUG] 代理: {test_proxy or '无'}")
        print(f"[DEBUG] 按 F10 单步执行下一行...")
        
        # ⏹️ 调试断点 #1: 在此暂停，检查参数
        breakpoint()  # 在调试器中可查看 test_url, test_cookie 等变量
        
        # 2. 刮削页面
        print(f"\n[DEBUG] 开始解析页面...\n  URL: {test_url}\n")
        info = scrape_vixenplus(
            url=test_url,
            cookie_str=test_cookie or COOKIE_STRING,
            proxy=test_proxy
        )
        
        # ⏹️ 调试断点 #2: 刮削完成，检查 info 结果
        breakpoint()  # 在调试器中可查看 info 字典内容
        
        # 2. 打印结果
        print("\n" + "=" * 70)
        print("  🎬 刮削结果")
        print("=" * 70)
        print(f"\n  标题    : {info.get('title', '(未获取)')}")
        print(f"  日期    : {info.get('date', '(未获取)')}")
        print(f"  时长    : {info.get('runtime', '(未获取)')} 分钟")
        print(f"  导演    : {info.get('director', '(未获取)')}")
        print(f"  演员数  : {len(info.get('actors', []))} 人")
        if info.get('actors'):
            print(f"      → {', '.join(info['actors'][:5])}{'...' if len(info['actors']) > 5 else ''}")
        print(f"  标签数  : {len(info.get('genres', []))} 个")
        if info.get('genres'):
            print(f"      → {', '.join(info['genres'][:5])}{'...' if len(info['genres']) > 5 else ''}")
        print(f"  剧照    : {len(info.get('gallery_urls', []))} 张")
        print(f"  封面    : {'✓ 已获取' if info.get('cover') else '✗ 未获取'}")
        print(f"  简介    : {len(info.get('plot', ''))} 字符")
        
        # 3. 保存到文件（可选）
        print(f"\n  [DEBUG] 调试信息保存位置:")
        dir_name = "debug_output"
        nfo_path = save_nfo(info, test_output_dir)
        print(f"      NFO: {nfo_path}")
        
        # 4. 保存原始 JSON 用于调试
        debug_json = os.path.join(test_output_dir, f"{dir_name}_raw_data.json")
        os.makedirs(test_output_dir, exist_ok=True)
        with open(debug_json, "w", encoding="utf-8") as f:
            json.dump(info, f, ensure_ascii=False, indent=2)
        print(f"      原始数据: {debug_json}")
        
        print(f"\n✓ 调试完成！\n")
        return True
        
    except KeyboardInterrupt:
        print("\n\n⚠ 用户中断")
        return False
    except Exception as e:
        print(f"\n✗ 调试失败: {e}\n")
        import traceback
        traceback.print_exc()
        return False


if __name__ == "__main__":
    import sys
    
    main()
    # 检查命令行参数 --debug，如果有则进入调试模式
    # if "--debug" in sys.argv or len(sys.argv) == 1:
    #     # 无参数或显式 --debug 时进入调试模式
    #     if len(sys.argv) == 1:
    #         sys.exit(0 if debug_mode() else 1)
    #     else:
    #         debug_mode()
    # else:
        # 否则正常 CLI 模式
    # main()
