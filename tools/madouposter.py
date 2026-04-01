import requests
from bs4 import BeautifulSoup
import re
import datetime
import os
from urllib.parse import urlparse
import argparse
import xml.etree.ElementTree as ET
import json
import shutil
import zhconv
from PIL import Image

def generate_nfo_file(result_data, nfo_path):
    """
    根据抓取结果生成 NFO 文件
    
    参数:
        result_data (dict): scrape_madou 函数返回的结果字典
        nfo_path (str): NFO 文件保存路径（不包含扩展名）
    """
    # 创建根元素
    movie = ET.Element('movie')
    
    # plot 元素（使用 CDATA）
    plot_elem = ET.SubElement(movie, 'plot')
    plot_text = f"{result_data.get('title', '')}"
    # 注意：ElementTree 不直接支持 CDATA，这里直接设置文本
    plot_elem.text = plot_text
    
    # outline 元素
    ET.SubElement(movie, 'outline')
    
    # lockdata 元素
    lockdata_elem = ET.SubElement(movie, 'lockdata')
    lockdata_elem.text = 'false'
    
    # dateadded 元素
    dateadded_elem = ET.SubElement(movie, 'dateadded')
    dateadded_elem.text = result_data.get('meta_date', '')
    
    # title 元素
    title_elem = ET.SubElement(movie, 'title')
    title_elem.text = result_data.get('pianming', '') or result_data.get('title', '')
    
    # originaltitle 元素
    originaltitle_elem = ET.SubElement(movie, 'originaltitle')
    originaltitle_elem.text = result_data.get('title', '')
    
    # actor 元素（如果有女郎信息）
    if result_data.get('girl'):
        actor_elem = ET.SubElement(movie, 'actor')
        name_elem = ET.SubElement(actor_elem, 'name')
        name_elem.text = result_data.get('girl', '')
        type_elem = ET.SubElement(actor_elem, 'type')
        type_elem.text = 'Actor'
    
    # year 元素
    year_elem = ET.SubElement(movie, 'year')
    date_str = result_data.get('meta_date', '')
    if date_str and len(date_str) >= 4:
        year_elem.text = date_str[:4]
    
    # sorttitle 元素
    sorttitle_elem = ET.SubElement(movie, 'sorttitle')
    sorttitle_elem.text = result_data.get('fanhao', '')
    
    # mpaa 元素
    mpaa_elem = ET.SubElement(movie, 'mpaa')
    mpaa_elem.text = 'XXX'
    
    # premiered 元素
    premiered_elem = ET.SubElement(movie, 'premiered')
    premiered_elem.text = result_data.get('meta_date', '')
    
    # releasedate 元素
    releasedate_elem = ET.SubElement(movie, 'releasedate')
    releasedate_elem.text = result_data.get('meta_date', '')
    
    # runtime 元素
    runtime_elem = ET.SubElement(movie, 'runtime')
    runtime_elem.text = '30'
    
    # genre 元素（使用片商作为类型）
    if result_data.get('meta_category'):
        genre_elem = ET.SubElement(movie, 'genre')
        genre_elem.text = result_data.get('meta_category', '')
    
    # studio 元素
    studio_elem = ET.SubElement(movie, 'studio')
    studio_elem.text = result_data.get('meta_category', '91 制片厂')
    
    # uniqueid 元素（JavScraper 类型）
    # uniqueid_elem = ET.SubElement(movie, 'uniqueid')
    # uniqueid_elem.set('type', 'JavScraper')
    # uniqueid_elem.text = result_data.get('fanhao', '')
    
    # javscraperid 元素
    javscraperid_elem = ET.SubElement(movie, 'javscraperid')
    javscraperid_elem.text = result_data.get('fanhao', '')
    
    # uniqueid 元素（JSON 类型）
    # json_uniqueid_elem = ET.SubElement(movie, 'uniqueid')
    # json_uniqueid_elem.set('type', 'JavScraper-Json')
    # json_data = {
    #     'OriginalTitle': result_data.get('title', ''),
    #     'Cover': result_data.get('downloaded_paths', [''])[0] if result_data.get('downloaded_paths') else '',
    #     'Date': result_data.get('meta_date', '')
    # }
    # import json
    # json_uniqueid_elem.text = json.dumps(json_data, ensure_ascii=False)
    
    # javscraper-jsonid 元素
    # json_id_elem = ET.SubElement(movie, 'javscraper-jsonid')
    # json_id_elem.text = json.dumps(json_data, ensure_ascii=False)
    
    # fileinfo 元素（视频和音频信息）
    fileinfo_elem = ET.SubElement(movie, 'fileinfo')
    streamdetails_elem = ET.SubElement(fileinfo_elem, 'streamdetails')
    
    # video 信息
    video_elem = ET.SubElement(streamdetails_elem, 'video')
    
    # 添加视频编解码器信息
    codec_elem = ET.SubElement(video_elem, 'codec')
    codec_elem.text = 'h264'
    
    micodec_elem = ET.SubElement(video_elem, 'micodec')
    micodec_elem.text = 'h264'
    
    bitrate_elem = ET.SubElement(video_elem, 'bitrate')
    bitrate_elem.text = '2909267'
    
    width_elem = ET.SubElement(video_elem, 'width')
    width_elem.text = '1280'
    
    height_elem = ET.SubElement(video_elem, 'height')
    height_elem.text = '720'
    
    aspect_elem = ET.SubElement(video_elem, 'aspect')
    aspect_elem.text = '16:9'
    
    aspectratio_elem = ET.SubElement(video_elem, 'aspectratio')
    aspectratio_elem.text = '16:9'
    
    framerate_elem = ET.SubElement(video_elem, 'framerate')
    framerate_elem.text = '29.97003'
    
    scantype_elem = ET.SubElement(video_elem, 'scantype')
    scantype_elem.text = 'progressive'
    
    default_elem = ET.SubElement(video_elem, 'default')
    default_elem.text = 'False'
    
    forced_elem = ET.SubElement(video_elem, 'forced')
    forced_elem.text = 'False'
    
    duration_elem = ET.SubElement(video_elem, 'duration')
    duration_elem.text = '33'
    
    durationinseconds_elem = ET.SubElement(video_elem, 'durationinseconds')
    durationinseconds_elem.text = '2031'
    
    # audio 信息
    audio_elem = ET.SubElement(streamdetails_elem, 'audio')
    
    audio_codec_elem = ET.SubElement(audio_elem, 'codec')
    audio_codec_elem.text = 'aac'
    
    audio_micodec_elem = ET.SubElement(audio_elem, 'micodec')
    audio_micodec_elem.text = 'aac'
    
    audio_bitrate_elem = ET.SubElement(audio_elem, 'bitrate')
    audio_bitrate_elem.text = '319940'
    
    language_elem = ET.SubElement(audio_elem, 'language')
    language_elem.text = 'eng'
    
    audio_scantype_elem = ET.SubElement(audio_elem, 'scantype')
    audio_scantype_elem.text = 'progressive'
    
    channels_elem = ET.SubElement(audio_elem, 'channels')
    channels_elem.text = '2'
    
    samplingrate_elem = ET.SubElement(audio_elem, 'samplingrate')
    samplingrate_elem.text = '48000'
    
    audio_default_elem = ET.SubElement(audio_elem, 'default')
    audio_default_elem.text = 'False'
    
    audio_forced_elem = ET.SubElement(audio_elem, 'forced')
    audio_forced_elem.text = 'False'
    
    # 创建 XML 树并保存
    tree = ET.ElementTree(movie)
    
    # 设置 XML 声明
    ET.register_namespace('', '')  # 避免默认命名空间前缀
    
    # 保存到文件
    nfo_file_path = f"{nfo_path}.nfo"
    tree.write(nfo_file_path, encoding='utf-8', xml_declaration=True)
    
    print(f"NFO 文件已生成: {nfo_file_path}")
    return nfo_file_path


    """
    遍历指定目录中的文件，提取番号并爬取信息
    
    参数:
        directory_path (str): 要遍历的目录路径
        save_directory (str): 保存图片的目录路径
        
    返回:
        list: 处理结果列表
    """
    results = []
    processed_ids = set()  # 用于跟踪已处理的番号，避免重复处理
    
    if not os.path.exists(directory_path):
        print(f"错误: 目录 {directory_path} 不存在")
        return results
    
    print(f"开始处理目录: {directory_path}")
    
    # 遍历目录中的所有文件
    for root, dirs, files in os.walk(directory_path):
        for file in files:
            file_path = os.path.join(root, file)
            
            # 跳过非视频文件（简单判断，可以根据需要调整）
            video_extensions = [".mp4", ".avi", ".mkv", ".wmv", ".mov", ".flv", ".ts", ".webm"]
            if not any(file.lower().endswith(ext) for ext in video_extensions):
                continue
            
            # 从文件名中提取番号
            jav_id = extract_jav_id_from_filename(file)
            if '-' not in jav_id:
                jav_id = re.sub(r'([A-Z]+)([0-9]+)', r'\1-\2', jav_id)
            if jav_id and jav_id not in processed_ids:
                print(f"\n 处理文件: {file}")
                print(f"提取到番号: {jav_id}")
                
                try:
                    # 爬取信息并保存
                    result = scrape_madou(jav_id, save_directory=save_directory, source_file_path=file_path)
                    results.append(result)
                    processed_ids.add(jav_id)
                    
                    print(f"成功处理番号: {jav_id}")
                except Exception as e:
                    print(f"处理番号 {jav_id} 时出错: {str(e)}")
    
    print(f"\n 目录处理完成，共处理 {len(results)} 个文件")
    return results


