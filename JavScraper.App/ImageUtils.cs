using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using SkiaSharp;
using System.IO;
using System.Linq;
using System.Text;

namespace JavScraper.App
{
    public class ImageUtils
    {
        /// <summary>
        /// 图片处理工具类，封装了常用的图片格式转换、裁剪与按目标格式保存的功能。
        /// </summary>
        /// <remarks>
        /// - 方法均为静态，方便在无需创建实例的情况下直接调用；
        /// - 对于涉及文件 IO 与图像对象的操作，方法内部会确保及时释放资源（Dispose）；
        /// - 对于 WebP 的支持依赖系统是否安装了对应的图像编码器（通过 <see cref="ImageCodecInfo"/> 检测），若未找到则会回退为 PNG。
        /// </remarks>
        /// <summary>
        /// 将图片从源文件转换并保存为指定目标文件。目标格式可通过目标文件扩展名推断（例如 .png, .jpg, .webp）。
        /// </summary>
        /// <param name="filename">源图片文件路径。</param>
        /// <param name="destname">目标图片文件路径（包含文件名与扩展名）。</param>
        /// <param name="format">可选的目标 <see cref="ImageFormat"/>，若为 null 则根据目标扩展名推断或默认使用 JPEG。</param>
        /// <remarks>
        /// - 对 .webp 扩展名的支持依赖系统安装的 WebP 编码器；若未安装则回退为 PNG 保存并输出警告信息。
        /// - 方法会在内部确保释放加载的图像资源。
        /// </remarks>
        public static void ConvertImage(string filename, string destname, ImageFormat format = null)
        {
            if (!File.Exists(filename))
                throw new FileNotFoundException("Source image not found.", filename);

            var bytes = File.ReadAllBytes(filename);
            using var codec = SKCodec.Create(new SKMemoryStream(bytes));
            if (codec == null)
                throw new InvalidOperationException("Unable to decode source image.");

            using var skBitmap = SKBitmap.Decode(codec);
            using var skImage = SKImage.FromBitmap(skBitmap);

            var ext = Path.GetExtension(destname).ToLowerInvariant();
            var encFormat = GetSkiaFormatByExtension(ext);
            var quality = 90;

            using var data = skImage.Encode(encFormat, quality);
            if (data == null)
                throw new InvalidOperationException("Failed to encode image.");

            using var fs = File.Open(destname, FileMode.Create, FileAccess.Write);
            data.SaveTo(fs);
        }
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


        /// <summary>
        /// 将指定图片裁剪为固定尺寸并保存到目标文件。
        /// </summary>
        /// <param name="sourceFile">源图片文件路径。</param>
        /// <param name="destname">目标图片文件完整路径。</param>
        /// <param name="format">可选的保存格式，若为 null 则根据目标扩展名或默认值确定。</param>
        /// <remarks>裁剪逻辑默认使用右侧裁剪，保留原始高度或在小尺寸下按比例调整宽度。</remarks>
        public static void CutImage(string sourceFile, string destname, ImageFormat format = null)
        {
            //新图片路径
            string fileExt = Path.GetExtension(sourceFile);

            //string destName = string.Format("{0}/{1}{2}", destPath, filename, fileExt);
            //加载图片
            // decode with SkiaSharp for better cross-format support
            var bytes = File.ReadAllBytes(sourceFile);
            using var codec = SKCodec.Create(new SKMemoryStream(bytes));
            if (codec == null)
                return;
            using var srcBitmap = SKBitmap.Decode(codec);

            int imgWidth = srcBitmap.Width;
            int imgHeight = srcBitmap.Height;

            int x = 0;
            int y = 0;
            int width = 378;
            int height = 538;

            if (imgHeight == height)
            {
                x = imgWidth - 378;
            }
            else
            {
                if (imgWidth < 800)
                {
                    width = imgHeight / 3 * 2;
                }

                height = imgHeight;
                x = imgWidth - width;
            }

            //定义截取矩形
            // 判断超出的位置否
            if ((imgWidth < x + width) || imgHeight < y + height)
            {
                return;
            }

            var subset = new SKRectI(x, y, x + width, y + height);
            using var cropped = new SKBitmap(subset.Width, subset.Height, srcBitmap.ColorType, srcBitmap.AlphaType);
            srcBitmap.ExtractSubset(cropped, subset);

            using var image = SKImage.FromBitmap(cropped);
            var ext = Path.GetExtension(destname).ToLowerInvariant();
            var encFormat = GetSkiaFormatByExtension(ext);
            var quality = 90;
            using var data = image.Encode(encFormat, quality);
            if (data != null)
            {
                using var fs = File.OpenWrite(destname);
                data.SaveTo(fs);
            }
        }

