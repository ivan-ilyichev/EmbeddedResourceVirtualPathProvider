using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;

namespace TestMvc.Controllers
{
    public class VirtualDirectoryController: Controller
    {
        [HttpGet]
        public ActionResult Index()
        {
            return RedirectToAction("Details", new { path = "~/" });
        }

        [HttpGet]
        public ActionResult Details(string path)
        {
            if (!HostingEnvironment.VirtualPathProvider.DirectoryExists(path))
                return HttpNotFound("Directory doesn't exist");

            var virtualDir = HostingEnvironment.VirtualPathProvider.GetDirectory(path);

            return View(virtualDir);
        }
    }
}