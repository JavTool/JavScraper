using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Text.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using Image = System.Drawing.Image;
using static JavScraper.App.ImageUtils;
using JavScraper.App.Models;
using JavScraper.App.Services;
using JavScraper.App.Configuration;

namespace JavScraper.App
{
    public partial class FormMain : Form
    {
        public int threadNum { get; set; }          // 进程
        public bool[] threadStatus { get; set; }    // 每个线程结束标志
        public string[] fileNames { get; set; }     // 每个线程接收文件的文件名
        public int[] StartPos { get; set; }     // 每个线程接收文件的起始位置
        public int[] fileSize { get; set; }         // 每个线程接收文件的大小
        public string Url { get; set; }             // 接受文件的URL
        public bool HasMerge { get; set; }           // 文件合并标志
        private string currentImagePath;
        private Dictionary<string, Image> previewImages = new Dictionary<string, Image>();
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isProcessing = false;
        private readonly HttpClient _httpClient = new HttpClient();

        public FormMain()
        {
            InitializeComponent();

            // 设置默认值
            comboBoxCoverType.SelectedIndex = 0;  // 默认选择竖版封面(2:3)
            comboBoxCropMode.SelectedIndex = 0;   // 默认选择右侧
            trackBarWidth.Value = 50;             // 设置宽度调整的默认值为中等

            // 根据裁切方式设置 trackBarScroll 默认值
            switch (comboBoxCropMode.SelectedIndex)
            {
                case 0: // 右侧
                    trackBarScroll.Value = 100;
                    break;
                case 1: // 中间
                    trackBarScroll.Value = 50;
                    break;
                case 2: // 左侧
                    trackBarScroll.Value = 0;
                    break;
            }
            SetupImageCropTab();
            // 加载保存的配置
            LoadConfig();

            // 在窗体关闭时保存配置
            this.FormClosing += FormMain_FormClosing;
        }

        private void FormMain_FormClosing(object? sender, FormClosingEventArgs e)
        {
            try
            {
                SaveConfig();
            }
            catch
            {
                // 忽略保存错误
            }
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <remarks>
        /// 每个线程平均分配文件大小，剩余部分由最后一个线程完成
        /// </remarks>
        /// <param name="filesize"></param>
        private void Init(long filesize)
        {
            threadStatus = new bool[threadNum];
            fileNames = new string[threadNum];
            StartPos = new int[threadNum];
            fileSize = new int[threadNum];
            int filethread = (int)filesize / threadNum;
            int filethreade = filethread + (int)filesize % threadNum;
            for (int i = 0; i < threadNum; i++)
            {
                threadStatus[i] = false;
                fileNames[i] = i.ToString() + ".dat";
                if (i < threadNum - 1)
                {
                    StartPos[i] = filethread * i;
                    fileSize[i] = filethread - 1;
                }
                else
                {
                    StartPos[i] = filethread * i;
                    fileSize[i] = filethreade - 1;
                }
            }
        }
        /// <summary>
        /// 合并文件
        /// </summary>
        public void MergeFile()
        {
            while (true)
            {
                HasMerge = true;
                for (int i = 0; i < threadNum; i++)
                {
                    if (threadStatus[i] == false) // 若有未结束线程，则等待
                    {
                        HasMerge = false;
                        Thread.Sleep(100);
                        break;
                    }
                }
                if (HasMerge == true) // 否则，停止等待
                    break;
            }

            int bufferSize = 512;
            int readSize;
            string downFileNamePath = "";
            byte[] bytes = new byte[bufferSize];
            FileStream fs = new(downFileNamePath, FileMode.Create);
            for (int k = 0; k < threadNum; k++)
            {
                FileStream fsTmp = new FileStream(fileNames[k], FileMode.Open);
                while (true)
                {
                    readSize = fsTmp.Read(bytes, 0, bufferSize);
                    if (readSize > 0)
                        fs.Write(bytes, 0, readSize);
                    else
                        break;
                }
                fsTmp.Close();
            }
            fs.Close();
            MessageBox.Show("接收完毕!!!");
        }

        private void optionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormOption formOption = new FormOption();
            formOption.ShowDialog(this);

        }

