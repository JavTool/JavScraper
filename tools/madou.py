import requests
from bs4 import BeautifulSoup
import re
import datetime
import os
from urllib.parse import urlparse
import argparse
import xml.etree.ElementTree as ET

# 处理日期格式，支持将"*天前"和"*小时前"转换为 yyyy-mm-dd 格式
def convert_relative_date(date_str):
    
    # 匹配"*天前"或"*小时前"格式
    days_ago_match = re.match(r'(\d+)\s*天前', date_str)
    hours_ago_match = re.match(r'(\d+)\s*小时前', date_str)
    
    today = datetime.datetime.now()
    
    if days_ago_match:
        days = int(days_ago_match.group(1))
        target_date = today - datetime.timedelta(days=days)
        return target_date.strftime('%Y-%m-%d')
    elif hours_ago_match:
        hours = int(hours_ago_match.group(1))
        target_date = today - datetime.timedelta(hours=hours)
        return target_date.strftime('%Y-%m-%d')
    else:
        # 如果不是相对日期格式，则原样返回
        return date_str


# 拼接文件名 "[发布时间] - [番号] - [片名]"
def generate_filename(date, fanhao, title, girl):
    # 清理文件名中的非法字符
    def clean_filename(name):
        # 替换 Windows 文件名中不允许的字符
        invalid_chars = ['\\', '/', ':', '*', '?', '"', '<', '>', '|']
        for char in invalid_chars:
            name = name.replace(char, '_')
        return name
    
    # 确保所有部分都有值，如果没有则使用默认值
    date_part = date if date else "未知日期"
    fanhao_part = fanhao if fanhao else "未知番号"
    title_part = clean_filename(title) if title else "未知片名"
    girl_part = girl if girl else "未知性别"
    
    # 拼接文件名
    return f"[{date_part}] - [{fanhao_part}] - [{title_part} {girl}]"


# 下载图片到本地指定路径
def download_images(image_urls, save_dir, meta_date='', fanhao='', girl='', pianming='', use_combined_name=True, headers=None):
    if headers is None:
        headers = {
            'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36'
        }
    
    # 确保保存目录存在
    os.makedirs(save_dir, exist_ok=True)
    
    # 生成基础文件名
    base_filename = ""
    if use_combined_name:
        base_filename = generate_filename(meta_date, fanhao, pianming, girl)
    
    downloaded_paths = []
    for i, img_url in enumerate(image_urls):
        try:
            # 获取文件扩展名
            parsed_url = urlparse(img_url)
            file_ext = os.path.splitext(parsed_url.path)[1]
            if not file_ext:
                file_ext = '.jpg'  # 默认扩展名
            
            # 构建保存路径
            if use_combined_name and base_filename:
                filename = f"{base_filename}{file_ext}"
            elif fanhao:  # 向后兼容，使用番号作为前缀
                filename = f"{fanhao}{file_ext}"
            else:
                filename = f"image_{i+1}{file_ext}"
            
            save_path = os.path.join(save_dir, filename)
            
            # 下载图片
            img_response = requests.get(img_url, headers=headers, stream=True)
            img_response.raise_for_status()
            
            # 保存图片
            with open(save_path, 'wb') as f:
                for chunk in img_response.iter_content(chunk_size=8192):
                    f.write(chunk)
            
            downloaded_paths.append(save_path)
            print(f"已下载图片: {save_path}")
            
        except Exception as e:
            print(f"下载图片失败 {img_url}: {str(e)}")
    
    return downloaded_paths


