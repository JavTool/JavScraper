# 修复元数据与文件规范化处理
from math import fabs
import os, re, argparse, shutil, zipfile
import xml.etree.ElementTree as ET

# 验证 sorttitle 是否符合如 BAA-123 / ABC-123-U / ABCD-123-UC / 91CM-076 格式
SORTTITLE_RE = re.compile(r"^[A-Za-z0-9_]{2,6}-\d{2,5}(-[A-Za-z_]+)?$")

SUB_EXTS = {".srt", ".ass", ".ssa", ".vtt"}
IMG_EXTS = {".jpg", ".jpeg", ".png", ".webp"}


def is_leaf_dir(_root, dirs):
    return len(dirs) == 0


def validate_sorttitle(nfo_path: str) -> bool:
    try:
        tree = ET.parse(nfo_path)
        root = tree.getroot()
        elem = root.find("sorttitle")
        val = (elem.text or "").strip() if elem is not None else ""
        return bool(SORTTITLE_RE.match(val))
    except Exception:
        # 无法解析的 nfo 也视为不符合
        return False


def ensure_subtitle_names(dir_path: str):
    dirname = os.path.basename(dir_path.rstrip(os.sep))
    for fn in os.listdir(dir_path):
        src = os.path.join(dir_path, fn)
        if not os.path.isfile(src):
            continue
        name, ext = os.path.splitext(fn)
        if ext.lower() in SUB_EXTS and ".Chi" not in name:
            dst = os.path.join(dir_path, f"{dirname}.Chi{ext.lower()}")
            # 若已存在，追加序号避免覆盖
            i, final_dst = 2, dst
            while os.path.exists(final_dst):
                final_dst = os.path.join(dir_path, f"{dirname}.Chi_{i}{ext.lower()}")
                i += 1
            print(f"重命名字幕: {src} -> {final_dst}")
            os.rename(src, final_dst)


def get_earliest_zip(dir_path: str):
    zips = []
    for fn in os.listdir(dir_path):
        if fn.lower().endswith(".zip"):
            fp = os.path.join(dir_path, fn)
            try:
                ctime = os.path.getctime(fp)
            except Exception:
                ctime = os.path.getmtime(fp)
            zips.append((ctime, fp))
    if not zips:
        return None
    zips.sort(key=lambda x: x[0])
    print(f"找到 {len(zips)} 个 zip 文件，最早的是: {zips[0][1]}")
    return zips[0][1]


def extract_and_copy_poster(dir_path: str):
    zip_path = get_earliest_zip(dir_path)
    if not zip_path:
        return
    print(f"解压最早 zip: {zip_path}")
    try:
        with zipfile.ZipFile(zip_path, 'r') as zf:
            members = zf.namelist()
            poster_candidates = [m for m in members if "-poster".lower() in m.lower() and os.path.splitext(m)[1].lower() in IMG_EXTS]
            # 解压全部（简单起见），也可只解压候选
            zf.extractall(dir_path)
        if poster_candidates:
            src_rel = poster_candidates[0].rstrip("/\\")
            src_abs = os.path.join(dir_path, *src_rel.split("/"))
            if os.path.isfile(src_abs):
                for target in ("poster.jpg", "folder.jpg"):
                    dst = os.path.join(dir_path, target)
                    print(f"复制海报: {src_abs} -> {dst}")
                    shutil.copyfile(src_abs, dst)
    except Exception as e:
        print(f"解压/复制海报时出错: {e}")


# ---------------- 新增：修复 sorttitle 的工具函数 ----------------
FANHAO_HYPHEN_RE = re.compile(r"\b([A-Za-z0-9_]{2,6}-\d{2,5})\b")
FANHAO_COMPACT_RE = re.compile(r"\b([A-Za-z0-9_]{2,6})(\d{2,5})\b")


def _extract_fanhao(text: str) -> str | None:
    if not text:
        return None
    m = FANHAO_HYPHEN_RE.search(text)
    if m:
        return m.group(1).upper()
    m = FANHAO_COMPACT_RE.search(text)
    if m:
        return f"{m.group(1).upper()}-{m.group(2)}"
    return None


def _determine_suffix(originaltitle: str, title: str, dir_name: str) -> str:
    def has_any(s: str, keywords: list[str]) -> bool:
        s = s or ""
        return any(k in s for k in keywords)

    ot = originaltitle or ""
    tt = title or ""

    # 优先匹配“中字无码/中字無碼”或“中字 & (无码|無碼)” -> UC
    if has_any(ot, ["[中字无码]", "[中字無碼]", "中字无码", "中字無碼"]) or has_any(tt, ["[中字无码]", "[中字無碼]", "中字无码", "中字無碼"]) or \
       (("中字" in ot) and ("无码" in ot or "無碼" in ot)) or (("中字" in tt) and ("无码" in tt or "無碼" in tt)):
        return "-UC"

    # 仅有“无码/無碼”
    if has_any(ot, ["[无码]", "[無碼]", "无码", "無碼"]) or has_any(tt, ["[无码]", "[無碼]", "无码", "無碼"]):
        return "-U"

    # 仅有“中字”
    if "[中字]" in ot or "[中字]" in tt or "中字" in ot or "中字" in tt:
        return "-C"

    # 从目录名判断
    dn = dir_name or ""
    if has_any(dn, ["中字无码", "中字無碼"]):
        return "-UC"
    if ("中字" in dn) and ("无码" in dn or "無碼" in dn):
        return "-UC"
    if "无码" in dn or "無碼" in dn:
        return "-U"
    if "中字" in dn:
        return "-C"
    return ""