        private async void btnStart_Click(object sender, EventArgs e)
        {
            if (_isProcessing)
            {
                MessageBox.Show("已有任务正在处理中");
                return;
            }

            if (!ValidateInputs())
                return;

            try
            {
                _isProcessing = true;
                btnStart.Enabled = false;
                btnStop.Enabled = true;
                btnTest.Enabled = false;

                _cancellationTokenSource = new CancellationTokenSource();
                await ProcessMediaFilesAsync(_cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                AppendLog("任务已取消");
            }
            catch (Exception ex)
            {
                AppendLog($"处理出错: {ex.Message}");
            }
            finally
            {
                _isProcessing = false;
                btnStart.Enabled = true;
                btnStop.Enabled = false;
                btnTest.Enabled = true;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private bool ValidateInputs()
        {
            if (string.IsNullOrEmpty(txtInputFolder.Text))
            {
                MessageBox.Show("请选择输入目录");
                return false;
            }

            if (string.IsNullOrEmpty(txtSuccessfulOutputFolder.Text))
            {
                MessageBox.Show("请选择成功输出目录");
                return false;
            }

            if (!Directory.Exists(txtInputFolder.Text))
            {
                MessageBox.Show("输入目录不存在");
                return false;
            }

            if (!Directory.Exists(txtSuccessfulOutputFolder.Text))
            {
                Directory.CreateDirectory(txtSuccessfulOutputFolder.Text);
            }

            if (!string.IsNullOrEmpty(txtFailedOutputFolder.Text) && !Directory.Exists(txtFailedOutputFolder.Text))
            {
                Directory.CreateDirectory(txtFailedOutputFolder.Text);
            }

            return true;
        }

        private async Task ProcessMediaFilesAsync(CancellationToken cancellationToken)
        {
            var mediaFiles = Directory.GetFiles(txtInputFolder.Text, "*.*", SearchOption.AllDirectories)
                .Where(f => IsMediaFile(f));

            foreach (var file in mediaFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    string fileName = Path.GetFileName(file);
                    AppendLog($"处理文件: {fileName}");

                    // 从文件名提取番号
                    string number = ExtractJavNumber(fileName);
                    if (string.IsNullOrEmpty(number))
                    {
                        AppendLog($"无法从文件提取番号: {fileName}");
                        MoveToFailedDir(file);
                        continue;
                    }

                    AppendLog($"提取番号: {number}");

                    // 获取番号信息
                    var info = await JavOrganization.GetJavInfo(number);
                    if (info == null)
                    {
                        AppendLog($"无法获取番号信息: {number}");
                        MoveToFailedDir(file);
                        continue;
                    }

                    string outputDir = CreateOutputDirectory(info);

                    // 处理文件
                    await ProcessJavFilesAsync(file, info, outputDir);
                    AppendLog($"处理完成: {number}");
                }
                catch (Exception ex)
                {
                    AppendLog($"处理文件出错: {Path.GetFileName(file)}, {ex.Message}");
                    MoveToFailedDir(file);
                }
            }
        }

        private async Task ProcessJavFilesAsync(string sourceFile, JavInfo info, string outputDir)
        {
            // 下载封面
            if (chkDownCover.Checked && !string.IsNullOrEmpty(info.CoverUrl))
            {
                string coverPath = Path.Combine(outputDir, "cover.jpg");
                await DownloadFileAsync(info.CoverUrl, coverPath);
                AppendLog($"下载封面: {info.CoverUrl}");
            }

            // 下载图库
            if (chkDownGallery.Checked && info.GalleryUrls?.Any() == true)
            {
                string galleryDir = Path.Combine(outputDir, "gallery");
                Directory.CreateDirectory(galleryDir);

                for (int i = 0; i < info.GalleryUrls.Count; i++)
                {
                    string imagePath = Path.Combine(galleryDir, $"{i + 1}.jpg");
                    await DownloadFileAsync(info.GalleryUrls[i], imagePath);
                    AppendLog($"下载图片 {i + 1}: {info.GalleryUrls[i]}");
                }
            }

            // 生成NFO文件
            if (chkGenerateNFO.Checked)
            {
                string nfoPath = Path.Combine(outputDir, "info.nfo");
                GenerateNFO(info, nfoPath);
                AppendLog("生成NFO文件");
            }

            // 移动视频文件
            string newFileName = GetOutputFileName(info, Path.GetExtension(sourceFile));
            string newFilePath = Path.Combine(outputDir, newFileName);
            File.Move(sourceFile, newFilePath);
            AppendLog($"移动文件: {newFileName}");
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            _cancellationTokenSource?.Cancel();
        }

        private void btnTest_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBoxNamingRule.Text))
            {
                MessageBox.Show("请输入命名规则");
                return;
            }

