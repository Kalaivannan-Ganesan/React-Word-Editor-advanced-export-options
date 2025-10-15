using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.IO;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Syncfusion.EJ2.DocumentEditor;
using WDocument = Syncfusion.DocIO.DLS.WordDocument;
using WFormatType = Syncfusion.DocIO.FormatType;
using SkiaSharp;
using BitMiracle.LibTiff.Classic;
using Syncfusion.DocIORenderer;
using Syncfusion.Pdf;
using System.Diagnostics;
using System.Text;
using EJ2DocumentEditorServer;

namespace SyncfusionDocument.Controllers
{
    [Route("api/[controller]")]
    public class DocumentEditorController : Controller
    {
        private readonly IWebHostEnvironment  _hostingEnvironment;

        public DocumentEditorController(IWebHostEnvironment  hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }

        [AcceptVerbs("Post")]
        [HttpPost]
        [EnableCors("AllowAllOrigins")]
        [Route("Import")]
        public string Import(IFormCollection data)
        {
            if (data.Files.Count == 0)
                return null;
            Stream stream = new MemoryStream();
            IFormFile file = data.Files[0];
            int index = file.FileName.LastIndexOf('.');
            string type = index > -1 && index < file.FileName.Length - 1 ?
            file.FileName.Substring(index) : ".docx";
            file.CopyTo(stream);
            stream.Position = 0;

            //Hooks MetafileImageParsed event.
            WordDocument.MetafileImageParsed += OnMetafileImageParsed;
            WordDocument document = WordDocument.Load(stream, GetFormatType(type.ToLower()));
            //Unhooks MetafileImageParsed event.
            WordDocument.MetafileImageParsed -= OnMetafileImageParsed;
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(document);
            document.Dispose();
            return json;
        }

        //Converts Metafile to raster image.
        private static void OnMetafileImageParsed(object sender, MetafileImageParsedEventArgs args)
        {
            if (args.IsMetafile)
            {
                //MetaFile image conversion(EMF and WMF)
                //You can write your own method definition for converting metafile to raster image using any third-party image converter.
                args.ImageStream = ConvertMetafileToRasterImage(args.MetafileStream);
            }
            else
            {
                //TIFF image conversion
                args.ImageStream = TiffToPNG(args.MetafileStream);

            }
        }

        // Converting Tiff to Png image using Bitmiracle https://www.nuget.org/packages/BitMiracle.LibTiff.NET
        private static MemoryStream TiffToPNG(Stream tiffStream)
        {
            MemoryStream imageStream = new MemoryStream();
            using (Tiff tif = Tiff.ClientOpen("in-memory", "r", tiffStream, new TiffStream()))
            {
                // Find the width and height of the image
                FieldValue[] value = tif.GetField(BitMiracle.LibTiff.Classic.TiffTag.IMAGEWIDTH);
                int width = value[0].ToInt();

                value = tif.GetField(BitMiracle.LibTiff.Classic.TiffTag.IMAGELENGTH);
                int height = value[0].ToInt();

                // Read the image into the memory buffer
                int[] raster = new int[height * width];
                if (!tif.ReadRGBAImage(width, height, raster))
                {
                    throw new Exception("Could not read image");
                }

                // Create a bitmap image using SkiaSharp.
                using (SKBitmap sKBitmap = new SKBitmap(width, height, SKImageInfo.PlatformColorType, SKAlphaType.Premul))
                {
                    // Convert a RGBA value to byte array.
                    byte[] bitmapData = new byte[sKBitmap.RowBytes * sKBitmap.Height];
                    for (int y = 0; y < sKBitmap.Height; y++)
                    {
                        int rasterOffset = y * sKBitmap.Width;
                        int bitsOffset = (sKBitmap.Height - y - 1) * sKBitmap.RowBytes;

                        for (int x = 0; x < sKBitmap.Width; x++)
                        {
                            int rgba = raster[rasterOffset++];
                            bitmapData[bitsOffset++] = (byte)((rgba >> 16) & 0xff);
                            bitmapData[bitsOffset++] = (byte)((rgba >> 8) & 0xff);
                            bitmapData[bitsOffset++] = (byte)(rgba & 0xff);
                            bitmapData[bitsOffset++] = (byte)((rgba >> 24) & 0xff);
                        }
                    }

                    // Convert a byte array to SKColor array.
                    SKColor[] sKColor = new SKColor[bitmapData.Length / 4];
                    int index = 0;
                    for (int i = 0; i < bitmapData.Length; i++)
                    {
                        sKColor[index] = new SKColor(bitmapData[i + 2], bitmapData[i + 1], bitmapData[i], bitmapData[i + 3]);
                        i += 3;
                        index += 1;
                    }

                    // Set the SKColor array to SKBitmap.
                    sKBitmap.Pixels = sKColor;

                    // Save the SKBitmap to PNG image stream.
                    sKBitmap.Encode(SKEncodedImageFormat.Png, 100).SaveTo(imageStream);
                    imageStream.Flush();
                }
            }
            return imageStream;
        }


