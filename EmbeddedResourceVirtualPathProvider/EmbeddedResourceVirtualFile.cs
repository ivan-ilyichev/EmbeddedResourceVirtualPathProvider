using System.IO;
using System.Web;
using System.Web.Hosting;

namespace EmbeddedResourceVirtualPathProvider
{
    class EmbeddedResourceVirtualFile : VirtualFile
    {
        readonly EmbeddedResource _embedded;
        readonly EmbeddedResourceCacheControl _cacheControl;

        public EmbeddedResourceVirtualFile(string virtualPath, EmbeddedResource embedded, EmbeddedResourceCacheControl cacheControl)
            : base(virtualPath)
        {
            this._embedded = embedded;
            this._cacheControl = cacheControl;
        }

        public override Stream Open()
        {
            if (_cacheControl != null)
            {
                HttpContext.Current.Response.Cache.SetCacheability(_cacheControl.Cacheability);
                HttpContext.Current.Response.Cache.AppendCacheExtension("max-age=" + _cacheControl.MaxAge);
                // HttpContext.Current.Response.Cache.SetETag();

                /*
                 * Private:	            Default value. Sets Cache-Control: private to specify that the response is cacheable only on the client and not by shared (proxy server) caches.                 
                 * Public:	            Sets Cache-Control: public to specify that the response is cacheable by clients and shared (proxy) caches.
                 * ServerAndPrivate:	Indicates that the response is cached at the server and at the client but nowhere else. Proxy servers are not allowed to cache the response.
                 */
                if (_cacheControl.Cacheability == HttpCacheability.Private ||
                    _cacheControl.Cacheability == HttpCacheability.Public ||
                    _cacheControl.Cacheability == HttpCacheability.ServerAndPrivate)
                {
                    var lastModified = !string.IsNullOrWhiteSpace(_embedded.FileName) && File.Exists(_embedded.FileName)
                        ? File.GetLastWriteTime(_embedded.FileName)
                        : _embedded.AssemblyLastModified;

                    HttpContext.Current.Response.Cache.SetLastModified(lastModified);
                }
            }
            return _embedded.GetStream();
        }
    }
}