            // 创建测试数据
            var testInfo = new JavInfo
            {
                Number = "ABC-123",
                Title = "测试标题",
                Actresses = new List<string> { "演员1", "演员2" },
                ReleaseDate = "2024-01-01",
                Studio = "测试厂商",
                Genres = new List<string> { "类型1", "类型2" }
            };

            // 预览文件名
            string testFileName = GetOutputFileName(testInfo, ".mp4");
            textBoxNamingPreview.Text = testFileName;
        }

        private void SetupImageCropTab()
        {
            // 为整个标签页添加拖放功能
            tabPageCropImage.AllowDrop = true;
            tabPageCropImage.DragEnter += TabPageCropImage_DragEnter;
            tabPageCropImage.DragDrop += TabPageCropImage_DragDrop;

            // 为预览面板添加拖放功能
            previewPanel.AllowDrop = true;
            previewPanel.DragEnter += TabPageCropImage_DragEnter;
            previewPanel.DragDrop += TabPageCropImage_DragDrop;

            // 选择封面类型改变时更新预览
            comboBoxCoverType.SelectedIndexChanged += (s, e) =>
            {
                //// 当选择竖版封面时，自动设置裁切方式为右侧
                //if (comboBoxCoverType.SelectedIndex == 0) // 竖版封面
                //{
                //    comboBoxCropMode.SelectedIndex = 0; // 右侧
                //}

                // 更新预览图
                if (!string.IsNullOrEmpty(currentImagePath))
                {
                    GenerateAllImages(currentImagePath);
                }
            };

            // 当裁切方式改变时更新预览
            comboBoxCropMode.SelectedIndexChanged += (s, e) =>
            {
                // 根据裁切方式设置 trackBarScroll 默认值
                switch (comboBoxCropMode.SelectedIndex)
                {
                    case 0: // 右侧
                        trackBarScroll.Value = 100;
                        break;
                    case 1: // 中间
                        trackBarScroll.Value = 50;
                        break;
                    case 2: // 左侧
                        trackBarScroll.Value = 0;
                        break;
                }

                if (!string.IsNullOrEmpty(currentImagePath))
                {
                    GenerateAllImages(currentImagePath);
                }
            };
        }

