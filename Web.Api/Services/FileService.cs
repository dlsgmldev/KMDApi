using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Net.Http;
using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace KDMApi.Services
{
    public class FileService
    {
        public FileService()
        {

        }
        public void CopyStream(Stream stream, string destPath)
        {
            stream.Seek(0, SeekOrigin.Begin);
            using (var fileStream = new FileStream(destPath, FileMode.Create, FileAccess.Write))
            {
                stream.CopyTo(fileStream);
            }
        }
        public bool CheckAndCreateDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    return true;
                }
                Directory.CreateDirectory(path);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }


        }
        public bool checkFileExtension(string ext, string[] validExts)
        {
            return validExts.Contains(ext);
        }

        public void ResizeImage(Image image, int width, int height, string filename, string ext)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);
            ImageFormat format = ext.Equals(".png") ? ImageFormat.Png : ImageFormat.Jpeg;

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                    destImage.Save(filename, format);
                }
            }


        }
        public void SaveByteArrayAsFile(string fullOutputPath, string base64String)
        {
            byte[] bytes = Convert.FromBase64String(base64String);
            File.WriteAllBytes(fullOutputPath, bytes);
        }

        public void SaveByteArrayAsImage(string fullOutputPath, string base64String, ImageFormat format)
        {
            byte[] bytes = Convert.FromBase64String(base64String);

            Image image;
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                image = Image.FromStream(ms);
                image.Save(fullOutputPath, format);
            }
        }

        public void SaveByteAsFile(string fullOutputPath, string base64String)
        {
            byte[] bytes = Convert.FromBase64String(base64String);

            using (MemoryStream ms = new MemoryStream(bytes))
            {
                SaveFromStream(ms, fullOutputPath);
            }
        }

        public void SaveFromStream(Stream stream, string fullOutputPath)
        {
            using (var fs = File.Create(fullOutputPath))
            {
                stream.CopyTo(fs);
            }
        }

        public byte[] ReadFile(string fullpath)
        {
            return File.ReadAllBytes(fullpath);
        }

        public int CopyFile(string source, string destination)
        {
            if (File.Exists(destination)) return 1;

            if (File.Exists(source))
            {
                File.Copy(source, destination);
                return -1;
            }
            return 0;
        }
    }
}
