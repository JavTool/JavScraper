#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
VixenPlus 刮削器调试测试脚本

用法：
    python test_vixenplus_scraper.py
    python test_vixenplus_scraper.py --url "https://www.vixenplus.com/videos/xxx"
    python test_vixenplus_scraper.py --proxy http://127.0.0.1:7890
"""

import os
import sys
import json
import argparse
import unittest
from unittest.mock import patch, MagicMock

# 将当前目录加入路径
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from vixenplus_scraper import (
    _parse_cookie_string,
    _parse_date,
    _parse_runtime,
    _deep_find,
    _extract_next_data,
    _extract_json_ld,
    _parse_from_next_data,
    _parse_from_json_ld,
    _parse_from_html,
    build_nfo_xml,
    scrape_vixenplus,
    COOKIE_STRING,
)
from bs4 import BeautifulSoup


# ---------------------------------------------------------------------------
# 单元测试（不需要网络）
# ---------------------------------------------------------------------------

class TestParseCookieString(unittest.TestCase):

    def test_basic(self):
        s = "foo=bar; baz=qux"
        result = _parse_cookie_string(s)
        self.assertEqual(result["foo"], "bar")
        self.assertEqual(result["baz"], "qux")

    def test_url_encoded_value(self):
        s = "consent=0; sid=s%3Aabc.def"
        result = _parse_cookie_string(s)
        self.assertEqual(result["consent"], "0")
        self.assertIn("sid", result)

    def test_empty(self):
        self.assertEqual(_parse_cookie_string(""), {})


class TestParseDate(unittest.TestCase):

    def test_iso(self):
        self.assertEqual(_parse_date("2025-04-24T00:00:00Z"), "2025-04-24")

    def test_full_month(self):
        self.assertEqual(_parse_date("April 24, 2025"), "2025-04-24")

    def test_short_month(self):
        self.assertEqual(_parse_date("Apr 24, 2025"), "2025-04-24")

    def test_empty(self):
        self.assertEqual(_parse_date(""), "")

    def test_invalid(self):
        result = _parse_date("not-a-date")
        self.assertEqual(result, "not-a-date")


class TestParseRuntime(unittest.TestCase):

    def test_hhmmss(self):
        self.assertEqual(_parse_runtime("01:30:00"), "90")

    def test_hhmmss_round_up(self):
        self.assertEqual(_parse_runtime("01:30:45"), "91")

    def test_iso_duration(self):
        self.assertEqual(_parse_runtime("PT1H30M"), "90")
        self.assertEqual(_parse_runtime("PT45M"), "45")

    def test_seconds_int(self):
        self.assertEqual(_parse_runtime("3600"), "60")

    def test_none(self):
        self.assertEqual(_parse_runtime(None), "")


class TestDeepFind(unittest.TestCase):

    def test_simple(self):
        obj = {"a": {"b": {"c": 42}}}
        self.assertEqual(_deep_find(obj, "c"), 42)

    def test_in_list(self):
        obj = {"items": [{"video": {"title": "Hello"}}]}
        self.assertEqual(_deep_find(obj, "video"), {"title": "Hello"})

    def test_not_found(self):
        self.assertIsNone(_deep_find({"x": 1}, "y"))


class TestExtractNextData(unittest.TestCase):

    def test_found(self):
        html = '<script id="__NEXT_DATA__" type="application/json">{"props":{"pageProps":{"video":{"title":"Test"}}}}</script>'
        data = _extract_next_data(html)
        self.assertEqual(data["props"]["pageProps"]["video"]["title"], "Test")

    def test_not_found(self):
        self.assertEqual(_extract_next_data("<html></html>"), {})

    def test_invalid_json(self):
        html = '<script id="__NEXT_DATA__">{bad json}</script>'
        self.assertEqual(_extract_next_data(html), {})


class TestParseFromNextData(unittest.TestCase):

    def _make_data(self):
        return {
            "title": "", "plot": "", "date": "", "actors": [],
            "genres": [], "cover": "", "url": "", "studio": "VixenPlus",
            "runtime": "", "director": "", "gallery_urls": [],
        }

    def test_basic_fields(self):
        next_data = {
            "props": {
                "pageProps": {
                    "video": {
                        "title": "Test Video",
                        "description": "Some plot here.",
                        "releaseDate": "2025-04-24",
                        "runLength": "01:28:00",
                        "modelsSlugged": [{"name": "Actress One"}, {"name": "Actress Two"}],
                        "directors": [{"name": "Director Name"}],
                        "tags": [{"name": "Blonde"}, {"name": "Outdoor"}],
                        "images": {
                            "listing": [
                                {"src": "https://cdn.vixenplus.com/cover.jpg", "width": 1920}
                            ]
                        },
                    }
                }
            }
        }
        data = self._make_data()
        _parse_from_next_data(next_data, data)

        self.assertEqual(data["title"], "Test Video")
        self.assertEqual(data["plot"], "Some plot here.")
        self.assertEqual(data["date"], "2025-04-24")
        self.assertEqual(data["runtime"], "88")
        self.assertIn("Actress One", data["actors"])
        self.assertIn("Actress Two", data["actors"])
        self.assertEqual(data["director"], "Director Name")
        self.assertIn("Blonde", data["genres"])
        self.assertIn("Outdoor", data["genres"])
        self.assertIn("cover.jpg", data["cover"])

    def test_empty_video(self):
        data = self._make_data()
        _parse_from_next_data({"props": {"pageProps": {}}}, data)
        self.assertEqual(data["title"], "")


class TestParseFromJsonLd(unittest.TestCase):

    def _make_data(self):
        return {
            "title": "", "plot": "", "date": "", "actors": [],
            "genres": [], "cover": "", "url": "", "studio": "VixenPlus",
            "runtime": "", "director": "", "gallery_urls": [],
        }

    def test_video_object(self):
        jld = {
            "@type": "VideoObject",
            "name": "JSON-LD Title",
            "description": "JSON-LD plot",
            "datePublished": "2025-01-15",
            "duration": "PT1H20M",
            "actor": [{"name": "Actor A"}],
            "genre": ["Genre X", "Genre Y"],
            "image": "https://cdn.example.com/thumb.jpg",
        }
        data = self._make_data()
        _parse_from_json_ld(jld, data)
        self.assertEqual(data["title"], "JSON-LD Title")
        self.assertEqual(data["date"], "2025-01-15")
        self.assertEqual(data["runtime"], "80")
        self.assertIn("Actor A", data["actors"])
        self.assertIn("Genre X", data["genres"])
        self.assertEqual(data["cover"], "https://cdn.example.com/thumb.jpg")

    def test_wrong_type_skipped(self):
        data = self._make_data()
        _parse_from_json_ld({"@type": "Person", "name": "X"}, data)
        self.assertEqual(data["title"], "")


class TestParseFromHtml(unittest.TestCase):

    def _make_data(self):
        return {
            "title": "", "plot": "", "date": "", "actors": [],
            "genres": [], "cover": "", "url": "", "studio": "VixenPlus",
            "runtime": "", "director": "", "gallery_urls": [],
        }

    def test_og_tags(self):
        html = """
        <html>
        <head>
            <meta property="og:title" content="OG Title" />
            <meta property="og:description" content="OG Description" />
            <meta property="og:image" content="https://img.example.com/cover.jpg" />
        </head>
        <body>
            <time datetime="2025-03-10">March 10, 2025</time>
            <a href="/tags/blonde">Blonde</a>
            <a href="/tags/outdoor">Outdoor</a>
            <a href="/models/actress-name">Actress Name</a>
        </body>
        </html>
        """
        soup = BeautifulSoup(html, "html.parser")
        data = self._make_data()
        _parse_from_html(soup, data)
        self.assertEqual(data["title"], "OG Title")
        self.assertEqual(data["plot"], "OG Description")
        self.assertEqual(data["cover"], "https://img.example.com/cover.jpg")
        self.assertEqual(data["date"], "2025-03-10")
        self.assertIn("Blonde", data["genres"])
        self.assertIn("Outdoor", data["genres"])

    def test_tags_link(self):
        html = """
        <html><body>
            <a href="/tags/big-boobs">Big Boobs</a>
            <a href="/tags/brunette">Brunette</a>
        </body></html>
        """
        soup = BeautifulSoup(html, "html.parser")
        data = self._make_data()
        _parse_from_html(soup, data)
        self.assertIn("Big Boobs", data["genres"])
        self.assertIn("Brunette", data["genres"])


class TestBuildNfoXml(unittest.TestCase):

    def test_basic_nfo(self):
        info = {
            "title": "Test Video",
            "plot": "A great scene.",
            "date": "2025-04-24",
            "actors": ["Actress One", "Actress Two"],
            "genres": ["Blonde", "Outdoor"],
            "cover": "https://cdn.example.com/cover.jpg",
            "url": "https://www.vixenplus.com/videos/test-video",
            "studio": "VixenPlus",
            "runtime": "90",
            "director": "John Doe",
            "gallery_urls": [],
        }
        xml = build_nfo_xml(info, dir_name="VixenPlus.25.04.24.Test.Video")
        self.assertIn("<title>Test Video</title>", xml)
        self.assertIn("<premiered>2025-04-24</premiered>", xml)
        self.assertIn("<studio>VixenPlus</studio>", xml)
        self.assertIn("Actress One", xml)
        self.assertIn("Blonde", xml)
        self.assertIn("<sorttitle>VixenPlus.25.04.24</sorttitle>", xml)
        self.assertIn("<runtime>90</runtime>", xml)
        self.assertIn("<director>John Doe</director>", xml)

    def test_chinese_sub_flag(self):
        info = {
            "title": "Test Video", "plot": "", "date": "2025-04-24",
            "actors": [], "genres": [], "cover": "", "url": "",
            "studio": "VixenPlus", "runtime": "", "director": "", "gallery_urls": [],
        }
        xml = build_nfo_xml(info, dir_name="VixenPlus.25.04.24.Test.Video-C")
        self.assertIn("[中字]", xml)
        self.assertIn("VixenPlus.25.04.24-C", xml)


# ---------------------------------------------------------------------------
# 集成测试（需要真实网络 + Cookie）
# ---------------------------------------------------------------------------

class TestLiveVixenPlus:
    """
    真实网络集成测试，不继承 unittest.TestCase，
    通过 run_live_test() 手动调用。
    """

    # ===== 在此修改测试 URL =====
    TEST_URL = "https://www.vixenplus.com/videos/after-hours"
    # ============================

    def run(self, proxy: str = None, cookie_str: str = None):
        print(f"\n{'='*60}")
        print(f"[LIVE] 开始集成测试")
        print(f"[LIVE] URL: {self.TEST_URL}")
        print(f"{'='*60}\n")

        info = scrape_vixenplus(
            self.TEST_URL,
            cookie_str=cookie_str or COOKIE_STRING,
            proxy=proxy,
        )

        # 打印完整结果
        print("\n======= 刮削结果 =======")
        print(f"  标题  : {info['title']}")
        print(f"  日期  : {info['date']}")
        print(f"  时长  : {info['runtime']} 分钟")
        print(f"  导演  : {info.get('director', '')}")
        print(f"  演员  : {', '.join(info['actors'])}")
        print(f"  标签  : {', '.join(info['genres'])}")
        print(f"  剧照  : {len(info.get('gallery_urls', []))} 张")
        print(f"  封面  : {info['cover']}")
        print(f"  简介  : {info['plot'][:120]}{'...' if len(info['plot']) > 120 else ''}")
        print("========================\n")

        # 保存原始 JSON 供调试
        out_json = os.path.join(os.path.dirname(__file__), "vixenplus_debug.json")
        with open(out_json, "w", encoding="utf-8") as f:
            json.dump(info, f, ensure_ascii=False, indent=2)
        print(f"[OK] 原始数据已保存: {out_json}")

        # 简单断言
        errors = []
        if not info["title"]:
            errors.append("title 为空")
        if not info["plot"]:
            errors.append("plot 为空")
        if not info["date"]:
            errors.append("date 为空")
        if not info["actors"]:
            errors.append("actors 为空")
        if not info["genres"]:
            errors.append("genres 为空（标签）")
        if not info["cover"]:
            errors.append("cover 为空")

        if errors:
            print(f"\n[FAIL] 以下字段缺失: {', '.join(errors)}")
        else:
            print("\n[PASS] 所有关键字段均已提取 ✓")

        return info


# ---------------------------------------------------------------------------
# 入口
# ---------------------------------------------------------------------------

def main():
    parser = argparse.ArgumentParser(description="VixenPlus 刮削器测试")
    parser.add_argument("--unit", action="store_true", default=False,
                        help="只运行单元测试（不需要网络）")
    parser.add_argument("--live", action="store_true", default=False,
                        help="运行真实网络集成测试")
    parser.add_argument("--url", default=None,
                        help="集成测试使用的 URL（覆盖默认值）")
    parser.add_argument("--proxy", default=None,
                        help="代理地址，例如 http://127.0.0.1:7890")
    parser.add_argument("--cookie-string", default=None,
                        help="覆盖内置 Cookie")
    args = parser.parse_args()

    run_unit = args.unit or (not args.live)  # 默认运行单元测试

    if run_unit:
        print("=" * 60)
        print("运行单元测试...")
        print("=" * 60)
        loader = unittest.TestLoader()
        suite = unittest.TestSuite()
        for cls in [
            TestParseCookieString,
            TestParseDate,
            TestParseRuntime,
            TestDeepFind,
            TestExtractNextData,
            TestParseFromNextData,
            TestParseFromJsonLd,
            TestParseFromHtml,
            TestBuildNfoXml,
        ]:
            suite.addTests(loader.loadTestsFromTestCase(cls))
        runner = unittest.TextTestRunner(verbosity=2)
        result = runner.run(suite)
        if not result.wasSuccessful():
            sys.exit(1)

    if args.live:
        live = TestLiveVixenPlus()
        if args.url:
            live.TEST_URL = args.url
        live.run(proxy=args.proxy, cookie_str=args.cookie_string)


if __name__ == "__main__":
    main()