def scrape_madou(jav_id, source_url=None, source_file_path="./downloaded", save_directory="./downloaded"):
    """
    从 madouqu.com 网站抓取指定番号的信息和图片
    
    参数:
        jav_id (str): 要抓取的番号，例如 "91KCM-142"
        source_url (str, optional): 源文件 URL，如果为 None 则自动根据番号生成 URL
        save_directory (str, optional): 保存图片的目录路径，默认为"./downloaded_images"
        source_file_path (str, optional): 源文件路径，如果提供，将会被移动到新创建的文件夹中
        
    返回:
        dict: 包含抓取结果的字典，包括番号、女郎、片名、标题、发布时间、片商、图片数量和下载的图片路径
    """
    
    headers = {
        'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36'
    }
    
    # 如果没有提供源 URL，则根据番号生成 URL
    if source_url is None:
        # 将番号转换为小写并替换破折号为连字符
        formatted_id = jav_id.lower().replace('-', '-')
        source_url = f'https://madouqu.com/video/{formatted_id}/'
    
    # 发送请求获取页面内容
    response = requests.get(source_url, headers=headers)
    soup = BeautifulSoup(response.text, 'html.parser')

    # 标题 (.entry-title)
    title = soup.select_one('.entry-title').get_text(strip=True)

    # 发布时间 (.entry-header > .meta-date)
    meta_date_raw = soup.select_one('.entry-header .meta-date').get_text(strip=True)
    meta_date = convert_relative_date(meta_date_raw)

    # 片商 (.entry-header > .meta-category)
    meta_category = soup.select_one('.entry-header .meta-category').get_text(strip=True)

    # 图片 (.entry-content 内的 img 标签)
    images = [img['src'] for img in soup.select('.entry-content img')]

    # 页面正文中寻找番号、片名、女郎
    # 获取 .entry-content 下的所有段落元素
    entry_content_elements = soup.select('.entry-content p')

    # 初始化变量
    fanhao = jav_id  # 默认使用传入的番号
    girl = ''
    pianming = ''

    # 遍历所有段落元素
    for element in entry_content_elements:
        element_text = element.get_text(strip=True)
        
        # 在同一元素中搜索番号（支持"番号"和"番號"两种格式）
        if '番号' in element_text or '番號' in element_text:
            fanhao_match = re.search(r'番号[:：]\s*([A-Z0-9\-]+)', element_text)
            if not fanhao_match:
                fanhao_match = re.search(r'番號[:：]\s*([A-Z0-9\-]+)', element_text)
            if fanhao_match:
                fanhao = fanhao_match.group(1)
        
        # 在同一元素中搜索女郎
        if '女郎' in element_text:
            girl_match = re.search(r'女郎[:：]\s*([\u4e00-\u9fa5a-zA-Z0-9\s]+)', element_text)
            if girl_match:
                girl = girl_match.group(1).strip()
        
        # 在同一元素中搜索片名
        if '片名' in element_text:
            title_match = re.search(r'片名[:：]\s*(.+)', element_text)
            if title_match:
                pianming = title_match.group(1).strip()


    # 创建基于片商的文件夹
    # 清理片商名称中的非法字符
    def clean_dirname(name):
        # 替换 Windows 文件名中不允许的字符
        invalid_chars = ['\\', '/', ':', '*', '?', '"', '<', '>', '|']
        for char in invalid_chars:
            name = name.replace(char, '_')
        return name
    
    # 创建片商文件夹
    clean_category = clean_dirname(meta_category)
    category_dir = os.path.join(save_directory, clean_category)
    os.makedirs(category_dir, exist_ok=True)
    
    # 创建基于文件名的子文件夹
    folder_name = generate_filename(meta_date, fanhao, pianming, girl)
    movie_dir = os.path.join(category_dir, folder_name)
    os.makedirs(movie_dir, exist_ok=True)
    
    print(f"创建文件夹: {movie_dir}")
    
    # 如果提供了源文件路径，将源文件移动到新文件夹
    if source_file_path and os.path.exists(source_file_path):
        # 获取源文件名
        source_filename = os.path.basename(source_file_path)
        # 获取源文件扩展名
        source_ext = os.path.splitext(source_file_path)[1]
        # 构建目标路径
        target_path = os.path.join(movie_dir, folder_name + source_ext)
        
        try:
            # 如果目标文件已存在，先删除
            if os.path.exists(target_path):
                os.remove(target_path)
                
            # 复制文件（而不是移动，以防出错）
            import shutil
            shutil.copy2(source_file_path, target_path)
            print(f"已复制源文件到: {target_path}")
        except Exception as e:
            print(f"复制源文件失败: {str(e)}")
    
    # 下载图片到电影文件夹（使用拼接的文件名格式：[发布时间] - [番号] - [片名]）
    downloaded_image_paths = download_images(images, movie_dir, meta_date, fanhao, girl, pianming, True, headers)
    print(f"共下载了 {len(downloaded_image_paths)} 张图片到 {movie_dir}")
    
    # 复制第一张图片为 poster.jpg
    if downloaded_image_paths:
        first_image_path = downloaded_image_paths[0]
        poster_path = os.path.join(movie_dir, "poster.jpg")
        try:
            import shutil
            shutil.copy2(first_image_path, poster_path)
            print(f"已复制封面图片: {poster_path}")
        except Exception as e:
            print(f"复制封面图片失败: {str(e)}")

    # 输出采集结果
    print('番号:', fanhao)
    print('女郎:', girl)
    print('片名:', pianming)
    print('标题:', title)
    print('发布时间:', meta_date)
    print('片商:', meta_category)
    print('图片数量:', len(images))
    
    # 返回结果字典
    result = {
        'fanhao': fanhao,
        'girl': girl,
        'pianming': pianming,
        'title': title,
        'meta_date': meta_date,
        'meta_category': meta_category,
        'image_count': len(images),
        'downloaded_paths': downloaded_image_paths
    }
    
    # 生成 NFO 文件
    nfo_path = os.path.join(movie_dir, folder_name)
    generate_nfo_file(result, nfo_path)
    
    return result