        private static Stream ConvertMetafileToRasterImage(Stream ImageStream)
        {
            //Here we are loading a default raster image as fallback.
            Stream imgStream = GetManifestResourceStream("ImageNotFound.jpg");
            return imgStream;
            //To do : Write your own logic for converting metafile to raster image using any third-party image converter(Syncfusion doesn't provide any image converter).
        }

        private static Stream GetManifestResourceStream(string fileName)
        {
            System.Reflection.Assembly execAssembly = typeof(WDocument).Assembly;
            string[] resourceNames = execAssembly.GetManifestResourceNames();
            foreach (string resourceName in resourceNames)
            {
                if (resourceName.EndsWith("." + fileName))
                {
                    fileName = resourceName;
                    break;
                }
            }
            return execAssembly.GetManifestResourceStream(fileName);
        }

        
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        internal static FormatType GetFormatType(string format)
        {
            if (string.IsNullOrEmpty(format))
                throw new NotSupportedException("EJ2 DocumentEditor does not support this file format.");
            switch (format.ToLower())
            {
                case ".dotx":
                case ".docx":
                case ".docm":
                case ".dotm":
                    return FormatType.Docx;
                case ".dot":
                case ".doc":
                    return FormatType.Doc;
                case ".rtf":
                    return FormatType.Rtf;
                case ".txt":
                    return FormatType.Txt;
                case ".xml":
                    return FormatType.WordML;
                case ".html":
                    return FormatType.Html;
                default:
                    throw new NotSupportedException("EJ2 DocumentEditor does not support this file format.");
            }
        }
        internal static WFormatType GetWFormatType(string format)
        {
            if (string.IsNullOrEmpty(format))
                throw new NotSupportedException("EJ2 DocumentEditor does not support this file format.");
            switch (format.ToLower())
            {
                case ".dotx":
                    return WFormatType.Dotx;
                case ".docx":
                    return WFormatType.Docx;
                case ".docm":
                    return WFormatType.Docm;
                case ".dotm":
                    return WFormatType.Dotm;
                case ".dot":
                    return WFormatType.Dot;
                case ".doc":
                    return WFormatType.Doc;
                case ".rtf":
                    return WFormatType.Rtf;
                case ".txt":
                    return WFormatType.Txt;
                case ".xml":
                    return WFormatType.WordML;
                case ".odt":
                    return WFormatType.Odt;
                case ".html":
                    return WFormatType.Html;
                case ".md":
                    return WFormatType.Markdown;
                default:
                    throw new NotSupportedException("EJ2 DocumentEditor does not support this file format.");
            }
        }

        private string RetrieveFileType(string name)
        {
            int index = name.LastIndexOf('.');
            string format = index > -1 && index < name.Length - 1 ?
                name.Substring(index) : ".doc";
            return format;
        }

        public class SaveParameter
        {
            public string Content { get; set; }
            public string FileName { get; set; }
            public string Format { get; set; }
        }

        [AcceptVerbs("Post")]
        [HttpPost]
        [EnableCors("AllowAllOrigins")]
        [Route("Export")]
        public FileStreamResult Export([FromBody] SaveParameter data)
        {
            string fileName = data.FileName;
            string format = RetrieveFileType(string.IsNullOrEmpty(data.Format) ? fileName : data.Format);
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = "Document1.docx";
            }
            WDocument document;
            if (format.ToLower() == ".pdf")
            {
                Stream stream = WordDocument.Save(data.Content, FormatType.Docx);
                document = new Syncfusion.DocIO.DLS.WordDocument(stream, Syncfusion.DocIO.FormatType.Docx);
            }
            else
            {
                document = WordDocument.Save(data.Content);
            }
            return SaveDocument(document, format, fileName);
        }
        private FileStreamResult SaveDocument(WDocument document, string format, string fileName)
        {
            Stream stream = new MemoryStream();
            string contentType = "";
            if (format.ToLower() == ".pdf")
            {
                contentType = "application/pdf";
                DocIORenderer render = new DocIORenderer();
                PdfDocument pdfDocument = render.ConvertToPDF(document);
                stream = new MemoryStream();
                pdfDocument.Save(stream);
                pdfDocument.Close();
            }
            else
            {
                WFormatType type = GetWFormatType(format);
                switch (type)
                {
                    case WFormatType.Rtf:
                        contentType = "application/rtf";
                        break;
                    case WFormatType.WordML:
                        contentType = "application/xml";
                        break;
                    case WFormatType.Html:
                        contentType = "application/html";
                        break;
                    case WFormatType.Dotx:
                        contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.template";
                        break;
                    case WFormatType.Docx:
                        contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                        break;
                    case WFormatType.Doc:
                        contentType = "application/msword";
                        break;
                    case WFormatType.Dot:
                        contentType = "application/msword";
                        break;
                    case WFormatType.Odt:
                        contentType = "application/vnd.oasis.opendocument.text";
                        break;
                    case WFormatType.Markdown:
                        contentType = "text/markdown";
                        break;
                }
                document.Save(stream, type);
            }
            document.Close();
            stream.Position = 0;
            return new FileStreamResult(stream, contentType)
            {
                FileDownloadName = fileName
            };
        }
    }

}
