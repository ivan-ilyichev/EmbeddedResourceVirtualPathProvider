using System.IO;
using System.Web;
using System.Web.Hosting;

namespace EmbeddedResourceVirtualPathProvider
{
    class EmbeddedResourceVirtualFile : VirtualFile
    {
        readonly EmbeddedResource embedded;
        readonly EmbeddedResourceCacheControl cacheControl;

        public EmbeddedResourceVirtualFile(string virtualPath, EmbeddedResource embedded, EmbeddedResourceCacheControl cacheControl)
            : base(virtualPath)
        {
            this.embedded = embedded;
            this.cacheControl = cacheControl;
        }

        public override Stream Open()
        {
            if (cacheControl != null)
            {
                HttpContext.Current.Response.Cache.SetCacheability(cacheControl.Cacheability);
                HttpContext.Current.Response.Cache.AppendCacheExtension("max-age=" + cacheControl.MaxAge);

                /*
                 * Private:	            Default value. Sets Cache-Control: private to specify that the response is cacheable only on the client and not by shared (proxy server) caches.                 
                 * Public:	            Sets Cache-Control: public to specify that the response is cacheable by clients and shared (proxy) caches.
                 * ServerAndPrivate:	Indicates that the response is cached at the server and at the client but nowhere else. Proxy servers are not allowed to cache the response.
                 */
                if (cacheControl.Cacheability == HttpCacheability.Private ||
                    cacheControl.Cacheability == HttpCacheability.Public ||
                    cacheControl.Cacheability == HttpCacheability.ServerAndPrivate)
                {
                    var lastModified = !string.IsNullOrWhiteSpace(embedded.FileName) && File.Exists(embedded.FileName)
                        ? File.GetLastWriteTime(embedded.FileName)
                        : embedded.AssemblyLastModified;

                    HttpContext.Current.Response.Cache.SetLastModified(lastModified);
                }
            }
            return embedded.GetStream();
        }
    }
}