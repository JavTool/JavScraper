# NFO 文件处理工具

这个工具可以递归遍历指定目录中的 nfo 文件，并按照特定规则修改其中的字段。

## 功能说明

### 处理的字段
- `plot`: 剧情描述
- `title`: 标题
- `originaltitle`: 原始标题
- `sorttitle`: 排序标题
- `javscraperid`: JAV Scraper ID

### 处理规则

1. **繁体转简体**: 对所有字段进行繁体字转简体字转换

2. **sorttitle 格式化**:
   - 如果 `javscraperid` 的值中包含 "-"，就将 `javscraperid` 的值赋值给 `sorttitle`
   - 如果 `sorttitle` 的值不包含 "-"，就在字母和数字之间添加 "-"
   - 特殊处理 "91" 开头的番号：将 "-" 放在最后的字母和数字之间
     - 例如："91CM076" → "91CM-076"

3. **originaltitle 格式化**: 改成 "{sorttitle} {title}" 的格式

4.5. **plot 内容替换**: 将 `plot` 的值改成 `title` 的值

5. **poster 图片处理**: 
   - 自动检测目录下的 poster 图片文件（支持 jpg、png、webp、bmp 格式）
   - 将非 jpg 格式的 poster 图片转换为 poster.jpg
   - 自动将 poster.jpg 复制为 fanart.jpg 和 thumb.jpg

## 使用方法

### 方法一：作为脚本直接运行

```bash
python madouposter.py <目录路径>
```

例如：
```bash
python madouposter.py "C:/Videos/JAV"
```

**支持网络路径：**
```bash
python madouposter.py "\\192.168.1.199\Jav\China\果冻传媒\何苗"
```

### 方法二：在 Python 代码中调用

```python
from madouposter import process_nfo_files

# 处理本地目录中的所有 nfo 文件
process_nfo_files("C:/Videos/JAV")

# 处理网络路径中的所有 nfo 文件
process_nfo_files(r"\\192.168.1.199\Jav\China\果冻传媒\何苗")
```

### 方法三：处理单个文件

```python
from madouposter import process_single_nfo_file

# 处理单个 nfo 文件
process_single_nfo_file("C:/Videos/JAV/movie.nfo")
```

## 处理示例

### 示例 1：91 开头的番号

**处理前：**
```xml
<movie>
    <plot>測試劇情</plot>
    <title>測試標題</title>
    <originaltitle>測試原標題</originaltitle>
    <sorttitle>91CM076</sorttitle>
    <javscraperid>91CM076</javscraperid>
</movie>
```

**处理后：**
```xml
<movie>
    <plot>测试标题</plot>
    <title>测试标题</title>
    <originaltitle>91CM-076 测试标题</originaltitle>
    <sorttitle>91CM-076</sorttitle>
    <javscraperid>91CM076</javscraperid>
</movie>
```

### 示例 2：javscraperid 包含连字符

**处理前：**
```xml
<movie>
    <plot>測試劇情</plot>
    <title>測試標題</title>
    <originaltitle>測試原標題</originaltitle>
    <sorttitle>ABC123</sorttitle>
    <javscraperid>ABC-123</javscraperid>
</movie>
```

**处理后：**
```xml
<movie>
    <plot>测试标题</plot>
    <title>测试标题</title>
    <originaltitle>ABC-123 测试标题</originaltitle>
    <sorttitle>ABC-123</sorttitle>
    <javscraperid>ABC-123</javscraperid>
</movie>
```

### 示例 3：poster 图片处理功能

**场景 1：PNG 格式转换**

处理前的目录结构：
```
/path/to/videos/
├── movie1.nfo
├── poster.png    # PNG 格式的 poster 图片
└── movie1.mp4
```

处理后的目录结构：
```
/path/to/videos/
├── movie1.nfo
├── poster.jpg    # 转换后的 JPG 格式
├── fanart.jpg    # 从 poster.jpg 复制
├── thumb.jpg     # 从 poster.jpg 复制
└── movie1.mp4
```

**场景 2：JPG 格式（无需转换）**

处理前的目录结构：
```
/path/to/videos/
├── movie2.nfo
├── poster.jpg    # 已经是 JPG 格式
└── movie2.mp4
```

处理后的目录结构：
```
/path/to/videos/
├── movie2.nfo
├── poster.jpg    # 保持不变
├── fanart.jpg    # 从 poster.jpg 复制
├── thumb.jpg     # 从 poster.jpg 复制
└── movie2.mp4
```

## 网络路径支持

工具完全支持 Windows UNC 网络路径，可以直接处理网络共享文件夹中的 nfo 文件。

### 网络路径格式
- **标准格式**: `\\服务器IP\共享文件夹\路径`
- **示例**: `\\192.168.1.199\Jav\China\果冻传媒\何苗`

### 网络路径使用注意事项
1. **网络连接**: 确保网络连接正常，能够访问目标服务器
2. **权限验证**: 确保当前用户有访问网络共享文件夹的权限
3. **路径格式**: 在 Python 代码中使用原始字符串 `r"\\server\path"` 或转义 `"\\\\server\\path"`
4. **性能考虑**: 网络路径处理速度可能比本地路径慢，取决于网络状况
5. **错误处理**: 如果网络中断，程序会显示相应错误信息

## 注意事项

1. 工具会递归处理指定目录及其所有子目录中的 `.nfo` 文件
2. 处理过程中会直接修改原文件，建议先备份重要数据
3. 如果 XML 文件格式有问题，会跳过该文件并显示错误信息
4. 需要安装 `zhconv` 库用于繁简转换：`pip install zhconv`
5. 图片格式转换功能依赖 `Pillow` 库，请确保已正确安装：`pip install Pillow`
6. **poster 图片处理说明**：
   - 支持的图片格式：jpg、jpeg、png、webp、bmp
   - 非 jpg 格式会自动转换为 jpg 格式，原文件会被删除（jpeg 格式除外）
   - 转换时会自动处理透明背景（转换为白色背景）
   - 如果 `fanart.jpg` 或 `thumb.jpg` 文件已存在，会被覆盖
   - 如果没有找到 poster 图片文件，会输出提示信息但不影响其他字段的处理
7. 支持本地路径和网络路径（UNC 路径）

## 依赖库

- `xml.etree.ElementTree`: XML 解析（Python 标准库）
- `os`: 文件系统操作（Python 标准库）
- `re`: 正则表达式（Python 标准库）
- `zhconv`: 繁简转换

## 测试

运行测试脚本验证功能：

```bash
python test_nfo_processing.py
```

测试脚本会创建临时的测试文件，验证各种处理规则是否正确工作。

### 网络路径测试

使用专门的网络路径示例脚本：

```bash
# 预览网络路径内容
python network_path_example.py "\\\\192.168.1.199\\Jav\\China\\果冻传媒\\何苗" --preview-only

# 试运行模式（不实际修改文件）
python network_path_example.py "\\\\192.168.1.199\\Jav\\China\\果冻传媒\\何苗" --dry-run

# 实际处理网络路径中的文件
python network_path_example.py "\\\\192.168.1.199\\Jav\\China\\果冻传媒\\何苗"
```

### 图片格式转换测试

使用专门的图片转换测试脚本：

```bash
# 测试图片格式转换功能
python test_image_conversion.py
```

该脚本会：
- 创建不同格式的测试图片（PNG、WebP、JPG）
- 测试格式转换功能（PNG/WebP → JPG）
- 验证 fanart.jpg 和 thumb.jpg 的复制功能
- 检查透明背景处理（PNG 格式）
- 验证文件大小一致性