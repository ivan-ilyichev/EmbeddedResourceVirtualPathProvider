using System;
using System.IO;
using System.Web;
using System.Web.Hosting;

namespace EmbeddedResourceVirtualPathProvider
{
    /// <summary>
    /// HostingEnvironmentUtils
    /// </summary>
    public static class PathUtils
    {
        private static object _syncLock = new object();

        private static string hostMapPath = null;
        /// <summary>
        /// Simples the map path.
        /// </summary>
        /// <param name="virtualPath">The virtual path.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">virtualPath</exception>
        /// <exception cref="System.Exception">virtualPath must start with ~</exception>
        public static string MapPath(string virtualPath)
        {
            if (hostMapPath == null)
            {
                lock (_syncLock)
                {
                    hostMapPath = HostingEnvironment.MapPath("~/");
                }
            }

            if (String.IsNullOrWhiteSpace(virtualPath))
                throw new ArgumentNullException("virtualPath");

            if (!(virtualPath.StartsWith("~/", StringComparison.OrdinalIgnoreCase) || virtualPath.StartsWith("/", StringComparison.OrdinalIgnoreCase) || virtualPath.StartsWith("\\", StringComparison.OrdinalIgnoreCase)))
                throw new Exception("virtualPath must start with ~/ , / or \\");

            var path = virtualPath.TrimStart('~', '/', '\\');

            var httpContext = HttpContext.Current;
            if (httpContext != null && httpContext.Request != null)
            {
                var applicationPath = httpContext.Request.ApplicationPath.TrimStart('/');
                if (path.StartsWith(applicationPath, StringComparison.OrdinalIgnoreCase))
                {
                    var startIndex = applicationPath.Length;
                    path = path.Substring(startIndex);
                }
            }

            path = path.Replace('/', '\\');
            path = path.TrimStart('\\');

            var result = Path.Combine(hostMapPath, path);
            return result;
        }

        /// <summary>
        /// Virtuals the path.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">We can only map an absolute back to a relative path if an HttpContext is available.</exception>
        public static string VirtualPath(string filePath)
        {
            if (filePath == null)
                throw new ArgumentNullException("filePath");

            if (HttpContext.Current == null)
                throw new InvalidOperationException("We can only map an absolute back to a relative path if an HttpContext is available.");

            var request = HttpContext.Current.Request;
            var applicationPath = request.PhysicalApplicationPath;
            var virtualDir = request.ApplicationPath;
            virtualDir = virtualDir == "/" ? virtualDir : (virtualDir + "/");
            return "~" + filePath.Replace(applicationPath, virtualDir).Replace("\\", "/");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="virtualPath">The virtual path.</param>
        /// <returns></returns>
        public static string GetAbsolutePath(string virtualPath)
        {
            var httpContext = HttpContext.Current;
            if (httpContext == null)
            {
                return virtualPath;
            }

            var tilde = "~";

            var result = virtualPath
                .Replace(tilde, String.Empty);

            var applicationPath = httpContext.Request.ApplicationPath;

            if (applicationPath != "/" && result.StartsWith(applicationPath, StringComparison.OrdinalIgnoreCase))
            {
                var startIndex = applicationPath.Length;
                result = result.Substring(startIndex);
            }

            if (!result.StartsWith("/", StringComparison.Ordinal))
                result = "/" + result;

            return result;
        }
    }

}
