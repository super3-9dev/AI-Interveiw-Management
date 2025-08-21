// Services/PdfService.cs
using DinkToPdf;
using DinkToPdf.Contracts;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace InterviewBot.Services
{
    public class PdfService
    {
        private readonly IConverter _converter;

        public PdfService(IConverter converter)
        {
            _converter = converter;

            // Add this to ensure the native library is loaded
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var context = new CustomAssemblyLoadContext();
                var libPath = Path.Combine(Directory.GetCurrentDirectory(), "runtimes", "win-x64", "native", "libwkhtmltox.dll");
                context.LoadUnmanagedLibrary(libPath);
            }
        }

        public byte[] GeneratePdf(string htmlContent)
        {
            try
            {
                var globalSettings = new GlobalSettings
                {
                    ColorMode = ColorMode.Color,
                    Orientation = Orientation.Portrait,
                    PaperSize = PaperKind.A4,
                    Margins = new MarginSettings { Top = 20, Bottom = 20, Left = 20, Right = 20 },
                    DocumentTitle = "Interview Report"
                };

                var objectSettings = new ObjectSettings
                {
                    PagesCount = true,
                    HtmlContent = htmlContent,
                    WebSettings = {
                        DefaultEncoding = "utf-8",
                        UserStyleSheet = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "css", "pdf-styles.css")
                    },
                    LoadSettings = new LoadSettings
                    {
                        BlockLocalFileAccess = false // Allow local file access for CSS
                    }
                };

                var pdf = new HtmlToPdfDocument()
                {
                    GlobalSettings = globalSettings,
                    Objects = { objectSettings }
                };

                return _converter.Convert(pdf);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PDF generation error: {ex}");
                throw new Exception("Failed to generate PDF", ex);
            }
        }

    }
    internal class CustomAssemblyLoadContext : System.Runtime.Loader.AssemblyLoadContext
    {
        public IntPtr LoadUnmanagedLibrary(string absolutePath)
        {
            return LoadUnmanagedDll(absolutePath);
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            return LoadUnmanagedDllFromPath(unmanagedDllName);
        }

        protected override System.Reflection.Assembly Load(System.Reflection.AssemblyName assemblyName)
        {
            throw new NotImplementedException();
        }
    }
}