using System;
using System.Drawing.Printing;
using System.Globalization;
using System.Threading;

namespace Pechkin
{
    /// <summary>
    /// Global configuration object. Is used to create converter objects.
    /// 
    /// It uses fluid notation to change its fields.
    /// </summary>
    [Serializable]
    public class GlobalConfig
    {
        private string _paperSize = "A4";
        private string _paperWidth; // for example "4cm"
        private string _paperHeight; // or "12in"
        private string _paperOrientation = "Portrait"; // must be either "Landscape" or "Portrait"

        private string _colorMode = "Color"; // must be either "Color" or "Grayscale"
        private string _resolution; // deprecated, has no effect
        private string _dpi; // DPI used when printing, like "80"

        private string _pageOffset; // start page number (used in headers, footers and TOC)
        private string _copies; // number of copies to include into the document =)
        private string _collate = "false"; // collate copies or not, must be either "true" or "false"

        private string _outline = "false"; // generate table of contents, must be either "true" or "false"
        private string _outlineDepth = "4"; // outline depth
        private string _dumpOutline = ""; // filename to dump outline in XML format

        private string _output = ""; // filename to dump PDF into, if "-", then it's dumped into stdout
        private string _documentTitle; // title for the PDF document

        private string _useCompression = "true"; // turns on lossless compression of the PDF file

        private string _marginTop; // size of the top margin (ex. "2cm)
        private string _marginRight;
        private string _marginBottom;
        private string _marginLeft;

        private string _outputFormat = "pdf"; // can be "ps" or "pdf"

        private string _imageDpi; // maximum DPI for the images in document
        private string _imageQuality; // specifies JPEG compression factor for the (reencoded) images in pdf, from "0" to "100"

        private string _cookieJar; // path to (text) file used to load and store cookies

        [Obsolete("Setting paper size by name doesn't work in the lib. Use the overload that takes PaperKind instead.")]
        public GlobalConfig SetPaperSize(string sizeName)
        {
            _paperSize = sizeName;

            _paperHeight = "";
            _paperWidth = "";

            return this;
        }
        public GlobalConfig SetPaperSize(PaperKind kind)
        {
            // don't work
            //return SetPaperSize(Enum.GetName(typeof(PaperKind), kind));

            if (PechkinStatic.PaperSizes.ContainsKey(kind))
            {
                PechkinStatic.StrPaperSize ps = PechkinStatic.PaperSizes[kind];

                return SetPaperSize(ps.Width, ps.Height);
            }

            Tracer.Warn("T:" + Thread.CurrentThread.Name + " Unknown PaperKind specified in SetPaperSize (" + ((int)kind) + ")");

            return this;
        }

        internal GlobalConfig SetPaperSize(string width, string height)
        {
            _paperSize = null;

            _paperWidth = width;
            _paperHeight = height;

            return this;
        }

        /// <summary>
        /// Sets exact paper size for the document
        /// </summary>
        /// <param name="width">width of the document in hudredths of inches</param>
        /// <param name="height">height of the document in hudredths of inches</param>
        /// <returns>configuration object</returns>
        public GlobalConfig SetPaperSize(int width, int height)
        {
            _paperSize = null;

            _paperWidth = (width / 100.0).ToString("0.##", CultureInfo.InvariantCulture) +"in";
            _paperHeight = (height / 100.0).ToString("0.##", CultureInfo.InvariantCulture) +"in";

            return this;
        }
        public GlobalConfig SetPaperSize(PaperSize ps)
        {
            return ps.Kind == PaperKind.Custom ? SetPaperSize(ps.Width, ps.Height) : SetPaperSize(ps.Kind);
        }

        public GlobalConfig SetPaperOrientation(bool landscape)
        {
            _paperOrientation = landscape ? "Landscape" : "Portrait";

            return this;
        }