        /// <summary>
        /// 将指定图片裁剪为固定尺寸并以指定文件名保存到目标目录。
        /// </summary>
        /// <param name="sourceFile">源图片文件路径。</param>
        /// <param name="destPath">目标目录路径。</param>
        /// <param name="filename">保存的文件名（不包含扩展名，扩展名将与源文件相同）。</param>
        /// <param name="format">可选的保存格式，若为 null 则根据目标扩展名或默认值确定。</param>
        /// <remarks>方法会组合 destPath、filename 与源文件扩展名生成最终保存路径。</remarks>
        public static void CutImage(string sourceFile, string destPath, string filename, ImageFormat format = null)
        {
            //新图片路径
            string fileExt = Path.GetExtension(sourceFile);

            string destName = string.Format("{0}/{1}{2}", destPath, filename, fileExt);
            //加载图片
            var bytes = File.ReadAllBytes(sourceFile);
            using var codec = SKCodec.Create(new SKMemoryStream(bytes));
            if (codec == null)
                return;
            using var srcBitmap = SKBitmap.Decode(codec);

            int imgWidth = srcBitmap.Width;
            int imgHeight = srcBitmap.Height;

            int x = 0;
            int y = 0;
            int width = 378;
            int height = 538;

            if (imgHeight == height)
            {
                x = imgWidth - 378;
            }
            else
            {
                if (imgWidth < 800)
                {
                    width = imgHeight / 3 * 2;
                }

                height = imgHeight;
                x = imgWidth - width;
            }

            //定义截取矩形
            // 判断超出的位置否
            if ((imgWidth < x + width) || imgHeight < y + height)
            {
                return;
            }

            var subset = new SKRectI(x, y, x + width, y + height);
            using var cropped = new SKBitmap(subset.Width, subset.Height, srcBitmap.ColorType, srcBitmap.AlphaType);
            srcBitmap.ExtractSubset(cropped, subset);

            using var image = SKImage.FromBitmap(cropped);
            var ext = Path.GetExtension(destName).ToLowerInvariant();
            var encFormat = GetSkiaFormatByExtension(ext);
            var quality = 90;
            using var data = image.Encode(encFormat, quality);
            if (data != null)
            {
                using var fs = File.OpenWrite(destName);
                data.SaveTo(fs);
            }
        }

        public enum CropMode
        {
            Right,
            Center,
            Left

        }


        /// <summary>
        /// 根据目标文件扩展名推断 <see cref="ImageFormat"/>。
        /// </summary>
        /// <param name="path">目标文件路径或文件名。</param>
        /// <returns>若能直接映射返回相应的 <see cref="ImageFormat"/>，否则返回 null（例如 .webp）。</returns>
        private static ImageFormat ResolveFormatFromExtension(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            var ext = Path.GetExtension(path).ToLowerInvariant();
            return ext switch
            {
                ".png" => ImageFormat.Png,
                ".webp" => null,// No ImageFormat.WebP in System.Drawing; return null to trigger codec-based save
                ".jpg" or ".jpeg" => ImageFormat.Jpeg,
                ".bmp" => ImageFormat.Bmp,
                ".gif" => ImageFormat.Gif,
                _ => null,
            };
        }

        /// <summary>
        /// 将图像保存到指定路径，支持通过系统编码器保存 WebP（若可用），否则按指定格式保存。
        /// </summary>
        /// <param name="img">要保存的图像对象（调用方负责在此方法外释放资源）。</param>
        /// <param name="path">目标文件路径（通过扩展名决定保存策略）。</param>
        /// <param name="format">首选的 <see cref="ImageFormat"/>。在处理 .webp 时会尝试使用系统编码器。</param>
        private static void SaveImage(Image img, string path, ImageFormat format)
        {
            // If target is webp (extension .webp) but format is null or not webp, try codec by mime
            var ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext == ".webp")
            {
                var webpCodec = GetEncoderInfo("image/webp");
                if (webpCodec != null)
                {
                    img.Save(path, webpCodec, null);
                    return;
                }
                else
                {
                    // Fallback: save as PNG but with .webp extension (best-effort)
                    Console.WriteLine("WebP encoder not found on this system. Falling back to PNG format for .webp output.");
                    img.Save(path, ImageFormat.Png);
                    return;
                }
            }

            // For regular formats use Save with ImageFormat
            img.Save(path, format);
        }

        /// <summary>
        /// 根据 MIME 类型查找本机已注册的图像编码器信息（<see cref="ImageCodecInfo"/>）。
        /// </summary>
        /// <param name="mimeType">编码器的 MIME 类型（例如 "image/webp"）。</param>
        /// <returns>若找到返回对应的 <see cref="ImageCodecInfo"/>，否则返回 null。</returns>
        private static ImageCodecInfo GetEncoderInfo(string mimeType)
        {
            var codecs = ImageCodecInfo.GetImageEncoders();
            return codecs.FirstOrDefault(c => string.Equals(c.MimeType, mimeType, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 根据文件扩展名返回 SkiaSharp 的编码格式。
        /// </summary>
        /// <param name="ext">扩展名（例如 .jpg, .png, .webp）</param>
        /// <returns>对应的 <see cref="SKEncodedImageFormat"/></returns>
        private static SKEncodedImageFormat GetSkiaFormatByExtension(string ext)
        {
            return ext switch
            {
                ".png" => SKEncodedImageFormat.Png,
                ".webp" => SKEncodedImageFormat.Webp,
                ".gif" => SKEncodedImageFormat.Gif,
                ".bmp" => SKEncodedImageFormat.Bmp,
                ".jpg" or ".jpeg" => SKEncodedImageFormat.Jpeg,
                _ => SKEncodedImageFormat.Jpeg,
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
                    case CropMode.Center:
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
