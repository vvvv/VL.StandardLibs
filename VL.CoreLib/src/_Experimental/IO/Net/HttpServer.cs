using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VL.Lib.IO.Net
{
    using Path = System.IO.Path;
    using File = System.IO.File;
    using System.Diagnostics;

    /// <summary>
    /// From https://gist.github.com/zezba9000/04054e3128e6af413e5bc8002489b2fe
    /// In order to work on needs to execute as admin:
    /// netsh http add urlacl url=http://+:80/ user=username
    /// </summary>
    public class HTTPServer : IDisposable
    {
        private static readonly string[] indexFiles =
        {
            "index.html",
            "index.htm",
            "default.html",
            "default.htm"
        };

        private static readonly Dictionary<string, string> mimeTypes = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
        {
            {".asf", "video/x-ms-asf"},
            {".asx", "video/x-ms-asf"},
            {".avi", "video/x-msvideo"},
            {".bin", "application/octet-stream"},
            {".cco", "application/x-cocoa"},
            {".crt", "application/x-x509-ca-cert"},
            {".css", "text/css"},
            {".deb", "application/octet-stream"},
            {".der", "application/x-x509-ca-cert"},
            {".dll", "application/octet-stream"},
            {".dmg", "application/octet-stream"},
            {".ear", "application/java-archive"},
            {".eot", "application/octet-stream"},
            {".exe", "application/octet-stream"},
            {".flv", "video/x-flv"},
            {".gif", "image/gif"},
            {".hqx", "application/mac-binhex40"},
            {".htc", "text/x-component"},
            {".htm", "text/html"},
            {".html", "text/html"},
            {".ico", "image/x-icon"},
            {".img", "application/octet-stream"},
            {".iso", "application/octet-stream"},
            {".jar", "application/java-archive"},
            {".jardiff", "application/x-java-archive-diff"},
            {".jng", "image/x-jng"},
            {".jnlp", "application/x-java-jnlp-file"},
            {".jpeg", "image/jpeg"},
            {".jpg", "image/jpeg"},
            {".js", "application/x-javascript"},
            {".mml", "text/mathml"},
            {".mng", "video/x-mng"},
            {".mov", "video/quicktime"},
            {".mp3", "audio/mpeg"},
            {".mpeg", "video/mpeg"},
            {".mpg", "video/mpeg"},
            {".msi", "application/octet-stream"},
            {".msm", "application/octet-stream"},
            {".msp", "application/octet-stream"},
            {".pdb", "application/x-pilot"},
            {".pdf", "application/pdf"},
            {".pem", "application/x-x509-ca-cert"},
            {".pl", "application/x-perl"},
            {".pm", "application/x-perl"},
            {".png", "image/png"},
            {".prc", "application/x-pilot"},
            {".ra", "audio/x-realaudio"},
            {".rar", "application/x-rar-compressed"},
            {".rpm", "application/x-redhat-package-manager"},
            {".rss", "text/xml"},
            {".run", "application/x-makeself"},
            {".sea", "application/x-sea"},
            {".shtml", "text/html"},
            {".sit", "application/x-stuffit"},
            {".swf", "application/x-shockwave-flash"},
            {".tcl", "application/x-tcl"},
            {".tk", "application/x-tcl"},
            {".txt", "text/plain"},
            {".war", "application/java-archive"},
            {".wbmp", "image/vnd.wap.wbmp"},
            {".wmv", "video/x-ms-wmv"},
            {".xml", "text/xml"},
            {".xpi", "application/x-xpinstall"},
            {".zip", "application/zip"},
            {".map", "application/json"}
        };

        private Thread thread;
        private volatile bool threadActive;

        private HttpListener listener;
        private string path;
        private int port;

        public HTTPServer(IO.Path path, int port = 8080)
        {
            this.path = path;
            this.port = port;

            thread = new Thread(Listen);
            thread.Start();
        }

        public void Dispose()
        {
            // stop thread and listener
            threadActive = false;
            if (listener != null && listener.IsListening) listener.Stop();

            // wait for thread to finish
            if (thread != null)
            {
                thread.Join();
                thread = null;
            }

            // finish closing listener
            if (listener != null)
            {
                listener.Close();
                listener = null;
            }
        }

        private void Listen()
        {
            threadActive = true;

            // start listener
            try
            {
                listener = new HttpListener();
                listener.Prefixes.Add($"http://+:{port}/");
                listener.Start();
            }
            catch (Exception e)
            {
                Trace.TraceError(e.Message);
                threadActive = false;
                return;
            }

            // wait for requests
            while (threadActive)
            {
                try
                {
                    var context = listener.GetContext();
                    if (!threadActive) break;
                    ProcessContext(context);
                }
                catch (HttpListenerException e)
                {
                    if (e.ErrorCode != 995)
                        Trace.TraceError(e.Message);
                    threadActive = false;
                }
                catch (Exception e)
                {
                    Trace.TraceError(e.Message);
                    threadActive = false;
                }
            }
        }

        private void ProcessContext(HttpListenerContext context)
        {
            // get filename path
            string filename = context.Request.Url.AbsolutePath;
            if (filename != null) filename = filename.Substring(1);

            // get default index file if needed
            if (string.IsNullOrEmpty(filename))
            {
                foreach (string indexFile in indexFiles)
                {
                    if (File.Exists(Path.Combine(path, indexFile)))
                    {
                        filename = indexFile;
                        break;
                    }
                }
            }

            Trace.TraceInformation("Loading file: " + filename);
            filename = Path.Combine(path, filename);

            // send file
            HttpStatusCode statusCode;
            if (File.Exists(filename))
            {
                try
                {
                    using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        // get mime type
                        context.Response.ContentType = mimeTypes[Path.GetExtension(filename)];
                        context.Response.ContentLength64 = stream.Length;

                        // copy file stream to response
                        stream.CopyTo(context.Response.OutputStream);
                        stream.Flush();
                        context.Response.OutputStream.Flush();
                    }

                    statusCode = HttpStatusCode.OK;
                }
                catch (Exception e)
                {
                    Trace.TraceError(e.Message);
                    statusCode = HttpStatusCode.InternalServerError;
                }
            }
            else
            {
                Trace.TraceInformation("File not found: " + filename);
                statusCode = HttpStatusCode.NotFound;
            }

            // finish
            context.Response.StatusCode = (int)statusCode;
            if (statusCode == HttpStatusCode.OK)
            {
                context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
                context.Response.AddHeader("Last-Modified", File.GetLastWriteTime(filename).ToString("r"));
            }

            context.Response.OutputStream.Close();
        }
    }
}
