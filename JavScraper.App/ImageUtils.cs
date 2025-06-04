using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace JavScraper.App
{
    public class ImageUtils
    {
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