def _backup_file_increment(path: str) -> str:
    base = path + ".bak"
    if not os.path.exists(base):
        shutil.copy2(path, base)
        return base
    i = 1
    while True:
        candidate = f"{base}.{i}"
        if not os.path.exists(candidate):
            shutil.copy2(path, candidate)
            return candidate
        i += 1


def fix_sorttitle_for_nfo(nfo_path: str) -> bool:
    try:
        tree = ET.parse(nfo_path)
        root = tree.getroot()
        originaltitle_elem = root.find("originaltitle")
        title_elem = root.find("title")
        sorttitle_elem = root.find("sorttitle")

        originaltitle = (originaltitle_elem.text or "").strip() if originaltitle_elem is not None else ""
        title = (title_elem.text or "").strip() if title_elem is not None else ""
        dir_name = os.path.basename(os.path.dirname(nfo_path.rstrip(os.sep)))

        base = _extract_fanhao(originaltitle) or _extract_fanhao(title) or _extract_fanhao(dir_name)
        if not base:
            print(f"未能从标题/目录中提取番号，跳过: {nfo_path}")
            return False

        # 只保留“前缀-数字”的基本形态
        m = FANHAO_HYPHEN_RE.search(base)
        if m:
            base = m.group(1)

        suffix = _determine_suffix(originaltitle, title, dir_name)
        new_sorttitle = f"{base}{suffix}"

        # 备份
        _backup_file_increment(nfo_path)

        if sorttitle_elem is None:
            sorttitle_elem = ET.SubElement(root, 'sorttitle')
        sorttitle_elem.text = new_sorttitle

        tree.write(nfo_path, encoding='utf-8', xml_declaration=True)

        ok = bool(SORTTITLE_RE.match(new_sorttitle))
        if ok:
            print(f"已修复 sorttitle: {nfo_path} -> {new_sorttitle}")
        else:
            print(f"写入的新 sorttitle 未通过校验: {new_sorttitle} @ {nfo_path}")
        return ok
    except Exception as e:
        print(f"修复 sorttitle 失败: {nfo_path}，原因: {e}")
        return False
# ---------------- 新增函数结束 ----------------

def process_root(root_path: str, report_path: str):
    invalid_list = []
    for r, dnames, fnames in os.walk(root_path):
        if not is_leaf_dir(r, dnames):
            continue
        # 3. 检查 leaf 目录中的 nfo sorttitle
        for fn in fnames:
            if fn.lower().endswith('.nfo'):
                nfo_fp = os.path.join(r, fn)
                if not validate_sorttitle(nfo_fp):
                    # 优先尝试修复 sorttitle
                    fixed = False # fix_sorttitle_for_nfo(nfo_fp)
                    fixed = fix_sorttitle_for_nfo(nfo_fp)
                    if not (fixed and validate_sorttitle(nfo_fp)):
                        invalid_list.append(nfo_fp)
        # 4. 处理字幕命名
        ensure_subtitle_names(r)
        # 5. 解压 zip 并复制 -poster 图片
        # extract_and_copy_poster(r)
    # 写报告
    if report_path:
        os.makedirs(os.path.dirname(report_path) or '.', exist_ok=True)
        # 读取已存在的记录，避免重复
        existing = set()
        if os.path.exists(report_path):
            try:
                with open(report_path, 'r', encoding='utf-8') as f:
                    existing = {line.strip() for line in f if line.strip()}
            except Exception:
                existing = set()
        added = 0
        # 以追加方式写入新记录
        with open(report_path, 'a', encoding='utf-8') as f:
            for line in invalid_list:
                if line and line not in existing:
                    f.write(line + "\n")
                    existing.add(line)
                    added += 1
        print(f"已追加 {added} 条记录到: {report_path}，当前去重后总数约 {len(existing)} 条")
    else:
        print("不符合 sorttitle 的 NFO:")
        for p in invalid_list:
            print("  ", p)


def main():
    # parser = argparse.ArgumentParser(description="修复元数据与文件规范化：检查 NFO 的 sorttitle，规范字幕命名，解压 zip 提取海报")
    # parser.add_argument("path", help="需要处理的根目录（可为网络路径，如 \\\\192.168.1.199\\share）")
    # parser.add_argument("-r", "--report", help="输出不合规 sorttitle 列表的 txt 路径（默认打印到控制台）", default=None)
    # args = parser.parse_args()

    root = os.path.abspath('\\\\192.168.1.199\\Porn\\Censored\\新ありな')




    if not os.path.exists(root):
        print(f"路径不存在: {root}")
        return
    report = "metafix_invalid_sorttitle.txt"
    process_root(root, report)


if __name__ == "__main__":
    main()