def convert_traditional_to_simplified(text):
    """
    将繁体字转换为简体字
    
    参数:
        text (str): 需要转换的文本
        
    返回:
        str: 转换后的简体字文本
    """
    if not text:
        return text
    
    # 使用 zhconv 库进行繁简转换
    try:
        return zhconv.convert(text, 'zh-cn')
    except Exception as e:
        print(f"繁简转换失败: {e}")
        return text


def format_sorttitle(sorttitle, javscraperid):
    """
    格式化 sorttitle 字段
    
    参数:
        sorttitle (str): 原始 sorttitle 值
        javscraperid (str): javscraperid 值
        
    返回:
        str: 格式化后的 sorttitle
    """
    # 如果 javscraperid 包含 "-"，直接使用 javscraperid 的值
    if javscraperid and '-' in javscraperid:
        return javscraperid
    
    # 如果 sorttitle 不包含 "-"，在字母和数字之间添加 "-"
    if sorttitle and '-' not in sorttitle:
        # 处理 "91" 开头的特殊情况
        if sorttitle.startswith('91'):
            # 匹配 91 + 字母 + 数字的模式
            match = re.match(r'(91)([A-Z]+)(\d+)', sorttitle)
            if match:
                return f"{match.group(1)}{match.group(2)}-{match.group(3)}"
        
        # 普通情况：在字母和数字之间添加 "-"
        return re.sub(r'([A-Z]+)(\d+)', r'\1-\2', sorttitle)
    
    return sorttitle