        /// <summary>
        /// Sets top paper margin.
        /// </summary>
        /// <param name="width">width of the margin in hundredths of inch</param>
        /// <returns>config object</returns>
        public GlobalConfig SetMarginTop(int width)
        {
            _marginTop = (width / 100.0).ToString("0.##", CultureInfo.InvariantCulture) + "in";

            return this;
        }
        /// <summary>
        /// Sets right paper margin.
        /// </summary>
        /// <param name="width">width of the margin in hundredths of inch</param>
        /// <returns>config object</returns>
        public GlobalConfig SetMarginRight(int width)
        {
            _marginRight = (width / 100.0).ToString("0.##", CultureInfo.InvariantCulture) + "in";

            return this;
        }
        /// <summary>
        /// Sets bottom paper margin.
        /// </summary>
        /// <param name="width">width of the margin in hundredths of inch</param>
        /// <returns>config object</returns>
        public GlobalConfig SetMarginBottom(int width)
        {
            _marginBottom = (width / 100.0).ToString("0.##", CultureInfo.InvariantCulture) + "in";

            return this;
        }
        /// <summary>
        /// Sets left paper margin.
        /// </summary>
        /// <param name="width">width of the margin in hundredths of inch</param>
        /// <returns>config object</returns>
        public GlobalConfig SetMarginLeft(int width)
        {
            _marginLeft = (width / 100.0).ToString("0.##", CultureInfo.InvariantCulture) + "in";

            return this;
        }
        /// <summary>
        /// Sets all four paper margins at once.
        /// </summary>
        /// <param name="top">width of the top margin in hundredths of inch</param>
        /// <param name="right">width of the right margin in hundredths of inch</param>
        /// <param name="bottom">width of the bottom margin in hundredths of inch</param>
        /// <param name="left">width of the left margin in hundredths of inch</param>
        /// <returns>config object</returns>
        public GlobalConfig SetMargins(int top, int right, int bottom, int left)
        {
            return SetMarginTop(top)
                .SetMarginRight(right)
                .SetMarginBottom(bottom)
                .SetMarginLeft(left);
        }
        public GlobalConfig SetMargins(Margins margins)
        {
            return SetMargins(margins.Top, margins.Right, margins.Bottom, margins.Left);
        }

        public GlobalConfig SetOutputDpi(int dpi)
        {
            _dpi = dpi.ToString();

            return this;
        }

        public GlobalConfig SetPageSettings(PageSettings ps)
        {
            return SetPaperSize(ps.PaperSize)
                .SetPaperOrientation(ps.Landscape)
                .SetMargins(ps.Margins)
                .SetOutputDpi(ps.PrinterResolution.X);
        }

        public GlobalConfig SetColorMode(bool grayscale)
        {
            _colorMode = grayscale ? "Grayscale" : "Color";

            return this;
        }

        [Obsolete("This setting does nothing. Left here for full compatibility.")]
        public GlobalConfig SetResolution(int resolution)
        {
            _resolution = resolution.ToString();

            return this;
        }

        public GlobalConfig SetCopyCount(int count)
        {
            _copies = count.ToString();

            return this;
        }

        /// <summary>
        /// Sets first page number to use on TOC and header/footer
        /// </summary>
        /// <param name="startPageNumber">first page number</param>
        /// <returns>config object</returns>
        public GlobalConfig SetPageOffset(int startPageNumber)
        {
            _pageOffset = startPageNumber.ToString();

            return this;
        }

        public GlobalConfig SetCopyCollation(bool collate)
        {
            _collate = collate ? "true" : "false";

            return this;
        }

        /// <summary>
        /// Allows you to enable outline generation. Oultine appears in Adobe Reader to the left of the document by default.
        /// </summary>
        /// <param name="generateOutline"></param>
        /// <returns>config object</returns>
        public GlobalConfig SetOutlineGeneration(bool generateOutline)
        {
            _outline = generateOutline ? "true" : "false";

            return this;
        }

        public GlobalConfig SetOutlineDepth(int depth)
        {
            _outlineDepth = depth.ToString();

            return this;
        }

        public GlobalConfig SetOutlineDumpFile(string outlineXmlFilename)
        {
            _dumpOutline = outlineXmlFilename;

            return this;
        }

        public GlobalConfig SetOutputFile(string outputFilename)
        {
            _output = outputFilename;

            return this;
        }

        public enum OutputFormat { PostScript, Pdf }
        public GlobalConfig SetOutputFormat(OutputFormat format)
        {
            _outputFormat = format == OutputFormat.Pdf ? "pdf" : "ps";

            return this;
        }

        public GlobalConfig SetDocumentTitle(string title)
        {
            _documentTitle = title;

            return this;
        }

        public GlobalConfig SetLosslessCompression(bool zipOutputFile)
        {
            _useCompression = zipOutputFile ? "true" : "false";

            return this;
        }

        /// <summary>
        /// Sets filename for the cookies to read and write to. It's used for objects that are webpages.
        /// </summary>
        /// <param name="cookieFileName">Filename of the cookie archive</param>
        /// <returns></returns>
        public GlobalConfig SetCookieTextFile(string cookieFileName)
        {
            _cookieJar = cookieFileName;

            return this;
        }

