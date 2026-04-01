# Caribbean.com 封面图片下载器

这个工具可以自动遍历指定目录中的 NFO 文件，识别 Caribbean.com (カリビアンコム) 工作室的影片，并从官网下载对应的封面图片。

## 功能特点

- **递归目录遍历**: 支持递归遍历指定目录及其所有子目录
- **网络路径支持**: 完全支持 Windows UNC 网络路径
- **智能识别**: 自动识别 Caribbean.com 工作室的影片
- **番号提取**: 从 NFO 文件的 `originaltitle` 和 `sorttitle` 字段提取番号
- **多格式保存**: 下载的封面会保存为 `jacket.jpg`、`folder.jpg` 和 `poster.jpg`
- **重复检测**: 自动跳过已存在封面图片的目录
- **详细日志**: 记录所有操作和错误信息到日志文件

## 使用方法

### 命令行使用

```bash
# 处理本地目录
python jacketdownload.py "C:/Videos/JAV"

# 处理网络路径
python jacketdownload.py "\\192.168.1.199\Jav\Caribbean"

# 指定自定义日志文件
python jacketdownload.py "C:/Videos/JAV" --log "my_download.log"
```

### Python 代码中使用

```python
from jacketdownload import JacketDownloader

# 创建下载器实例
downloader = JacketDownloader("download.log")

# 处理整个目录
stats = downloader.process_directory("C:/Videos/JAV")
print(f"成功下载: {stats['success_download']} 个封面")

# 处理单个 NFO 文件
success = downloader.process_nfo_file("C:/Videos/JAV/movie.nfo")
if success:
    print("封面下载成功")
```

## NFO 文件要求

工具会从 NFO 文件中读取以下字段：

- **studio**: 工作室名称，必须为 "カリビアンコム" 才会处理
- **originaltitle**: 原始标题，用于提取番号
- **sorttitle**: 排序标题，用于提取番号

### 支持的番号格式

- **Caribbean.com 标准格式**: `123456-789`
- **其他格式**: `ABC-123`、`ABC123` 等

### NFO 文件示例

```xml
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<movie>
    <title>影片标题</title>
    <originaltitle>123456-789 影片原始标题</originaltitle>
    <sorttitle>123456-789</sorttitle>
    <studio>カリビアンコム</studio>
    <plot>影片描述</plot>
</movie>
```

## 下载逻辑

1. **目录遍历**: 递归遍历指定目录下的所有 `.nfo` 文件
2. **工作室检查**: 只处理 `studio` 字段为 "カリビアンコム" 的文件
3. **番号提取**: 从 `originaltitle` 或 `sorttitle` 字段提取番号
4. **重复检查**: 如果目录下已存在 `jacket.jpg`，则跳过下载
5. **图片下载**: 从 Caribbean.com 官网下载封面图片
6. **多格式保存**: 将下载的图片保存为三种格式：
   - `jacket.jpg` (原始下载文件)
   - `folder.jpg` (文件夹封面)
   - `poster.jpg` (海报图片)

## 下载 URL 格式

Caribbean.com 的封面图片 URL 格式：
```
https://www.caribbeancom.com/moviepages/{番号}/images/l_l.jpg
```

例如，番号 `123456-789` 对应的封面 URL：
```
https://www.caribbeancom.com/moviepages/123456-789/images/l_l.jpg
```

## 日志记录

工具会记录以下信息到日志文件：

- 处理的文件路径
- 提取的番号和工作室信息
- 下载成功/失败的详细信息
- 错误信息和异常
- 处理统计信息

### 日志示例

```
[2024-01-15 10:30:15] 开始处理目录: C:\Videos\JAV
[2024-01-15 10:30:16] 处理文件: C:\Videos\JAV\movie1.nfo
[2024-01-15 10:30:16] 提取到番号: 123456-789, 工作室: カリビアンコム
[2024-01-15 10:30:17] 成功下载封面: C:\Videos\JAV\jacket.jpg
[2024-01-15 10:30:17] 复制封面为: C:\Videos\JAV\folder.jpg
[2024-01-15 10:30:17] 复制封面为: C:\Videos\JAV\poster.jpg
```

## 网络路径支持

工具完全支持 Windows UNC 网络路径：

```bash
# 网络路径示例
python jacketdownload.py "\\192.168.1.199\Jav\Caribbean"
python jacketdownload.py "\\server\share\videos"
```

### 网络路径注意事项

1. **网络连接**: 确保网络连接正常
2. **访问权限**: 确保有访问网络共享的权限
3. **路径格式**: 使用标准的 UNC 路径格式
4. **性能考虑**: 网络路径处理可能比本地路径慢

## 错误处理

工具会处理以下常见错误：

- **网络连接错误**: 下载失败时会记录详细错误信息
- **文件权限错误**: 无法写入文件时的错误处理
- **XML 解析错误**: NFO 文件格式错误时的处理
- **路径不存在**: 目录或文件不存在时的处理

## 统计信息

处理完成后，工具会输出详细的统计信息：

- **总 NFO 文件数**: 扫描到的所有 NFO 文件数量
- **Caribbean.com NFO 文件数**: 识别为 Caribbean.com 的文件数量
- **已存在封面的文件数**: 跳过的已有封面的文件数量
- **成功下载封面数**: 成功下载的封面数量
- **下载失败数**: 下载失败的文件数量

## 依赖库

- `requests`: HTTP 请求库
- `xml.etree.ElementTree`: XML 解析 (Python 标准库)
- `os`: 文件系统操作 (Python 标准库)
- `re`: 正则表达式 (Python 标准库)
- `shutil`: 文件操作 (Python 标准库)
- `argparse`: 命令行参数解析 (Python 标准库)

## 安装依赖

```bash
pip install requests
```

## 注意事项

1. **网络连接**: 需要稳定的网络连接访问 Caribbean.com
2. **请求频率**: 工具会在每次下载后等待 1 秒，避免请求过于频繁
3. **文件覆盖**: 如果目标文件已存在，会被覆盖
4. **番号格式**: 确保 NFO 文件中的番号格式正确
5. **工作室名称**: 必须使用日文 "カリビアンコム" 而不是英文 "Caribbean.com"

## 测试

运行测试脚本验证功能：

```bash
python test_jacketdownload.py
```

测试脚本会验证：
- NFO 文件解析功能
- 番号和工作室提取功能
- 目录遍历功能
- 非 Caribbean.com 工作室的跳过逻辑