using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KDMApi.Models.Temp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;

namespace KDMApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImageController : ControllerBase
    {
        public ImageController()
        {

        }

        [AllowAnonymous]
        [HttpPost("certificate")]
        public string WriteCertificate(CertificateRequest request)
        {
            foreach (string n in request.Names)
            {
                Bitmap bitMapImage = new Bitmap(Path.Combine(new[] { "d:", "temp", "certificate.jpg" }));
                Graphics graphicImage = Graphics.FromImage(bitMapImage);
                graphicImage.SmoothingMode = SmoothingMode.AntiAlias;

                graphicImage.DrawString(n, new Font("Arial", 64, FontStyle.Bold), SystemBrushes.WindowText, new Point(100, 250));

                string fullOutputPath = Path.Combine(new[] { "d:", "temp", n + ".png" });
                bitMapImage.Save(fullOutputPath, ImageFormat.Png);

                graphicImage.Dispose();
                bitMapImage.Dispose();
            }

            return "OK";
        }

    }

}