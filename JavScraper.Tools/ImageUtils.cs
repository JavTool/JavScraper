using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace JavScraper.Tools
{
    public class ImageUtils
    {
        /// <summary>
        /// 判断图片是否为纵向长图：
        /// 条件为：高度大于宽度、宽高比约为 0.7、宽度大于 500 像素
        /// </summary>
        /// <param name="imagePath">图片文件的完整路径</param>
        /// <returns>如果符合条件返回 true，否则返回 false</returns>
        public static bool IsVerticalLongImage(string imagePath)
        {
            try
            {
                // 使用 using 语句自动释放图片资源
                using (Image img = Image.FromFile(imagePath))
                {
                    // 获取图片宽度和高度（单位：像素）
                    int width = img.Width;
                    int height = img.Height;

                    // 计算宽高比（宽 ÷ 高）
                    double aspectRatio = (double)width / height;

                    // 设定目标宽高比为 0.7，可接受误差为 ±0.05
                    const double targetRatio = 0.7;
                    const double tolerance = 0.05;

                    // 条件一：高度必须大于宽度（即为纵向图）
                    bool isVertical = height > width;

                    // 条件二：宽高比必须在 0.65 到 0.75 之间（即约为 0.7）
                    bool isRatioClose = Math.Abs(aspectRatio - targetRatio) <= tolerance;

                    // 条件三：宽度必须大于 500 像素
                    bool isWidthEnough = width > 500;

                    // 满足所有条件时，返回 true
                    return isVertical && isRatioClose && isWidthEnough;
                }
            }
            catch (Exception ex)
            {
                // 捕获任何异常（如路径错误、格式不支持等），并打印错误信息
                Console.WriteLine($"读取图片失败: {ex.Message}");

                // 如果发生异常，默认返回 false
                return false;
            }
        }

        public static void ConvertImage(string filename, string destname, ImageFormat format = null)
        {
            Image image = Image.FromFile(filename);
            if (format is null)
            {
                format = ImageFormat.Jpeg;
            }
            image.Save(destname, format);
            image.Dispose();
            //var eps = new EncoderParameters(1);
            //var ep = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 85L);
            //eps.Param[0] = ep;
            //var jpsEncodeer = GetEncoder(ImageFormat.Jpeg);
            ////保存图片
            //imgurl = @"/Content/Upload/" + guid + extension;
            //im.Save(path + imgurl, jpsEncodeer, eps);
            ////释放资源
            //im.Dispose();
            //ep.Dispose();
            //eps.Dispose();
        }

        /// <summary>
        /// 图片裁剪。
        /// </summary>
        /// <param name="sourcePath">源路径。</param>
        /// <param name="destPath">目标路径。</param>
        /// <param name="filename">文件名称。</param>
        public static void CutImage(string sourceFile, string destname, ImageFormat format = null)
        {
            //新图片路径
            string fileExt = Path.GetExtension(sourceFile);

            //string destName = string.Format("{0}/{1}{2}", destPath, filename, fileExt);
            //加载图片
            Image image = Image.FromStream(new MemoryStream(File.ReadAllBytes(sourceFile)));

            int x = 0;
            int y = 0;
            int width = 378;
            int height = 538;

            if (image.Height == height)
            {
                x = image.Width - 378;
            }
            else
            {
                if (image.Width < 800)
                {
                    width = image.Height / 3 * 2;
                }

                height = image.Height;
                x = image.Width - width;
            }

            //定义截取矩形
            Rectangle rectangle = new Rectangle(x, y, width, height);

            // 判断超出的位置否
            if ((image.Width < x + width) || image.Height < y + height)
            {
                image.Dispose();
                return;
            }

            // 定义 Bitmap 对象
            Bitmap bitmap = new Bitmap(image);
            // 进行裁剪
            Bitmap bmpCrop = bitmap.Clone(rectangle, PixelFormat.Format32bppArgb);
            if (format is null)
            {
                format = ImageFormat.Jpeg;
            }

            // 保存成新文件
            bmpCrop.Save(destname, format);
            // 释放对象
            image.Dispose();
            bmpCrop.Dispose();
        }

        /// <summary>
        /// 图片裁剪。
        /// </summary>
        /// <param name="sourcePath">源路径。</param>
        /// <param name="destPath">目标路径。</param>
        /// <param name="filename">文件名称。</param>
        public static void CutImage(string sourceFile, string destPath, string filename, ImageFormat format = null)
        {
            //新图片路径
            string fileExt = Path.GetExtension(sourceFile);

            string destName = string.Format("{0}/{1}{2}", destPath, filename, fileExt);
            //加载图片
            Image image = Image.FromStream(new MemoryStream(File.ReadAllBytes(sourceFile)));

            int x = 0;
            int y = 0;
            int width = 378;
            int height = 538;

            if (image.Height == height)
            {
                x = image.Width - 378;
            }
            else
            {
                if (image.Width < 800)
                {
                    width = image.Height / 3 * 2;
                }

                height = image.Height;
                x = image.Width - width;
            }

            //定义截取矩形
            Rectangle rectangle = new Rectangle(x, y, width, height);

            // 判断超出的位置否
            if ((image.Width < x + width) || image.Height < y + height)
            {
                image.Dispose();
                return;
            }

            // 定义 Bitmap 对象
            Bitmap bitmap = new Bitmap(image);
            // 进行裁剪
            Bitmap bmpCrop = bitmap.Clone(rectangle, PixelFormat.Format32bppArgb);
            if (format is null)
            {
                format = ImageFormat.Jpeg;
            }

            // 保存成新文件
            bmpCrop.Save(destName, format);
            // 释放对象
            image.Dispose();
            bmpCrop.Dispose();
        }

        public enum CropMode
        {
            Right,
            Center,
            Left
        }

        // 缓存常用尺寸模板
        private static readonly ConcurrentDictionary<float, CropTemplate> _ratioCache = new();

        class CropTemplate
        {
            public int Width { get; set; }
            public int Height { get; set; }
            // 其他预计算参数...
        }
        public static string CropImage(string sourcePath, string savePath, float targetRatio, CropMode cropMode = CropMode.Center)
        {
            // 加载图片并创建克隆副本（解除文件锁定）
            using (var source = (Image)Image.FromFile(sourcePath).Clone())
            {
                // 执行核心裁剪逻辑
                using (var croppedImage = CropImage(source, targetRatio, cropMode))
                {
                    // 确保输出目录存在
                    var saveDir = Path.GetDirectoryName(savePath);
                    if (!Directory.Exists(saveDir))
                    {
                        Directory.CreateDirectory(saveDir);
                    }

                    // 根据扩展名选择保存格式
                    croppedImage.Save(savePath, GetImageFormat(savePath));
                    return savePath;
                }
            }
        }

        private static ImageFormat GetImageFormat(string path)
        {
            return Path.GetExtension(path).ToLower() switch
            {
                ".png" => ImageFormat.Png,
                ".bmp" => ImageFormat.Bmp,
                ".gif" => ImageFormat.Gif,
                _ => ImageFormat.Jpeg
            };
        }

        public static Image CropImage(Image source, float targetRatio, CropMode cropMode = CropMode.Center)
        {
            float sourceRatio = (float)source.Width / source.Height;
            int cropWidth, cropHeight;
            int x = 0, y = 0;

            if (sourceRatio > targetRatio)
            {
                // 原图过宽，需要裁剪宽度
                cropHeight = source.Height;
                cropWidth = (int)(cropHeight * targetRatio);

                // 特殊处理：如果原图尺寸为 800x538，裁切尺寸为 378x538
                if (source.Width == 800 && source.Height == 538)
                {
                    cropWidth = 378;
                }

                // 根据裁切模式确定 x 坐标
                switch (cropMode)
                {
                    case CropMode.Left:
                        x = 0;
                        break;
                    case CropMode.Right:
                        x = source.Width - cropWidth;
                        break;
                    default: // Center
                        x = (source.Width - cropWidth) / 2;
                        break;
                }
            }
            else
            {
                // 原图过高，需要裁剪高度
                cropWidth = source.Width;
                cropHeight = (int)(cropWidth / targetRatio);
                y = (source.Height - cropHeight) / 2; // 高度始终居中
            }

            Bitmap target = new Bitmap(cropWidth, cropHeight);
            using (Graphics g = Graphics.FromImage(target))
            {
                g.DrawImage(source,
                    new Rectangle(0, 0, cropWidth, cropHeight),
                    new Rectangle(x, y, cropWidth, cropHeight),
                    GraphicsUnit.Pixel);
            }

            return target;
        }
    }
}
