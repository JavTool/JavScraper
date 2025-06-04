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
using static JavScraper.App.ImageUtils;
using System.Threading;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Xml.Linq;

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

            SetupImageCropTab();
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
                        System.Threading.Thread.Sleep(100);
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
            FileStream fs = new FileStream(downFileNamePath, FileMode.Create);
            FileStream fsTmp = null;

            for (int k = 0; k < threadNum; k++)
            {
                fsTmp = new FileStream(fileNames[k], FileMode.Open);
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
            if (string.IsNullOrEmpty(txtInputDir.Text))
            {
                MessageBox.Show("请选择输入目录");
                return false;
            }

            if (string.IsNullOrEmpty(txtSuccessfulOutputDir.Text))
            {
                MessageBox.Show("请选择成功输出目录");
                return false;
            }

            if (!Directory.Exists(txtInputDir.Text))
            {
                MessageBox.Show("输入目录不存在");
                return false;
            }

            if (!Directory.Exists(txtSuccessfulOutputDir.Text))
            {
                Directory.CreateDirectory(txtSuccessfulOutputDir.Text);
            }

            if (!string.IsNullOrEmpty(txtFailedOutputDir.Text) && !Directory.Exists(txtFailedOutputDir.Text))
            {
                Directory.CreateDirectory(txtFailedOutputDir.Text);
            }

            return true;
        }

        private async Task ProcessMediaFilesAsync(CancellationToken cancellationToken)
        {
            var mediaFiles = Directory.GetFiles(txtInputDir.Text, "*.*", SearchOption.AllDirectories)
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
                dialog.Filter = "图片文件|*.jpg;*.jpeg";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    ProcessImageFile(dialog.FileName);
                }
            }
        }

        private void ProcessImageFile(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLower();
            if (ext == ".jpg" || ext == ".jpeg")
            {
                currentImagePath = filePath;
                GenerateAllImages(filePath);
                EnableCropButton();
            }
            else
            {
                MessageBox.Show("请选择 JPG 格式的图片文件");
            }
        }

        private void GenerateAllImages(string imagePath)
        {
            try
            {
                using (var originalImage = Image.FromFile(imagePath))
                {
                    // 清理之前的预览图片
                    foreach (var img in previewImages.Values)
                    {
                        img?.Dispose();
                    }
                    previewImages.Clear();

                    // 生成所有预览图
                    previewImages["poster"] = GeneratePosterImage(originalImage);
                    previewImages["folder"] = GeneratePosterImage(originalImage);
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
            }
            catch (Exception ex)
            {
                MessageBox.Show($"生成预览图失败: {ex.Message}");
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

                // 保存所有预览图片
                foreach (var kvp in previewImages)
                {
                    string fileName = kvp.Key + ".jpg";
                    string filePath = Path.Combine(dirPath, fileName);
                    kvp.Value.Save(filePath, System.Drawing.Imaging.ImageFormat.Jpeg);
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
            string baseDir = txtSuccessfulOutputDir.Text;
            string outputDir = Path.Combine(baseDir, info.Number);
            Directory.CreateDirectory(outputDir);
            return outputDir;
        }

        private void MoveToFailedDir(string filePath)
        {
            if (string.IsNullOrEmpty(txtFailedOutputDir.Text))
                return;

            try
            {
                string fileName = Path.GetFileName(filePath);
                string destPath = Path.Combine(txtFailedOutputDir.Text, fileName);
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
    }
}