        private void TabPageCropImage_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void TabPageCropImage_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0)
            {
                ProcessImageFile(files[0]);
            }
        }

        private void SelectButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "图片文件|*.jpg;*.jpeg;*.webp";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    ProcessImageFile(dialog.FileName);
                }
            }
        }

        private void ProcessImageFile(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLower();
            if (ext == ".jpg" || ext == ".jpeg" || ext == ".webp")
            {
                currentImagePath = filePath;
                GenerateAllImages(filePath);
                EnableCropButton();
            }
            else
            {
                MessageBox.Show("请选择 JPG 或 WebP 格式的图片文件");
            }
        }

        /// <summary>
        /// 将 WebP 文件转换为 JPG 格式
        /// </summary>
        /// <param name="webpPath">WebP 文件路径</param>
        /// <returns>转换后的 JPG 文件路径，如果转换失败返回原路径</returns>
        private string ConvertWebPToJpg(string webpPath)
        {
            try
            {
                string tempDir = Path.Combine(Application.StartupPath, "temp");
                if (!Directory.Exists(tempDir))
                {
                    Directory.CreateDirectory(tempDir);
                }
                string fileName = Path.GetFileNameWithoutExtension(webpPath);
                string jpgPath = Path.Combine(tempDir, $"{fileName}_{DateTime.Now.Ticks}.jpg");

                // 使用 SixLabors.ImageSharp 处理 WebP 格式
                using (var image = SixLabors.ImageSharp.Image.Load(webpPath))
                {
                    image.Save(jpgPath, new JpegEncoder());
                    currentImagePath = jpgPath;
                    return jpgPath;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"WebP 转换失败: {ex.Message}");
                return webpPath;
            }
        }

        private void GenerateAllImages(string imagePath)
        {
            try
            {
                string processPath = imagePath;
                bool isConvertedFile = false;

                // 如果是 WebP 格式，先转换为 JPG
                string ext = Path.GetExtension(imagePath).ToLower();
                if (ext == ".webp")
                {
                    processPath = ConvertWebPToJpg(imagePath);
                    isConvertedFile = !processPath.Equals(imagePath);
                }

                using (var originalImage = Image.FromFile(processPath))
                {
                    // 清理之前的预览图片
                    foreach (var img in previewImages.Values)
                    {
                        img?.Dispose();
                    }
                    previewImages.Clear();

                    // 生成所有预览图
                    previewImages["poster"] = GeneratePosterImage(originalImage);
                    previewImages["folder"] = previewImages["poster"];
                    previewImages["thumb"] = new Bitmap(originalImage);
                    previewImages["backdrop"] = new Bitmap(originalImage);
                    previewImages["fanart"] = new Bitmap(originalImage);

                    // 更新预览
                    pictureBoxPoster.Image?.Dispose();
                    pictureBoxThumb.Image?.Dispose();
                    pictureBoxBackdrop.Image?.Dispose();
                    pictureBoxFanart.Image?.Dispose();

                    pictureBoxPoster.Image = previewImages["poster"];
                    pictureBoxThumb.Image = previewImages["thumb"];
                    pictureBoxBackdrop.Image = previewImages["backdrop"];
                    pictureBoxFanart.Image = previewImages["fanart"];
                }

                //// 清理临时转换的文件
                //if (isConvertedFile && File.Exists(processPath))
                //{
                //    try
                //    {
                //        File.Delete(processPath);
                //    }
                //    catch
                //    {
                //        // 忽略删除临时文件的错误
                //    }
                //}
            }
            catch (Exception ex)
            {
                MessageBox.Show($"生成预览图失败: {ex.Message}\n\n如果是 WebP 格式图片，请尝试转换为 JPG 格式后再使用。");
            }
        }

        private Image GeneratePosterImage(Image originalImage)
        {
            float targetRatio;
            switch (comboBoxCoverType.SelectedIndex)
            {
                case 0: // 竖版封面(2:3)
                    targetRatio = 2f / 3f;
                    break;
                case 1: // 横版封面(16:9)
                    targetRatio = 16f / 9f;
                    break;
                case 2: // 方形封面(1:1)
                    targetRatio = 1f;
                    break;
                default:
                    targetRatio = 2f / 3f;
                    break;
            }

            CropMode cropMode = (CropMode)comboBoxCropMode.SelectedIndex;
            float widthAdjustment = trackBarWidth.Value / 100.0f; // 从 TrackBar 获取宽度调整
            return ImageUtils.CropImage(originalImage, targetRatio, cropMode);
        }

        private void EnableCropButton()
        {
            var cropButton = tabPageCropImage.Controls.OfType<Button>().FirstOrDefault(b => b.Text == "裁切封面");
            if (cropButton != null)
            {
                cropButton.Enabled = true;
            }
        }

        private void CropButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(currentImagePath))
            {
                return;
            }

            try
            {
                string dirPath = Path.GetDirectoryName(currentImagePath);

                List<string> fileNames = new List<string>() { "thumb", "poster", "fanart" };
                // 保存所有预览图片
                foreach (var kvp in previewImages)
                {
                    string fileName = kvp.Key + ".jpg";
                    string filePath = Path.Combine(dirPath, fileName);
                    kvp.Value.Save(filePath, System.Drawing.Imaging.ImageFormat.Jpeg);
                    if (kvp.Key.Contains("fanart"))
                    { 
                        string landscapeFileName =  "landscape.jpg";
                        string landscapePath = Path.Combine(dirPath, landscapeFileName);
                        kvp.Value.Save(landscapePath, System.Drawing.Imaging.ImageFormat.Jpeg);
                    }
                    if (fileNames.Contains(kvp.Key))
                    {
                        string dirName = new DirectoryInfo(Path.GetDirectoryName(currentImagePath))?.Name;
                        string fileNamePath = Path.Combine(dirPath, $"{dirName}-{fileName}");
                        kvp.Value.Save(fileNamePath, System.Drawing.Imaging.ImageFormat.Jpeg);
                    }
                }

                MessageBox.Show("所有图片保存完成!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存图片失败: {ex.Message}");
            }
        }

        private bool IsMediaFile(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLower();
            return ext == ".mp4" || ext == ".mkv" || ext == ".avi" || ext == ".wmv";
        }

        private string ExtractJavNumber(string fileName)
        {
            // 常见的番号格式
            var patterns = new[]
            {
                @"([a-zA-Z]{2,5})-?(\d{3,4})",              // ABC-123, ABC123
                @"([a-zA-Z]{2,5})-?(\d{3,4})-[a-zA-Z]",     // ABC-123-A
                @"([a-zA-Z]{2,5})00(\d{3})",                // ABC00123
                @"([a-zA-Z]{2,5})-?(\d{3,4})([a-zA-Z])",    // ABC-123A, ABC123A
                @"(T28)-?(\d{3,4})",                        // T28-123
                @"k(\d{4})",                                // k1234
                @"n(\d{4})",                                // n1234
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(fileName, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    if (match.Groups.Count == 2)
                    {
                        // 处理 k1234, n1234 格式
                        return $"{match.Groups[0].Value.ToUpper()}";
                    }
                    else
                    {
                        // 标准格式：字母-数字
                        return $"{match.Groups[1].Value.ToUpper()}-{match.Groups[2].Value}";
                    }
                }
            }

            return null;
        }

        private string CreateOutputDirectory(JavInfo info)
        {
            string baseDir = txtSuccessfulOutputFolder.Text;
            string outputDir = Path.Combine(baseDir, info.Number);
            Directory.CreateDirectory(outputDir);
            return outputDir;
        }

        private void MoveToFailedDir(string filePath)
        {
            if (string.IsNullOrEmpty(txtFailedOutputFolder.Text))
                return;

            try
            {
                string fileName = Path.GetFileName(filePath);
                string destPath = Path.Combine(txtFailedOutputFolder.Text, fileName);
                File.Move(filePath, destPath);
                AppendLog($"移动文件到失败目录: {fileName}");
            }
            catch (Exception ex)
            {
                AppendLog($"移动文件失败: {ex.Message}");
            }
        }

        private async Task DownloadFileAsync(string url, string savePath)
        {
            try
            {
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = File.Create(savePath))
                {
                    await stream.CopyToAsync(fileStream);
                }
            }
            catch (Exception ex)
            {
                AppendLog($"下载文件失败: {ex.Message}");
                throw;
            }
        }

        private string GetOutputFileName(JavInfo info, string extension)
        {
            // 使用命名规则模板生成文件名
            string pattern = textBoxNamingRule.Text;
            if (string.IsNullOrEmpty(pattern))
            {
                pattern = "[%number%] %title%"; // 默认命名规则
            }

            string fileName = pattern
                .Replace("%number%", info.Number)
                .Replace("%title%", info.Title)
                .Replace("%actress%", string.Join(",", info.Actresses))
                .Replace("%date%", info.ReleaseDate);

            // 移除文件名中的非法字符
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '_');
            }

            return fileName + extension;
        }


        private void SaveConfig()
        {
            try
            {
                var cfg = new AppConfig
                {
                    InputFolder = txtInputFolder.Text,
                    SuccessfulOutputFolder = txtSuccessfulOutputFolder.Text,
                    FailedOutputFolder = txtFailedOutputFolder.Text,
                    DownCover = chkDownCover.Checked,
                    IsDownloadCover = chkDownCover.Checked,
                    DownGallery = chkDownGallery.Checked,
                    GenerateNFO = chkGenerateNFO.Checked,
                    IsGenNfoFile = chkGenerateNFO.Checked,
                    CoverTypeIndex = comboBoxCoverType.SelectedIndex,
                    CropModeIndex = comboBoxCropMode.SelectedIndex,
                    TrackBarScroll = trackBarScroll.Value,
                    TrackBarWidth = trackBarWidth.Value,
                    NamingRule = textBoxNamingRule.Text,
                    MultipleNamingRule = textBoxMultipleNamingRule.Text,
                };

                ConfigService.Save(cfg);
            }
            catch
            {
                // 忽略保存配置时的错误
            }
        }

        private void LoadConfig()
        {
            try
            {
                var cfg = ConfigService.Load();

                txtInputFolder.Text = cfg.InputFolder ?? string.Empty;
                txtSuccessfulOutputFolder.Text = cfg.SuccessfulOutputFolder ?? string.Empty;
                txtFailedOutputFolder.Text = cfg.FailedOutputFolder ?? string.Empty;
                chkDownCover.Checked = cfg.IsDownloadCover || cfg.DownCover;
                chkDownGallery.Checked = cfg.DownGallery;
                chkGenerateNFO.Checked = cfg.IsGenNfoFile || cfg.GenerateNFO;
                if (cfg.CoverTypeIndex >= 0 && cfg.CoverTypeIndex < comboBoxCoverType.Items.Count)
                    comboBoxCoverType.SelectedIndex = cfg.CoverTypeIndex;
                if (cfg.CropModeIndex >= 0 && cfg.CropModeIndex < comboBoxCropMode.Items.Count)
                    comboBoxCropMode.SelectedIndex = cfg.CropModeIndex;
                trackBarScroll.Value = Math.Max(trackBarScroll.Minimum, Math.Min(trackBarScroll.Maximum, cfg.TrackBarScroll));
                trackBarWidth.Value = Math.Max(trackBarWidth.Minimum, Math.Min(trackBarWidth.Maximum, cfg.TrackBarWidth));
                textBoxNamingRule.Text = cfg.NamingRule ?? string.Empty;
                textBoxMultipleNamingRule.Text = cfg.MultipleNamingRule ?? string.Empty;
            }
            catch
            {
                // 忽略加载配置时的错误
            }
        }

        private void GenerateNFO(JavInfo info, string savePath)
        {
            var doc = new XDocument(
                new XElement("movie",
                    new XElement("title", info.Title),
                    new XElement("originaltitle", info.Number),
                    new XElement("sorttitle", info.Number),
                    new XElement("year", info.ReleaseDate?.Substring(0, 4)),
                    new XElement("premiered", info.ReleaseDate),
                    new XElement("studio", info.Studio),
                    new XElement("genre", info.Genres?.Select(g => new XElement("genre", g))),
                    new XElement("actor", info.Actresses?.Select(a =>
                        new XElement("actor",
                            new XElement("name", a)
                        )
                    ))
                )
            );

            doc.Save(savePath);
        }

        private void AppendLog(string message)
        {
            if (richTextBox1.InvokeRequired)
            {
                richTextBox1.Invoke(new Action(() => AppendLog(message)));
                return;
            }

            richTextBox1.AppendText($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n");
            richTextBox1.ScrollToCaret();
        }

        private async Task TestJavNumberExtraction(string fileName)
        {
            string number = ExtractJavNumber(fileName);
            if (number != null)
            {
                AppendLog($"文件名: {fileName}");
                AppendLog($"提取番号: {number}");

                // 测试 JavBusOrganization 调用
                try
                {
                    var info = await JavOrganization.GetJavInfo(number);
                    if (info != null)
                    {
                        AppendLog($"获取信息成功: {info.Title}");
                    }
                    else
                    {
                        AppendLog("获取信息失败");
                    }
                }
                catch (Exception ex)
                {
                    AppendLog($"调用出错: {ex.Message}");
                }
            }
            else
            {
                AppendLog($"无法从文件名提取番号: {fileName}");
            }
        }

        private void trackBarScroll_Scroll(object sender, EventArgs e)
        {
            //toolStripStatusLabelScroll.Text = $"滚动位置: {trackBarScroll.Value}";

            if (string.IsNullOrEmpty(currentImagePath) || previewImages == null)
                return;

            try
            {
                using (var originalImage = Image.FromFile(currentImagePath))
                {
                    // 获取目标比例
                    float targetRatio;
                    switch (comboBoxCoverType.SelectedIndex)
                    {
                        case 0: targetRatio = 2f / 3f; break;
                        case 1: targetRatio = 16f / 9f; break;
                        case 2: targetRatio = 1f; break;
                        default: targetRatio = 2f / 3f; break;
                    }

                    // 计算最大可移动像素
                    int cropWidth = (int)(originalImage.Height * targetRatio);
                    if (cropWidth > originalImage.Width)
                        cropWidth = originalImage.Width;

                    int maxOffset = originalImage.Width - cropWidth;
                    int offset = 0;
                    if (maxOffset > 0)
                    {
                        // trackBarScroll.Value 范围假设为 0~100
                        offset = (int)(maxOffset * trackBarScroll.Value / 100.0);
                    }

                    // 裁切图片
                    System.Drawing.Rectangle cropRect = new System.Drawing.Rectangle(offset, 0, cropWidth, originalImage.Height);
                    using (Bitmap cropped = new Bitmap(cropRect.Width, cropRect.Height))
                    using (Graphics g = Graphics.FromImage(cropped))
                    {
                        g.DrawImage(originalImage, new System.Drawing.Rectangle(0, 0, cropRect.Width, cropRect.Height), cropRect, GraphicsUnit.Pixel);

                        // 替换预览图
                        pictureBoxPoster.Image?.Dispose();
                        pictureBoxPoster.Image = new Bitmap(cropped);

                        // 更新缓存
                        if (previewImages.ContainsKey("poster"))
                        {
                            previewImages["poster"]?.Dispose();
                            previewImages["poster"] = new Bitmap(cropped);


                        }     // 更新缓存
                        if (previewImages.ContainsKey("folder"))
                        {
                            previewImages["folder"]?.Dispose();
                            previewImages["folder"] = new Bitmap(cropped);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"图片裁切失败: {ex.Message}");
            }
        }

        private void trackBarWidth_Scroll(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(currentImagePath) || previewImages == null)
                return;

            try
            {
                using (var originalImage = Image.FromFile(currentImagePath))
                {
                    // 获取目标比例
                    float targetRatio;
                    switch (comboBoxCoverType.SelectedIndex)
                    {
                        case 0: targetRatio = 2f / 3f; break;
                        case 1: targetRatio = 16f / 9f; break;
                        case 2: targetRatio = 1f; break;
                        default: targetRatio = 2f / 3f; break;
                    }

                    // 获取裁切模式
                    CropMode cropMode = (CropMode)comboBoxCropMode.SelectedIndex;

                    // 将 trackBarWidth.Value (0~100) 转换为 widthAdjustment (0.0~1.0)
                    float widthAdjustment = trackBarWidth.Value / 100.0f;

                    // 使用新的裁切方法
                    using var croppedImage = ImageUtils.CropImage(originalImage, targetRatio, cropMode);
                    // 替换预览图
                    pictureBoxPoster.Image?.Dispose();
                    pictureBoxPoster.Image = new Bitmap(croppedImage);

                    // 更新缓存
                    if (previewImages.ContainsKey("poster"))
                    {
                        previewImages["poster"]?.Dispose();
                        previewImages["poster"] = new Bitmap(croppedImage);
                    }

                    // 更新 folder 缓存
                    if (previewImages.ContainsKey("folder"))
                    {
                        previewImages["folder"]?.Dispose();
                        previewImages["folder"] = new Bitmap(croppedImage);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"图片宽度调整失败: {ex.Message}");
            }
        }
    }
}
