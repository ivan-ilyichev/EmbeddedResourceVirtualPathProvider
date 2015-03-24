using System.Web.Mvc.Razor;

namespace EmbeddedResourceVirtualPathProvider
{
    public class CustomRazorHost : MvcWebPageRazorHost
    {
        public CustomRazorHost(string virtualPath, string physicalPath)
            : base(virtualPath, physicalPath)
        {
            var vpp = System.Web.Hosting.HostingEnvironment.VirtualPathProvider as Vpp;
            if (vpp != null && vpp.FileExists(virtualPath))
            {
                var file = vpp.GetFile(virtualPath) as EmbeddedResourceVirtualFile;
                if (file != null && !string.IsNullOrWhiteSpace(file.Embedded.FileName))
                {
                    PhysicalPath = file.Embedded.FileName;
                }
            }
        }
    }
}