def process_nfo_files(directory_path):
    """
    递归遍历指定目录中的 nfo 文件，读取并修改指定字段
    
    参数:
        directory_path (str): 要遍历的目录路径
    """
    if not os.path.exists(directory_path):
        print(f"错误: 目录 {directory_path} 不存在")
        return
    
    print(f"开始处理目录: {directory_path}")
    processed_count = 0
    
    # 递归遍历目录中的所有 nfo 文件
    for root, dirs, files in os.walk(directory_path):
        for file in files:
            if file.lower().endswith('.nfo'):
                file_path = os.path.join(root, file)
                try:
                    process_single_nfo_file(file_path)
                    processed_count += 1
                    print(f"已处理: {file_path}")
                except Exception as e:
                    print(f"处理文件 {file_path} 时出错: {str(e)}")
    
    print(f"\n处理完成，共处理 {processed_count} 个 nfo 文件")


def copy_poster_to_fanart(nfo_path):
    """
    在 nfo 文件所在目录中处理 poster 图片：
    1. 检查目录下名为 poster 的图片文件，如果不是 jpg 格式就转换为 poster.jpg
    2. 将 poster.jpg 复制为 fanart.jpg 和 thumb.jpg
    
    参数:
        nfo_path (str): nfo 文件路径
    """
    try:
        # 获取 nfo 文件所在目录
        nfo_dir = os.path.dirname(nfo_path)
        
        # 支持的图片格式
        poster_extensions = ['.jpg', '.jpeg', '.png', '.webp', '.bmp']
        poster_base_path = os.path.join(nfo_dir, 'poster')
        poster_jpg_path = os.path.join(nfo_dir, 'poster.jpg')
        
        # 查找 poster 图片文件
        found_poster = None
        for ext in poster_extensions:
            poster_file = poster_base_path + ext
            if os.path.exists(poster_file):
                found_poster = poster_file
                break
        
        if found_poster:
            # 如果找到的不是 poster.jpg，则转换为 jpg 格式
            if found_poster != poster_jpg_path:
                print(f"发现 poster 图片: {found_poster}")
                try:
                    # 使用 PIL 转换图片格式
                    with Image.open(found_poster) as img:
                        # 如果是 RGBA 模式，转换为 RGB
                        if img.mode in ('RGBA', 'LA', 'P'):
                            # 创建白色背景
                            background = Image.new('RGB', img.size, (255, 255, 255))
                            if img.mode == 'P':
                                img = img.convert('RGBA')
                            background.paste(img, mask=img.split()[-1] if img.mode in ('RGBA', 'LA') else None)
                            img = background
                        elif img.mode != 'RGB':
                            img = img.convert('RGB')
                        
                        # 保存为 jpg 格式
                        img.save(poster_jpg_path, 'JPEG', quality=95)
                        print(f"已转换为 poster.jpg: {poster_jpg_path}")
                        
                        # 删除原文件（如果不是 jpg）
                        if found_poster.lower().endswith(('.png', '.webp', '.bmp')):
                            os.remove(found_poster)
                            print(f"已删除原文件: {found_poster}")
                            
                except Exception as e:
                    print(f"转换图片格式时出错: {e}")
                    return
            else:
                print(f"发现 poster.jpg: {poster_jpg_path}")
            
            # 复制 poster.jpg 为 fanart.jpg 和 thumb.jpg
            fanart_path = os.path.join(nfo_dir, 'fanart.jpg')
            thumb_path = os.path.join(nfo_dir, 'thumb.jpg')
            
            try:
                shutil.copy2(poster_jpg_path, fanart_path)
                print(f"已复制 poster.jpg 为 fanart.jpg: {fanart_path}")
                
                shutil.copy2(poster_jpg_path, thumb_path)
                print(f"已复制 poster.jpg 为 thumb.jpg: {thumb_path}")
            except Exception as e:
                print(f"复制文件时出错: {e}")
                
        else:
            print(f"未找到 poster 图片文件（支持格式: {', '.join(poster_extensions)}）")
            
    except Exception as e:
        print(f"处理 poster 图片时出错: {e}")


