using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace JavScraper.Tools
{
    /// <summary>
    /// 图像处理工具类（基于 SixLabors.ImageSharp）。
    /// 本类提供一组常用的图像处理方法，包含：
    /// - 方向检测（横向/纵向长图判断）
    /// - 按项目约定保存封面变体（thumb/fanart/poster）
    /// - 图片格式转换（PNG/JPEG）
    /// - 海报裁切（CutImage）与通用裁剪（CropImage）
    ///
    /// 所有方法均使用 ImageSharp 进行解码/编码，保证在非 Windows 平台上的兼容性。
    /// 出于健壮性考虑，绝大多数方法在遇到异常时会返回默认值（例如返回 false 或空字符串），
    /// 以避免在批量处理流程中抛出异常导致任务中断。
    /// </summary>
    public class ImageUtils
    {
        /// <summary>
        /// 判断指定路径的图片是否为“纵向长图”。
        /// </summary>
        /// <param name="imagePath">图片文件的完整路径。</param>
        /// <returns>
        /// 如果图片满足以下条件则返回 true：
        /// 1. 高度大于宽度（纵向）；
        /// 2. 宽高比接近 0.7（允许 ±0.05 的误差）；
        /// 3. 宽度大于 500 像素。
        /// 在文件无法读取或解析时返回 false。
        /// </returns>
        /// <remarks>
        /// 该方法用于区分用于海报（poster）的纵向长图与用于横向展示的 backdrop/thumb。
        /// 使用 ImageSharp 读取图像元数据，因此会加载图像头信息以获取宽高。
        /// </remarks>
        public static bool IsVerticalLongImage(string imagePath)
        {
            try
            {
                using var img = Image.Load(imagePath);
                int width = img.Width;
                int height = img.Height;
                double aspectRatio = (double)width / height;
                const double targetRatio = 0.7;
                const double tolerance = 0.05;

                bool isVertical = height > width;
                bool isRatioClose = Math.Abs(aspectRatio - targetRatio) <= tolerance;
                bool isWidthEnough = width > 500;

                return isVertical && isRatioClose && isWidthEnough;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 判断图片是否为横向（宽 > 高）。
        /// 提供 Stream 与 文件路径 两个重载，失败时返回 false。
        /// </summary>
        /// <summary>
        /// 判断给定图像流表示的图片是否为横向（宽 &gt; 高）。
        /// </summary>
        /// <param name="imageStream">包含图像数据的流（方法会在可寻址时将流位置重置到起点）。</param>
        /// <returns>如果图片宽度大于高度则返回 true；当流无法读取或解析失败时返回 false。</returns>
        /// <remarks>
        /// 该重载允许在无需先写入磁盘的情况下检测图片方向，适用于从 HTTP 响应流直接判断场景。
        /// </remarks>
        public static bool IsLandscape(Stream imageStream)
        {
            try
            {
                if (imageStream.CanSeek)
                    imageStream.Seek(0, SeekOrigin.Begin);

                using var img = Image.Load(imageStream);
                return img.Width > img.Height;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 判断图片是否为横向（宽 > 高）。
        /// 提供 Stream 与 文件路径 两个重载，失败时返回 false。
        /// </summary>
        /// <summary>
        /// 判断指定路径的图片是否为横向（宽 &gt; 高）。
        /// </summary>
        /// <param name="imagePath">图片文件路径。</param>
        /// <returns>如果图片宽度大于高度则返回 true；解析失败或异常时返回 false。</returns>
        /// <remarks>该方法为 IsLandscape(Stream) 的文件路径便利重载。</remarks>
        public static bool IsLandscape(string imagePath)
        {
            try
            {
                using var img = Image.Load(imagePath);
                return img.Width > img.Height;
            }
            catch
            {
                return false;
            }
        }
        /// <summary>
        /// 根据图片方向保存封面的多个命名变体。
        /// </summary>
        /// <param name="imageStream">包含封面图片数据的流（方法不会关闭调用方传入的流）。</param>
        /// <param name="savePath">目标目录路径；如果目录不存在会尝试创建。</param>
        /// <param name="baseName">用于构造文件名的基础名称（不含扩展名）。
        /// 例如若 baseName 为 "abc"，则会生成 "abc-thumb.jpg" 或 "abc-poster.jpg" 等。</param>
        /// <returns>
        /// 返回第一个已存在或成功写入的文件的完整路径；若所有写入均失败则返回空字符串。
        /// </returns>
        /// <remarks>
        /// 保存规则：
        /// - 若图片为横向（宽 &gt; 高），保存顺序为：
        ///   "{baseName}-thumb.jpg", "{baseName}-fanart.jpg", "thumb.jpg", "fanart.jpg"。
        /// - 若图片为纵向（高 &gt; 宽），保存顺序为：
        ///   "{baseName}-poster.jpg", "poster.jpg"。
        /// - 在尝试写入之前会先检查目标文件是否已存在；若存在则直接返回该路径并跳过写入，避免重复下载/覆盖。
        /// - 输出编码为 JPEG（质量 100）。
        /// - 单个文件写入出错不会中断其它变体的写入，方法在完成后返回第一个成功或已存在的文件路径。
        /// </remarks>
        public static string SaveCoverVariants(string imagePath, string savePath, string baseName, List<string> saveNames)
        {
            try
            {
                if (!Directory.Exists(savePath))
                    Directory.CreateDirectory(savePath);

                using var image = Image.Load(imagePath);

                // 如果任一目标已存在，直接返回它
                //foreach (var name in saveNames)
                //{
                //    var existing = Path.Combine(savePath, name);
                //    if (File.Exists(existing))
                //        return existing;
                //}

                string firstSaved = string.Empty;
                foreach (var name in saveNames)
                {
                    try
                    {
                        var outPath = Path.Combine(savePath, name);
                        // save as jpeg
                        image.SaveAsJpeg(outPath, new JpegEncoder { Quality = 100 });
                        if (string.IsNullOrEmpty(firstSaved))
                            firstSaved = outPath;
                    }
                    catch { }
                }

                return firstSaved;
            }
            catch
            {
                return string.Empty;
            }
        }
        /// <summary>
        /// 根据图片方向保存封面的多个命名变体。
        /// </summary>
        /// <param name="imageStream">包含封面图片数据的流（方法不会关闭调用方传入的流）。</param>
        /// <param name="savePath">目标目录路径；如果目录不存在会尝试创建。</param>
        /// <param name="baseName">用于构造文件名的基础名称（不含扩展名）。
        /// 例如若 baseName 为 "abc"，则会生成 "abc-thumb.jpg" 或 "abc-poster.jpg" 等。</param>
        /// <returns>
        /// 返回第一个已存在或成功写入的文件的完整路径；若所有写入均失败则返回空字符串。
        /// </returns>
        /// <remarks>
        /// 保存规则：
        /// - 若图片为横向（宽 &gt; 高），保存顺序为：
        ///   "{baseName}-thumb.jpg", "{baseName}-fanart.jpg", "thumb.jpg", "fanart.jpg"。
        /// - 若图片为纵向（高 &gt; 宽），保存顺序为：
        ///   "{baseName}-poster.jpg", "poster.jpg"。
        /// - 在尝试写入之前会先检查目标文件是否已存在；若存在则直接返回该路径并跳过写入，避免重复下载/覆盖。
        /// - 输出编码为 JPEG（质量 100）。
        /// - 单个文件写入出错不会中断其它变体的写入，方法在完成后返回第一个成功或已存在的文件路径。
        /// </remarks>
        public static string SaveCover(Stream imageStream, string savePath, string fileName)
        {
            try
            {
                if (!Directory.Exists(savePath))
                    Directory.CreateDirectory(savePath);

                if (imageStream.CanSeek)
                    imageStream.Seek(0, SeekOrigin.Begin);

                using var image = Image.Load(imageStream);

                string firstSaved = string.Empty;

                try
                {
                    var outPath = Path.Combine(savePath, fileName);
                    // save as jpeg
                    image.SaveAsJpeg(outPath, new JpegEncoder { Quality = 100 });
                    if (string.IsNullOrEmpty(firstSaved))
                        firstSaved = outPath;
                }
                catch { }


                return firstSaved;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 将图片文件转换为目标格式并保存到指定路径。
        /// </summary>
        /// <param name="filename">源图片路径。</param>
        /// <param name="destname">目标保存路径（包含文件名及扩展名）。</param>
        /// <param name="format">可选的格式提示（例如 "png"）；如果为 null，将使用 destname 的扩展名决定编码格式。</param>
        /// <remarks>
        /// 支持 PNG 与 JPEG 两种输出格式。JPEG 的默认质量为 90（可根据需要调整）。
        /// 使用 ImageSharp 解码与编码以保证跨平台兼容性。
        /// </remarks>
        public static void ConvertImage(string filename, string destname, string format = null)
        {
            using var image = SixLabors.ImageSharp.Image.Load(filename);
            var ext = (format ?? Path.GetExtension(destname)).ToLower();
            if (ext.StartsWith('.')) ext = ext.Substring(1);
            if (ext == "png")
                image.SaveAsPng(destname);
            else
                image.SaveAsJpeg(destname, new JpegEncoder { Quality = 90 });
        }

        /// <summary>
        /// 对源图片执行快速裁切以生成海报（poster）版本并保存。
        /// </summary>
        /// <param name="sourceFile">源图片文件路径。</param>
        /// <param name="destname">目标保存路径（包含文件名与扩展名）。</param>
        /// <param name="format">可选的格式提示，包含 "png" 时保存为 PNG，否则为 JPEG。</param>
        /// <remarks>
        /// 裁切规则保留原实现的行为：默认裁切尺寸为 378x538；当源图高度等于 538 时从右侧裁切；
        /// 否则按源尺寸调整宽度并从右侧裁切。若计算出的裁切矩形超出源图像则方法直接返回不保存。
        /// </remarks>
        public static void CutImage(string sourceFile, string destname, string format = null)
        {
            using var image = SixLabors.ImageSharp.Image.Load(sourceFile);
            int width = 378;
            int height = 538;
            int x = 0;
            int y = 0;

            if (image.Height == height)
            {
                x = image.Width - 378;
            }
            else
            {
                if (image.Width < 800)
                    width = image.Height / 3 * 2;

                height = image.Height;
                x = image.Width - width;
            }

            if ((image.Width < x + width) || image.Height < y + height)
                return;

            var rect = new SixLabors.ImageSharp.Rectangle(x, y, width, height);
            using var cropped = image.Clone(ctx => ctx.Crop(rect));
            if (format != null && format.Contains("png", StringComparison.CurrentCultureIgnoreCase))
                cropped.SaveAsPng(destname);
            else
                cropped.SaveAsJpeg(destname, new JpegEncoder { Quality = 90 });
        }

        /// <summary>
        /// CutImage 的便利重载，根据目录和基础文件名构造最终目标路径并调用 CutImage。
        /// </summary>
        /// <param name="sourceFile">源图片路径。</param>
        /// <param name="destPath">目标目录。</param>
        /// <param name="filename">基础文件名（不含扩展名）。</param>
        /// <param name="format">可选的格式提示。</param>
        public static void CutImage(string sourceFile, string destPath, string filename, string format = null)
        {
            var fileExt = Path.GetExtension(sourceFile);
            var destName = string.Format("{0}/{1}{2}", destPath, filename, fileExt);
            CutImage(sourceFile, destName, format);
        }

        public enum CropMode { Right, Center, Left }

        private static readonly ConcurrentDictionary<float, object> _ratioCache = new();

        /// <summary>
        /// 将源图片裁剪为指定的长宽比并保存结果。
        /// </summary>
        /// <param name="sourcePath">源图片路径。</param>
        /// <param name="savePath">目标保存路径（扩展名决定编码器）。</param>
        /// <param name="targetRatio">目标宽高比（width / height）。</param>
        /// <param name="cropMode">当源图片比目标更宽时，指定水平裁切位置（左/中/右）。</param>
        /// <returns>返回已保存的目标路径（与 savePath 相同）。</returns>
        /// <remarks>
        /// - 当源图更宽时按 targetRatio 计算需要裁剪的宽度并根据 cropMode 决定 x 起点；
        /// - 当源图更高时按 targetRatio 计算需要裁剪的高度并垂直居中；
        /// - 目标若以 ".png" 结尾则以 PNG 保存，否则以 JPEG 保存（质量 90）。
        /// </remarks>
        public static string CropImage(string sourcePath, string savePath, float targetRatio, CropMode cropMode = CropMode.Center)
        {
            using var img = SixLabors.ImageSharp.Image.Load(sourcePath);

            int cropWidth, cropHeight;
            int x = 0, y = 0;
            float sourceRatio = (float)img.Width / img.Height;

            if (sourceRatio > targetRatio)
            {
                cropHeight = img.Height;
                cropWidth = (int)(cropHeight * targetRatio);
                if (img.Width == 800 && img.Height == 538)
                    cropWidth = 378;

                switch (cropMode)
                {
                    case CropMode.Left: x = 0; break;
                    case CropMode.Right: x = img.Width - cropWidth; break;
                    default: x = (img.Width - cropWidth) / 2; break;
                }
            }
            else
            {
                cropWidth = img.Width;
                cropHeight = (int)(cropWidth / targetRatio);
                y = (img.Height - cropHeight) / 2;
            }

            var rect = new SixLabors.ImageSharp.Rectangle(x, y, cropWidth, cropHeight);
            using var cropped = img.Clone(ctx => ctx.Crop(rect));

            var dir = Path.GetDirectoryName(savePath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            if (savePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                cropped.SaveAsPng(savePath);
            else
                cropped.SaveAsJpeg(savePath, new JpegEncoder { Quality = 90 });

            return savePath;
        }
    }
}