# 从文件名中提取番号
def extract_jav_id_from_filename(filename):
    """
    从文件名中提取番号
    
    参数:
        filename (str): 文件名
        
    返回:
        str: 提取到的番号，如果没有找到则返回 None
    """
    # 常见的番号格式：字母+数字，如 ABC-123、XYZ-0001 等
    # 支持中括号格式 [ABC-123] 和普通格式 ABC-123
    patterns = [
        r'\[(\w+-\d+)\]',  # [ABC-123] 格式
        r'(\w+-\d+)',       # ABC-123 格式
        r'(\w+\d+)',        # ABC123 格式（无连字符）
    ]
    
    for pattern in patterns:
        matches = re.findall(pattern, filename, re.IGNORECASE)
        if matches:
            # 返回第一个匹配结果，并转换为大写
            return matches[0].upper()
    
    return None


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


# 遍历目录并处理文件
def process_directory(directory_path="./downloaded", save_directory="./downloaded"):
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


# 示例用法
if __name__ == "__main__":

    
    # 创建命令行参数解析器
    parser = argparse.ArgumentParser(description='从 madouqu.com 网站抓取指定番号的信息和图片')
    parser.add_argument('-i', '--id', help='要抓取的番号，例如 "91KCM-142"')
    parser.add_argument('-d', '--directory', default='F:\\111\\333', help='要处理的目录路径，将遍历该目录中的文件并提取番号')
    parser.add_argument('-s', '--save', default='F:\\111\\333\\downloaded', help='保存目录路径，默认为"./downloaded"')
    parser.add_argument('-u', '--url', help='源文件 URL，如果不提供则自动根据番号生成')
    parser.add_argument('-f', '--file', help='源文件路径，如果提供，将会被移动到新创建的文件夹中')
    
    args = parser.parse_args()
    
    # 根据命令行参数执行不同的操作
    if args.directory:
        # 处理整个目录
        results = process_directory(args.directory, args.save)
        
        # 打印处理结果摘要
        if results:
            print("\n 处理结果摘要:")
            for i, result in enumerate(results, 1):
                print(f"\n{i}. 番号: {result['fanhao']}")
                print(f"   片名: {result['pianming']}")
                print(f"   片商: {result['meta_category']}")
                print(f"   下载图片数: {result['image_count']}")
    
    elif args.id:
        # 处理单个番号
        result = scrape_madou(
            jav_id=args.id,
            source_url=args.url,
            save_directory=args.save,
            source_file_path=args.file
        )
        
        # 打印结果
        print("\n 抓取完成，结果摘要:")
        print(f"番号: {result['fanhao']}")
        print(f"片名: {result['pianming']}")
        print(f"片商: {result['meta_category']}")
        print(f"下载图片数: {result['image_count']}")
    
    else:
        # 如果没有提供参数，显示帮助信息
        parser.print_help()