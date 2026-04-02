using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using Image = System.Drawing.Image;

namespace JavScraper.App
{
    public partial class ImageCropForm: Form
    {

        private string currentImagePath;

        public ImageCropForm()
        {
            InitComponent();
            InitializeComponent();
            SetupDragDrop();
        }

        private void InitComponent()
        {
            //this.Text = "封面裁切工具";
            //this.Size = new Size(800, 600);

            //// 创建拖拽区域面板
            //Panel dropPanel = new Panel
            //{
            //    BorderStyle = BorderStyle.None,
            //    Size = new Size(600, 400),
            //    AllowDrop = true,
            //    Dock = DockStyle.Fill
            //};

            //Label dropLabel = new Label
            //{
            //    Text = "将图片文件拖拽到此处\n或点击选择文件",
            //    TextAlign = ContentAlignment.MiddleCenter,
            //    AutoSize = false,
            //    Dock = DockStyle.Fill
            //};

            //Button selectButton = new Button
            //{
            //    Text = "选择文件",
            //    Dock = DockStyle.Bottom
            //};

            //Button cropButton = new Button
            //{
            //    Text = "裁切封面",
            //    Enabled = false,
            //    Dock = DockStyle.Bottom
            //};

            //PictureBox previewBox = new PictureBox
            //{
            //    SizeMode = PictureBoxSizeMode.Zoom,
            //    Dock = DockStyle.Fill
            //};

            //// 添加控件
            //dropPanel.Controls.Add(dropLabel);
            //this.Controls.Add(dropPanel);
            //this.Controls.Add(selectButton);
            //this.Controls.Add(cropButton);
            //this.Controls.Add(previewBox);

            //// 绑定事件
            //selectButton.Click += SelectButton_Click;
            //cropButton.Click += CropButton_Click;
        }

        private void SetupDragDrop()
        {
            this.AllowDrop = true;
            this.DragEnter += Form_DragEnter;
            this.DragDrop += Form_DragDrop;
        }

        private void Form_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void Form_DragDrop(object sender, DragEventArgs e)
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
                // 显示预览
                ShowPreview(filePath);
                // 启用裁切按钮
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
                string tempDir = Path.GetTempPath();
                string fileName = Path.GetFileNameWithoutExtension(webpPath);
                string jpgPath = Path.Combine(tempDir, $"{fileName}_{DateTime.Now.Ticks}.jpg");

                // 使用 SixLabors.ImageSharp 处理 WebP 格式
                using (var image = SixLabors.ImageSharp.Image.Load(webpPath))
                {
                    image.Save(jpgPath, new JpegEncoder());
                    return jpgPath;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"WebP 转换失败: {ex.Message}");
                return webpPath;
            }
        }

        private void ShowPreview(string imagePath)
        {
            try
            {
                var pictureBox = Controls.OfType<PictureBox>().FirstOrDefault();
                if (pictureBox != null)
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

                    using (var image = Image.FromFile(processPath))
                    {
                        pictureBox.Image = new Bitmap(image);
                    }

                    // 清理临时转换的文件
                    if (isConvertedFile && File.Exists(processPath))
                    {
                        try
                        {
                            File.Delete(processPath);
                        }
                        catch
                        {
                            // 忽略删除临时文件的错误
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载图片失败: {ex.Message}\n\n如果是 WebP 格式图片，请尝试转换为 JPG 格式后再使用。");
            }
        }

        private void EnableCropButton()
        {
            var cropButton = Controls.OfType<Button>().FirstOrDefault(b => b.Text == "裁切封面");
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
                string processPath = currentImagePath;
                bool isConvertedFile = false;
                
                // 如果是 WebP 格式，先转换为 JPG
                string ext = Path.GetExtension(currentImagePath).ToLower();
                if (ext == ".webp")
                {
                    processPath = ConvertWebPToJpg(currentImagePath);
                    isConvertedFile = !processPath.Equals(currentImagePath);
                    
                    if (!isConvertedFile)
                    {
                        // 转换失败，直接返回
                        return;
                    }
                }

                string destName = "poster";
                string dirPath = Path.GetDirectoryName(currentImagePath);

                ImageUtils.CutImage(processPath, dirPath, destName);
                MessageBox.Show("裁切完成!");
                
                // 清理临时转换的文件
                if (isConvertedFile && File.Exists(processPath))
                {
                    try
                    {
                        File.Delete(processPath);
                    }
                    catch
                    {
                        // 忽略删除临时文件的错误
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"裁切失败: {ex.Message}\n\n如果是 WebP 格式图片，请尝试转换为 JPG 格式后再使用。");
            }
        }
    }
}