def process_single_nfo_file(nfo_path):
    """
    处理单个 nfo 文件
    
    参数:
        nfo_path (str): nfo 文件路径
    """
    try:
        # 解析 XML 文件
        tree = ET.parse(nfo_path)
        root = tree.getroot()
        
        # 读取需要修改的字段
        plot_elem = root.find('plot')
        title_elem = root.find('title')
        originaltitle_elem = root.find('originaltitle')
        sorttitle_elem = root.find('sorttitle')
        javscraperid_elem = root.find('javscraperid')
        
        # 获取原始值
        plot_text = plot_elem.text if plot_elem is not None else ''
        title_text = title_elem.text if title_elem is not None else ''
        originaltitle_text = originaltitle_elem.text if originaltitle_elem is not None else ''
        sorttitle_text = sorttitle_elem.text if sorttitle_elem is not None else ''
        javscraperid_text = javscraperid_elem.text if javscraperid_elem is not None else ''
        
        # 1. 对所有字段进行繁体转简体
        plot_text = convert_traditional_to_simplified(plot_text)
        title_text = convert_traditional_to_simplified(title_text)
        originaltitle_text = convert_traditional_to_simplified(originaltitle_text)
        sorttitle_text = convert_traditional_to_simplified(sorttitle_text)
        javscraperid_text = convert_traditional_to_simplified(javscraperid_text)
        
        # 2. 处理 sorttitle 字段
        formatted_sorttitle = format_sorttitle(sorttitle_text, javscraperid_text)
        
        # 3. 修改 originaltitle 为 "{sorttitle} {title}"
        new_originaltitle = f"{formatted_sorttitle} {title_text}".strip()
        
        # 4. 修改 plot 的值为 title 的值
        new_plot = title_text
        
        # 更新 XML 元素
        if plot_elem is not None:
            plot_elem.text = new_plot
        
        if title_elem is not None:
            title_elem.text = title_text
        
        if originaltitle_elem is not None:
            originaltitle_elem.text = new_originaltitle
        
        if sorttitle_elem is not None:
            sorttitle_elem.text = formatted_sorttitle
        
        if javscraperid_elem is not None:
            javscraperid_elem.text = javscraperid_text
        
        # 保存修改后的文件
        tree.write(nfo_path, encoding='utf-8', xml_declaration=True)
        
        # 5. 复制 poster.jpg 为 fanart.jpg
        copy_poster_to_fanart(nfo_path)
        
    except ET.ParseError as e:
        print(f"XML 解析错误: {e}")
        raise
    except Exception as e:
        print(f"处理文件时出错: {e}")
        raise


if __name__ == "__main__":
    import argparse
    
    parser = argparse.ArgumentParser(description='处理 nfo 文件，修改指定字段')
    parser.add_argument('directory', help='要处理的目录路径')
    
    args = parser.parse_args()
    
    # 处理 nfo 文件
    process_nfo_files(args.directory)
