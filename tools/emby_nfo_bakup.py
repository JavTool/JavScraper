r'''
Emby NFO 备份工具 - 备份 Emby 生成的 NFO 文件
说明：备份 Emby 生成的 NFO 文件，要根据源目录和目标目录的路径，将 NFO 文件从源目录复制到目标目录的对应路径，文件夹结构和文件名请保持一致，最后压缩成 zip 文件，文件名格式为 [源目录名称]_[日期].zip，例如：G_Emby_NFO_20230401.zip。
用法：python emby_nfo_bakup.py "源目录" "目标目录"
示例：python emby_nfo_bakup.py "\\192.168.1.199\Video\Cartoon" "G:/Emby/NFO_Bak"
'''

import os
import sys
import shutil
import zipfile
import tempfile
import argparse
from datetime import datetime


# 需要忽略的目录名（大小写不敏感）
IGNORE_DIRS = {'#recycle'}


def collect_nfo_files(source_dir):
    """递归搜集源目录下所有 .nfo 文件，返回 [(绝对路径, 相对路径), ...]"""
    result = []
    source_dir = os.path.abspath(source_dir)
    for root, dirs, files in os.walk(source_dir):
        # 原地过滤忽略目录（阻止 os.walk 继续下钻）
        dirs[:] = [d for d in dirs if d.lower() not in IGNORE_DIRS]
        for f in files:
            if f.lower().endswith('.nfo'):
                full_path = os.path.join(root, f)
                rel_path = os.path.relpath(full_path, source_dir)
                result.append((full_path, rel_path))
    return result


def copy_nfo_to_target(source_dir, staging_dir):
    """将 NFO 文件从源目录拷贝到暂存目录，保持目录结构

    返回: (拷贝后的目标根目录, 拷贝文件数)
    """
    source_dir = os.path.abspath(source_dir)
    staging_dir = os.path.abspath(staging_dir)

    # 暂存目录下建立与源目录同名的子目录（保留源根文件夹名）
    source_name = os.path.basename(source_dir.rstrip(os.sep))
    dest_root = os.path.join(staging_dir, source_name)

    nfo_files = collect_nfo_files(source_dir)
    if not nfo_files:
        print(f"[WARN] 源目录下未找到 .nfo 文件: {source_dir}")
        return dest_root, 0

    print(f"[INFO] 源目录: {source_dir}")
    print(f"[INFO] 暂存目录: {dest_root}")
    print(f"[INFO] 发现 {len(nfo_files)} 个 nfo 文件\n")

    copied = 0
    for full_path, rel_path in nfo_files:
        dest_path = os.path.join(dest_root, rel_path)
        os.makedirs(os.path.dirname(dest_path), exist_ok=True)
        try:
            shutil.copy2(full_path, dest_path)
            copied += 1
        except Exception as e:
            print(f"[ERROR] 拷贝失败 {rel_path}: {e}")

    print(f"[OK] 拷贝完成: {copied}/{len(nfo_files)}")
    return dest_root, copied


def build_zip_name(source_dir):
    """根据传入的源目录构建 zip 文件名：[源目录最后一级名]_[日期].zip

    使用统一分隔符切分后取最后一段非空部分，兼容 UNC、正/反斜杠、尾部分隔符等情况。
    例如：
      '\\\\192.168.1.199\\Jav'              → 'Jav_20230401.zip'
      '\\\\192.168.1.199\\Jav\\Hotel Vixen 2' → 'Hotel Vixen 2_20230401.zip'
      'G:/Emby/NFO'                        → 'NFO_20230401.zip'
    """
    # 统一分隔符，去除首/尾空白和分隔符，切片后取最后一段非空值
    normalized = source_dir.strip().replace('\\', '/').strip('/')
    parts = [p for p in normalized.split('/') if p]
    base = parts[-1] if parts else ''
    # 只替换 Windows 文件名的非法字符，保留空格/中文/点号等
    illegal = set('<>:"/\\|?*')
    safe_name = "".join('_' if c in illegal else c for c in base).strip()
    safe_name = safe_name or ''
    date_str = datetime.now().strftime('%Y%m%d')
    return f"{safe_name}_backup_{date_str}.zip"


