#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
VixenPlus 刮削器本地调试脚本
直接运行即可，不需要任何参数：
    python debug_vixenplus.py
"""

import os
import sys
import json
import re

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from vixenplus_scraper import (
    fetch_page,
    _extract_next_data,
    _extract_json_ld,
    _parse_from_next_data,
    _parse_from_json_ld,
    _parse_from_html,
    build_nfo_xml,
    COOKIE_STRING,
)
from bs4 import BeautifulSoup

# ===========================================================================
# ★ 在此修改调试参数 ★
# ===========================================================================

TEST_URL   = "https://www.vixenplus.com/videos/after-hours"
PROXY      = None          # 例如 "http://127.0.0.1:7890"
SAVE_HTML  = True          # 是否把原始 HTML 保存到本地（方便离线反复调试）
SAVE_JSON  = True          # 是否把 __NEXT_DATA__ 保存为 JSON
OUTPUT_DIR = os.path.dirname(os.path.abspath(__file__))

# ===========================================================================


def sep(title: str = ""):
    line = "=" * 60
    print(f"\n{line}")
    if title:
        print(f"  {title}")
        print(line)


def dump(label: str, value):
    """统一打印调试字段"""
    if isinstance(value, list):
        print(f"  [{label}]  ({len(value)} 项)")
        for i, v in enumerate(value):
            print(f"      [{i}] {v}")
    elif isinstance(value, dict):
        print(f"  [{label}]  (dict, {len(value)} keys)")
        for k, v in list(value.items())[:10]:
            print(f"      {k}: {str(v)[:120]}")
    else:
        text = str(value) if value else "(空)"
        print(f"  [{label}]  {text[:200]}")


def main():
    html_cache = os.path.join(OUTPUT_DIR, "_debug_vixenplus.html")
    json_cache = os.path.join(OUTPUT_DIR, "_debug_vixenplus_next.json")

    # ------------------------------------------------------------------
    # 1. 获取 HTML（优先用缓存，避免反复请求）
    # ------------------------------------------------------------------
    sep("步骤 1：获取页面 HTML")

    if os.path.exists(html_cache):
        print(f"  [CACHE] 读取本地缓存: {html_cache}")
        with open(html_cache, encoding="utf-8") as f:
            html = f.read()
    else:
        print(f"  [NET] 正在请求: {TEST_URL}")
        try:
            html = fetch_page(TEST_URL, cookie_str=COOKIE_STRING, proxy=PROXY)
            print(f"  [OK] 获取成功，HTML 长度: {len(html)} 字节")
            if SAVE_HTML:
                with open(html_cache, "w", encoding="utf-8") as f:
                    f.write(html)
                print(f"  [SAVED] HTML 已保存: {html_cache}")
        except Exception as e:
            print(f"  [ERROR] 请求失败: {e}")
            sys.exit(1)

    # ------------------------------------------------------------------
    # 2. 检查是否被重定向到登录页 / 年龄验证页
    # ------------------------------------------------------------------
    sep("步骤 2：检查页面状态")

    if "join" in html[:3000].lower() or "sign in" in html[:3000].lower():
        print("  [WARN] 页面可能是登录页，Cookie 可能已过期！")
    elif len(html) < 5000:
        print(f"  [WARN] HTML 过短 ({len(html)} 字节)，可能返回了错误页")
    else:
        print(f"  [OK] 页面正常，长度 {len(html)} 字节")

    # ------------------------------------------------------------------
    # 3. 提取 __NEXT_DATA__
    # ------------------------------------------------------------------
    sep("步骤 3：提取 __NEXT_DATA__")

    next_data = _extract_next_data(html)
    if next_data:
        print(f"  [OK] 找到 __NEXT_DATA__，顶层 keys: {list(next_data.keys())}")
        page_props = next_data.get("props", {}).get("pageProps", {})
        print(f"  pageProps keys: {list(page_props.keys())}")

        video_obj = (
            page_props.get("video")
            or page_props.get("videoData")
            or page_props.get("data", {}).get("video")
        )
        if video_obj:
            print(f"  video_obj keys: {list(video_obj.keys())}")
            dump("title",       video_obj.get("title"))
            dump("description", video_obj.get("description") or video_obj.get("synopsis"))
            dump("releaseDate", video_obj.get("releaseDate") or video_obj.get("publishDate"))
            dump("runLength",   video_obj.get("runLength") or video_obj.get("duration"))
            dump("models",      video_obj.get("modelsSlugged") or video_obj.get("models") or [])
            dump("directors",   video_obj.get("directors") or [])
            dump("tags",        video_obj.get("tags") or video_obj.get("categories") or [])
            dump("images keys", list((video_obj.get("images") or {}).keys()))
        else:
            print("  [WARN] 未找到 video_obj，pageProps 内容如下：")
            for k, v in page_props.items():
                print(f"      {k}: {str(v)[:100]}")

        if SAVE_JSON:
            with open(json_cache, "w", encoding="utf-8") as f:
                json.dump(next_data, f, ensure_ascii=False, indent=2)
            print(f"  [SAVED] __NEXT_DATA__ 已保存: {json_cache}")
    else:
        print("  [WARN] 未找到 __NEXT_DATA__，将依赖 JSON-LD 和 HTML 解析")

    # ------------------------------------------------------------------
    # 4. 提取 JSON-LD
    # ------------------------------------------------------------------
    sep("步骤 4：提取 JSON-LD")

    json_ld_list = _extract_json_ld(html)
    print(f"  找到 {len(json_ld_list)} 个 JSON-LD 块")
    for i, jld in enumerate(json_ld_list):
        print(f"  --- JSON-LD [{i}] @type={jld.get('@type')} ---")
        for k in ("name", "description", "datePublished", "duration", "genre"):
            if k in jld:
                dump(k, jld[k])

    # ------------------------------------------------------------------
    # 5. 运行三级解析，打印最终结果
    # ------------------------------------------------------------------
    sep("步骤 5：运行完整解析")

    data = {
        "title": "", "plot": "", "date": "", "actors": [],
        "genres": [], "cover": "", "url": TEST_URL,
        "studio": "VixenPlus", "runtime": "", "director": "", "gallery_urls": [],
    }

    print("  >> 解析 __NEXT_DATA__ ...")
    _parse_from_next_data(next_data, data)
    print(f"     title={data['title']!r}  date={data['date']!r}  actors={len(data['actors'])}  genres={len(data['genres'])}")

    print("  >> 解析 JSON-LD ...")
    for jld in json_ld_list:
        _parse_from_json_ld(jld, data)
    print(f"     title={data['title']!r}  date={data['date']!r}  actors={len(data['actors'])}  genres={len(data['genres'])}")

    print("  >> 解析 HTML ...")
    soup = BeautifulSoup(html, "html.parser")
    _parse_from_html(soup, data)
    print(f"     title={data['title']!r}  date={data['date']!r}  actors={len(data['actors'])}  genres={len(data['genres'])}")

    # ------------------------------------------------------------------
    # 6. 最终字段汇总
    # ------------------------------------------------------------------
    sep("步骤 6：最终字段汇总")

    dump("title",        data["title"])
    dump("plot",         data["plot"][:200] if data["plot"] else "")
    dump("date",         data["date"])
    dump("runtime",      data["runtime"])
    dump("director",     data["director"])
    dump("actors",       data["actors"])
    dump("genres(tags)", data["genres"])
    dump("cover",        data["cover"])
    dump("gallery_urls", data["gallery_urls"])

    # ------------------------------------------------------------------
    # 7. 生成 NFO 预览
    # ------------------------------------------------------------------
    sep("步骤 7：NFO XML 预览")

    dir_name = "VixenPlus.25.04.24.After.Hours"
    xml = build_nfo_xml(data, dir_name=dir_name)
    # 只打印前 60 行
    lines = xml.splitlines()
    for line in lines[:60]:
        print(line)
    if len(lines) > 60:
        print(f"  ... (共 {len(lines)} 行，已截断显示)")

    # ------------------------------------------------------------------
    # 8. 缺失字段警告
    # ------------------------------------------------------------------
    sep("步骤 8：缺失字段检查")

    missing = [f for f in ("title", "plot", "date", "cover") if not data[f]]
    missing += ["actors"] if not data["actors"] else []
    missing += ["genres"] if not data["genres"] else []

    if missing:
        print(f"  [WARN] 以下字段未获取到: {', '.join(missing)}")
        print()
        print("  排查建议：")
        if "title" in missing or "plot" in missing:
            print("    - 检查 _debug_vixenplus_next.json 中 video_obj 的结构")
        if "genres" in missing:
            print("    - VixenPlus 标签可能在 video_obj.tags / categories 下，")
            print("      或 HTML 中 <a href='/tags/...'> 元素")
            print("      请打开 _debug_vixenplus.html 用浏览器检查标签元素的实际 HTML")
        if "actors" in missing:
            print("    - 演员可能在 modelsSlugged / models / performers 字段下")
        if not data["cover"]:
            print("    - 封面图在 video_obj.images.listing[最大宽度].src")
    else:
        print("  [PASS] 所有关键字段均已提取 ✓")

    print("\n" + "=" * 60)
    print("  调试完成！")
    if SAVE_JSON:
        print(f"  完整 __NEXT_DATA__: {json_cache}")
    if SAVE_HTML:
        print(f"  原始 HTML:          {html_cache}")
    print("=" * 60 + "\n")

    # 保存最终 data 为 JSON
    result_json = os.path.join(OUTPUT_DIR, "_debug_vixenplus_result.json")
    with open(result_json, "w", encoding="utf-8") as f:
        json.dump(data, f, ensure_ascii=False, indent=2)
    print(f"  最终结果 JSON:      {result_json}\n")


if __name__ == "__main__":
    # 如果第一个参数是 --clear，删除 HTML 缓存强制重新请求
    if len(sys.argv) > 1 and sys.argv[1] == "--clear":
        cache = os.path.join(OUTPUT_DIR, "_debug_vixenplus.html")
        if os.path.exists(cache):
            os.remove(cache)
            print(f"[INFO] 已删除 HTML 缓存: {cache}")
    main()
