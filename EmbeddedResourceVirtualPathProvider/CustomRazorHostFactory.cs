using System.Web.WebPages.Razor;

namespace EmbeddedResourceVirtualPathProvider
{
    public class CustomRazorHostFactory : WebRazorHostFactory
    {
        public override WebPageRazorHost CreateHost(string virtualPath, string physicalPath)
        {
            // Implementation stolen from MvcRazorHostFactory :)
            var host = base.CreateHost(virtualPath, physicalPath);

            if (!host.IsSpecialPage)
            {
                return new CustomRazorHost(virtualPath, physicalPath);
            }

            return host;
        }
    }
}