def zip_directory(src_folder, zip_path):
    """将文件夹内部所有文件压缩为 zip

    archive 路径直接相对 src_folder 计算，zip 解压后即为 src_folder 内部平铺内容，
    不包含 src_folder 自身这一层和任何上层目录（如 Bak_Temp）。
    """
    src_folder = os.path.abspath(src_folder)
    if not os.path.isdir(src_folder):
        print(f"[WARN] 待压缩目录不存在: {src_folder}")
        return False

    count = 0
    with zipfile.ZipFile(zip_path, 'w', zipfile.ZIP_DEFLATED) as zf:
        for root, dirs, files in os.walk(src_folder):
            for f in files:
                full = os.path.join(root, f)
                # archive 路径相对 src_folder，不包含 src_folder 自身和任何上层路径
                arc = os.path.relpath(full, src_folder)
                zf.write(full, arc)
                count += 1

    print(f"[OK] 已压缩 {count} 个文件到: {zip_path}")
    return True


def _is_subpath(child, parent):
    """判断 child 是否位于 parent 内部（包括相等）"""
    try:
        child_n = os.path.normcase(os.path.abspath(child))
        parent_n = os.path.normcase(os.path.abspath(parent))
        return child_n == parent_n or child_n.startswith(
            parent_n.rstrip(os.sep) + os.sep
        )
    except Exception:
        return False


def backup_nfo(source_dir, target_dir, keep_copies=False):
    """备份 NFO 主流程。keep_copies=True 时保留暂存目录 Bak_Temp，默认压缩后清理"""
    # 保留用户传入的原始路径用于 zip 命名
    raw_source = source_dir
    source_dir = os.path.abspath(source_dir)
    target_dir = os.path.abspath(target_dir)

    if not os.path.isdir(source_dir):
        print(f"[ERROR] 源目录不存在: {source_dir}")
        return False

    os.makedirs(target_dir, exist_ok=True)

    # 暂存目录：目标目录下的 Bak_Temp
    staging_dir = os.path.join(target_dir, 'Bak_Temp')
    os.makedirs(staging_dir, exist_ok=True)

    # 第一步：拷贝 NFO 文件到暂存目录
    dest_root, copied = copy_nfo_to_target(source_dir, staging_dir)
    if copied == 0:
        # 没拷到也尝试清理空的 Bak_Temp
        try:
            shutil.rmtree(staging_dir)
        except Exception:
            pass
        return False
    dest_root = os.path.abspath(dest_root)

    # 第二步：构建最终 zip 路径（使用用户传入的原始路径命名，直接放到 target_dir）
    zip_name = build_zip_name(raw_source)
    final_zip_path = os.path.abspath(os.path.join(target_dir, zip_name))

    # 第三步：在 Bak_Temp 外的系统 temp 写 zip，避免被 Bak_Temp 清理波及
    tmp_fd, tmp_zip_path = tempfile.mkstemp(
        prefix="emby_nfo_bakup_", suffix=".zip"
    )
    os.close(tmp_fd)
    try:
        if not zip_directory(dest_root, tmp_zip_path):
            return False

        # 第四步：清理 Bak_Temp（默认直接删除）
        if not keep_copies:
            try:
                shutil.rmtree(staging_dir)
                print(f"[OK] 已清理暂存目录: {staging_dir}")
            except Exception as e:
                print(f"[WARN] 清理暂存目录失败: {e}")

        # 第五步：将临时 zip 移到最终位置（同名覆盖）
        if os.path.exists(final_zip_path):
            os.remove(final_zip_path)
        shutil.move(tmp_zip_path, final_zip_path)
        print(f"[OK] zip 已输出到: {final_zip_path}")
    finally:
        # 异常时清理临时文件
        if os.path.exists(tmp_zip_path):
            try:
                os.remove(tmp_zip_path)
            except Exception:
                pass

    print(f"\n{'=' * 50}")
    print(f"备份完成:")
    print(f"  nfo 文件数: {copied}")
    print(f"  zip 文件 : {final_zip_path}")
    return True


def main():
    parser = argparse.ArgumentParser(
        description="Emby NFO 备份工具 - 拷贝 NFO 保持目录结构并压缩为 zip"
    )
    parser.add_argument("source_dir", help="源目录（包含 .nfo 文件）")
    parser.add_argument("target_dir", help="目标目录（存放备份文件和 zip）")
    parser.add_argument(
        "--keep-copies",
        action="store_true",
        help="压缩后保留 Bak_Temp 暂存目录（默认直接删除）"
    )
    args = parser.parse_args()

    ok = backup_nfo(
        args.source_dir, args.target_dir, keep_copies=args.keep_copies
    )
    sys.exit(0 if ok else 1)


if __name__ == "__main__":
    main()