        /// <summary>
        /// Sets the maximum image DPI to use in generated PDF document. Images with higher dpis are scaled.
        /// </summary>
        /// <param name="dpi">raw DPI value or -1 for the infinity</param>
        /// <returns></returns>
        public GlobalConfig SetMaxImageDpi(int dpi)
        {
            _imageDpi = dpi != -1 ? dpi.ToString() : null;

            return this;
        }

        /// <summary>
        /// All the images in HTML are reencoded into JPEGs, this sets their new quality setting.
        /// </summary>
        /// <param name="quality">quality from 0 to 100, the more the better</param>
        /// <returns></returns>
        public GlobalConfig SetImageQuality(int quality)
        {
            _imageQuality = quality != -1 ? quality.ToString() : null;

            return this;
        }

        internal void SetUpGlobalConfig(IntPtr config)
        {
            Tracer.Trace("T:" + Thread.CurrentThread.Name + " Setting up global config (many wkhtmltopdf_set_global_setting)");

            if (_paperSize != null)
            {
                PechkinStatic.SetGlobalSetting(config, "size.paperSize", _paperSize);
            }
            if (_paperWidth != null)
            {
                PechkinStatic.SetGlobalSetting(config, "size.width", _paperWidth);
            }
            if (_paperHeight != null)
            {
                PechkinStatic.SetGlobalSetting(config, "size.height", _paperHeight);
            }
            if (_paperOrientation != null)
            {
                PechkinStatic.SetGlobalSetting(config, "orientation", _paperOrientation);
            }
            if (_colorMode != null)
            {
                PechkinStatic.SetGlobalSetting(config, "colorMode", _colorMode);
            }
            if (_resolution != null)
            {
                PechkinStatic.SetGlobalSetting(config, "resolution", _resolution);
            }
            if (_dpi != null)
            {
                PechkinStatic.SetGlobalSetting(config, "dpi", _dpi);
            }
            if (_pageOffset != null)
            {
                PechkinStatic.SetGlobalSetting(config, "pageOffset", _pageOffset);
            }
            if (_copies != null)
            {
                PechkinStatic.SetGlobalSetting(config, "copies", _copies);
            }
            if (_collate != null)
            {
                PechkinStatic.SetGlobalSetting(config, "collate", _collate);
            }
            if (_outline != null)
            {
                PechkinStatic.SetGlobalSetting(config, "outline", _outline);
            }
            if (_outlineDepth != null)
            {
                PechkinStatic.SetGlobalSetting(config, "outlineDepth", _outlineDepth);
            }
            if (_dumpOutline != null)
            {
                PechkinStatic.SetGlobalSetting(config, "dumpOutline", _dumpOutline);
            }
            if (_output != null)
            {
                PechkinStatic.SetGlobalSetting(config, "out", _output);
            }
            if (_documentTitle != null)
            {
                PechkinStatic.SetGlobalSetting(config, "documentTitle", _documentTitle);
            }
            if (_useCompression != null)
            {
                PechkinStatic.SetGlobalSetting(config, "useCompression", _useCompression);
            }
            if (_marginTop != null)
            {
                PechkinStatic.SetGlobalSetting(config, "margin.top", _marginTop);
            }
            if (_marginRight != null)
            {
                PechkinStatic.SetGlobalSetting(config, "margin.right", _marginRight);
            }
            if (_marginBottom != null)
            {
                PechkinStatic.SetGlobalSetting(config, "margin.bottom", _marginBottom);
            }
            if (_marginLeft != null)
            {
                PechkinStatic.SetGlobalSetting(config, "margin.left", _marginLeft);
            }
            if (_outputFormat != null)
            {
                PechkinStatic.SetGlobalSetting(config, "outputFormat", _outputFormat);
            }
            if (_imageDpi != null)
            {
                PechkinStatic.SetGlobalSetting(config, "imageDPI", _imageDpi);
            }
            if (_imageQuality != null)
            {
                PechkinStatic.SetGlobalSetting(config, "imageQuality", _imageQuality);
            }
            if (_cookieJar != null)
            {
                PechkinStatic.SetGlobalSetting(config, "load.cookieJar", _cookieJar);
            }
        }

        internal IntPtr CreateGlobalConfig()
        {
            IntPtr config = PechkinStatic.CreateGlobalSetting();

            SetUpGlobalConfig(config);

            return config;
        }
    }
}
