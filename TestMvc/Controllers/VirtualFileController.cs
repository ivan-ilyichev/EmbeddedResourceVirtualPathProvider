using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;

namespace TestMvc.Controllers
{
    public class VirtualFileController : Controller
    {
        [HttpGet]
        public ActionResult Details(string path)
        {
            if (!HostingEnvironment.VirtualPathProvider.FileExists(path))
                return HttpNotFound("File doesn't exist");

            var file = HostingEnvironment.VirtualPathProvider.GetFile(path);

            return View(file);
        }

        [HttpGet]
        public ActionResult Download(string path)
        {
            if (!HostingEnvironment.VirtualPathProvider.FileExists(path))
                return HttpNotFound("File doesn't exist");

            var file = HostingEnvironment.VirtualPathProvider.GetFile(path);

            using (var stream = file.Open())
            {
                var content = ToBytesArray(file.Open());
                return File(content, "octet-stream", file.Name);
            }
        }

        public static byte[] ToBytesArray(Stream input)
        {
            input.Position = 0;

            var buffer = new byte[16 * 1024];
            using (var ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }
    }
}