import cloudscraper, json, re

scraper = cloudscraper.create_scraper(browser={"browser": "chrome", "platform": "windows", "mobile": False})
scraper.cookies.set("consent", "1", domain=".vixen.com")
resp = scraper.get("https://www.vixen.com/videos/mutual-generosity", timeout=30)
html = resp.text

m = re.search(r'<script[^>]+id="__NEXT_DATA__"[^>]*>(.*?)</script>', html, re.S)
if m:
    d = json.loads(m.group(1))
    pp = d.get("props", {}).get("pageProps", {})
    video = pp.get("video", {})
    # 打印 carousel 字段
    carousel = video.get("carousel", [])
    print("carousel 长度:", len(carousel))
    if carousel:
        print("\n第一个 carousel 元素:")
        print(json.dumps(carousel[0], ensure_ascii=False, indent=2))
    # 打印 galleryImages 字段
    gallery = pp.get("galleryImages", [])
    print("\ngalleryImages 长度:", len(gallery))
    if gallery:
        print("\n第一个 galleryImages 元素:")
        print(json.dumps(gallery[0], ensure_ascii=False, indent=2))
