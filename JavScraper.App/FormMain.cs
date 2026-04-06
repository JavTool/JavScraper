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
using Microsoft.Extensions.Logging;
using static JavScraper.App.ImageUtils;
using JavScraper.App.Models;
using JavScraper.App.Services;
using JavScraper.App.Configuration;

namespace JavScraper.App
{
    public partial class FormMain : Form
    {
        public int ThreadNum { get; set; }          // 进程
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
        private readonly HttpClient _httpClient = new();
        private readonly ILogger _logger;

        /// <summary>
        /// 窗体构造函数，负责初始化控件、设置默认选项并加载用户配置。
        /// </summary>
        public FormMain()
        {
            // 创建当前窗体的 logger
            _logger = AppLogging.Factory.CreateLogger<FormMain>();
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

        /// <summary>
        /// 窗体关闭事件处理器。尝试保存当前用户配置到磁盘。
        /// </summary>
        /// <param name="sender">触发事件的对象。</param>
        /// <param name="e">包含事件数据的 <see cref="FormClosingEventArgs"/> 实例。</param>
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
        /// 初始化用于多线程下载的内部数组与分区信息。
        /// </summary>
        /// <remarks>
        /// 将总文件大小在 <see cref="ThreadNum"/> 个线程之间平均分配，余数分配给最后一个线程。
        /// </remarks>
        /// <param name="filesize">要下载的文件总大小（字节）。</param>
        private void Init(long filesize)
        {
            threadStatus = new bool[ThreadNum];
            fileNames = new string[ThreadNum];
            StartPos = new int[ThreadNum];
            fileSize = new int[ThreadNum];
            int filethread = (int)filesize / ThreadNum;
            int filethreade = filethread + (int)filesize % ThreadNum;
            for (int i = 0; i < ThreadNum; i++)
            {
                threadStatus[i] = false;
                fileNames[i] = i.ToString() + ".dat";
                if (i < ThreadNum - 1)
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
        /// 将各线程下载得到的分块文件按顺序合并为单个目标文件。
        /// </summary>
        /// <remarks>
        /// 会等待所有线程完成（通过检查 <see cref="threadStatus"/>）后再进行合并。
        /// </remarks>
        public void MergeFile()
        {
            while (true)
            {
                HasMerge = true;
                for (int i = 0; i < ThreadNum; i++)
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
            for (int k = 0; k < ThreadNum; k++)
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

        /// <summary>
        /// 选项菜单项点击处理器，显示选项对话框。
        /// </summary>
        /// <param name="sender">触发事件的对象。</param>
        /// <param name="e">事件参数。</param>
        private void OptionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormOption formOption = new FormOption();
            formOption.ShowDialog(this);

        }

        /// <summary>
        /// “开始”按钮点击事件处理器，开始异步处理输入目录下的媒体文件。
        /// </summary>
        /// <param name="sender">触发事件的对象（开始按钮）。</param>
        /// <param name="e">事件参数。</param>
        private async void BtnStart_Click(object sender, EventArgs e)
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

        /// <summary>
        /// 验证输入和输出路径等必要设置是否有效，并在需要时创建缺失的输出目录。
        /// </summary>
        /// <returns>如果输入有效则返回 true；否则返回 false。</returns>
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

        /// <summary>
        /// 遍历并处理输入目录下的所有媒体文件（递归），对每个文件执行番号提取、信息查询与后续处理。
        /// </summary>
        /// <param name="cancellationToken">用于取消操作的令牌。</param>
        /// <returns>表示异步操作的任务。</returns>
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
                    var info = await JavOrganizationService.GetJavInfo(number);
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

        /// <summary>
        /// 处理单个资源文件的异步方法：根据配置下载封面/图库、生成 NFO，并将视频文件移动到目标目录。
        /// </summary>
        /// <param name="sourceFile">源视频文件完整路径。</param>
        /// <param name="info">与该视频关联的元数据信息。</param>
        /// <param name="outputDir">处理后文件保存的目标目录。</param>
        /// <returns>表示异步操作的任务。</returns>
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

        /// <summary>
        /// “停止”按钮点击事件处理器，请求取消当前正在执行的处理任务。
        /// </summary>
        /// <param name="sender">触发事件的对象（停止按钮）。</param>
        /// <param name="e">事件参数。</param>
        private void BtnStop_Click(object sender, EventArgs e)
        {
            _cancellationTokenSource?.Cancel();
        }

        /// <summary>
        /// “测试”按钮点击事件处理器：使用示例数据生成并显示命名规则预览。
        /// </summary>
        /// <param name="sender">触发事件的对象（测试按钮）。</param>
        /// <param name="e">事件参数。</param>
        private void BtnTest_Click(object sender, EventArgs e)
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

        /// <summary>
        /// 初始化裁切图片选项卡的拖放与控件事件绑定，例如启用拖放并处理封面类型/裁切方式变更。
        /// </summary>
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

        /// <summary>
        /// 处理图片裁切页的拖放进入事件，判断拖放数据是否包含文件并设置拖放效果。
        /// </summary>
        /// <param name="sender">接收拖放的控件。</param>
        /// <param name="e">拖放事件参数。</param>
        private void TabPageCropImage_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        /// <summary>
        /// 处理图片裁切页的拖放释放事件，从拖放数据中取出文件并处理第一个文件。
        /// </summary>
        /// <param name="sender">接收拖放的控件。</param>
        /// <param name="e">拖放事件参数，包含文件列表。</param>
        private void TabPageCropImage_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0)
            {
                ProcessImageFile(files[0]);
            }
        }

        /// <summary>
        /// 选择图片按钮点击事件：打开文件对话框以选择图片并交由处理方法加载预览。
        /// </summary>
        /// <param name="sender">触发事件的对象（选择按钮）。</param>
        /// <param name="e">事件参数。</param>
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

        /// <summary>
        /// 处理选定的图片文件：验证扩展名、加载当前图片路径并生成所有预览图。
        /// </summary>
        /// <param name="filePath">要处理的图片文件路径。</param>
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
        /// 将 WebP 文件转换为 JPG 格式并保存到临时目录。
        /// </summary>
        /// <param name="webpPath">要转换的 WebP 文件路径。</param>
        /// <returns>返回转换后生成的 JPG 文件路径；若转换失败则返回原传入路径。</returns>
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

        /// <summary>
        /// 生成所有预览图（poster、folder、thumb、backdrop、fanart）并更新界面预览控件。
        /// </summary>
        /// <param name="imagePath">源图片文件路径。</param>
        /// <summary>
        /// 从指定图片路径生成所有需要的预览图（poster/folder/thumb/backdrop/fanart）并更新 UI。
        /// </summary>
        /// <param name="imagePath">源图片文件路径（支持 JPG、WebP）。</param>
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

                using var originalImage = Image.FromFile(processPath);
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

        /// <summary>
        /// 根据当前选定的封面类型和裁切模式从原图生成海报（poster）图片。
        /// </summary>
        /// <param name="originalImage">原始图片对象。</param>
        /// <returns>生成的海报图片对象。</returns>
        /// <summary>
        /// 根据当前封面类型与裁切模式，从原始图片生成用于海报显示的裁切图像。
        /// </summary>
        /// <param name="originalImage">原始图片对象。</param>
        /// <returns>裁切后的海报图片对象。</returns>
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

        /// <summary>
        /// 启用裁切按钮（当已加载合法图片时调用）。
        /// </summary>
        /// <summary>
        /// 启用裁切操作按钮（当已经加载并生成预览图时调用）。
        /// </summary>
        private void EnableCropButton()
        {
            var cropButton = tabPageCropImage.Controls.OfType<Button>().FirstOrDefault(b => b.Text == "裁切封面");
            if (cropButton != null)
            {
                cropButton.Enabled = true;
            }
        }

        /// <summary>
        /// 裁切按钮点击处理器：将当前预览图保存到图片所在目录，并生成附加的命名副本（例如 landscape、目录名-前缀等）。
        /// </summary>
        /// <param name="sender">触发事件的对象（裁切按钮）。</param>
        /// <param name="e">事件参数。</param>
        /// <summary>
        /// 裁切并保存所有预览图片的处理器，同时会按需要生成额外的文件名拷贝（如 landscape.jpg 和 目录名-前缀文件）。
        /// </summary>
        /// <param name="sender">触发事件的对象（裁切按钮）。</param>
        /// <param name="e">事件参数。</param>
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

        /// <summary>
        /// 判断指定文件是否为受支持的媒体文件（根据扩展名判定）。
        /// </summary>
        /// <param name="filePath">要检查的文件路径。</param>
        /// <returns>若为支持的媒体文件，则返回 true；否则返回 false。</returns>
        private bool IsMediaFile(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLower();
            return ext == ".mp4" || ext == ".mkv" || ext == ".avi" || ext == ".wmv";
        }

        /// <summary>
        /// 从文件名中提取常见的番号格式（如 ABC-123、ABC123、k1234 等），并返回标准化的番号字符串。
        /// </summary>
        /// <param name="fileName">要解析的文件名或路径。</param>
        /// <returns>匹配成功则返回标准化番号（例如 "ABC-123"），否则返回 null。</returns>
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

        /// <summary>
        /// 根据提供的 JavInfo 在成功输出目录下创建并返回一个以番号命名的子目录。
        /// </summary>
        /// <param name="info">用于生成目录名的元数据信息。</param>
        /// <returns>创建（或已存在）的输出目录完整路径。</returns>
        private string CreateOutputDirectory(JavInfo info)
        {
            string baseDir = txtSuccessfulOutputFolder.Text;
            string outputDir = Path.Combine(baseDir, info.Number);
            Directory.CreateDirectory(outputDir);
            return outputDir;
        }

        /// <summary>
        /// 将处理失败的文件移动到用户配置的失败输出目录，便于后续查看与处理。
        /// </summary>
        /// <param name="filePath">要移动的文件路径。</param>
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

        /// <summary>
        /// 异步下载指定 URL 的文件并保存到给定路径，失败时记录日志并重新抛出异常。
        /// </summary>
        /// <param name="url">要下载的远程文件地址。</param>
        /// <param name="savePath">保存到本地的完整路径。</param>
        /// <returns>表示异步下载操作的任务。</returns>
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
                _logger?.LogError(ex, "下载文件失败: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// 根据用户在界面中配置的命名规则模板与 JavInfo 元数据生成输出文件名，并移除非法文件名字符。
        /// </summary>
        /// <param name="info">用于填充模板的元数据信息。</param>
        /// <param name="extension">要追加的文件扩展名（包含点）。</param>
        /// <returns>生成的文件名（包含扩展名）。</returns>
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


        /// <summary>
        /// 将当前界面设置封装成配置对象并保存到磁盘（通过 ConfigService）。
        /// </summary>
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

        /// <summary>
        /// 从磁盘加载配置并将值应用到界面控件，保证控件值在有效范围内。
        /// </summary>
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

        /// <summary>
        /// 根据 JavInfo 元数据生成一个标准的 NFO（XML）文件并保存到指定路径。
        /// </summary>
        /// <param name="info">用于生成 NFO 的元数据信息。</param>
        /// <param name="savePath">NFO 文件的目标保存路径。</param>
        private static void GenerateNFO(JavInfo info, string savePath)
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

        /// <summary>
        /// 将消息追加到界面日志控件（richTextBox1），并在需要时切换到 UI 线程执行，保证线程安全。
        /// </summary>
        /// <param name="message">要追加的日志消息。</param>
        private void AppendLog(string message)
        {
            try
            {
                _logger?.LogInformation(message);
            }
            catch
            {
                // ignore logging failures
            }
            if (richTextBox1.InvokeRequired)
            {
                richTextBox1.Invoke(new Action(() => AppendLog(message)));
                return;
            }

            richTextBox1.AppendText($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n");
            richTextBox1.ScrollToCaret();
        }

        /// <summary>
        /// 测试从给定文件名提取番号并调用 JavOrganization 查询信息，用于调试和验证提取规则。
        /// </summary>
        /// <param name="fileName">要用于测试的文件名。</param>
        /// <returns>表示异步测试操作的任务。</returns>
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
                    var info = await JavOrganizationService.GetJavInfo(number);
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

        /// <summary>
        /// 滚动条（水平偏移）变化处理器：根据当前封面类型计算裁切区域的偏移并更新海报预览。
        /// </summary>
        /// <param name="sender">触发事件的滚动条控件。</param>
        /// <param name="e">事件参数。</param>
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

        /// <summary>
        /// 宽度调整滚动条变化处理器：根据当前设置重新裁切海报并更新预览及缓存。
        /// </summary>
        /// <param name="sender">触发事件的滚动条控件。</param>
        /// <param name="e">事件参数。</param>
        private void TrackBarWidth_Scroll(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(currentImagePath) || previewImages == null)
                return;

            try
            {
                using var originalImage = Image.FromFile(currentImagePath);
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
            catch (Exception ex)
            {
                MessageBox.Show($"图片宽度调整失败: {ex.Message}");
            }
        }
    }
}
