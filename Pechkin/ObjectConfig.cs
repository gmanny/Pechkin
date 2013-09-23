using System;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Threading;

namespace Pechkin
{
    /// <summary>
    /// Document settings object. Is used to supply document parameters to the converter.
    /// 
    /// You will find that there's no page body for conversion of pages that are in memory.
    /// Instead the page body is supplied into the <code>Convert</code> method of the converter.
    /// </summary>
    [Serializable]
    public class ObjectConfig
    {
        [Serializable]
        public class HeaderSettings
        {
            private string _fontSize; // font size for the header in pt, e.g. "13"
            private string _fontName; // font name for the header, e.g. "Courier New"
            
            /* following text fields can containt several macros which will be replaced with their respective values:
             * [page]       Replaced by the number of the pages currently being printed
             * [frompage]   Replaced by the number of the first page to be printed
             * [topage]     Replaced by the number of the last page to be printed
             * [webpage]    Replaced by the URL of the page being printed
             * [section]    Replaced by the name of the current section
             * [subsection] Replaced by the name of the current subsection
             * [date]       Replaced by the current date in system local format
             * [time]       Replaced by the current time in system local format
             * 
             * see http://madalgo.au.dk/~jakobt/wkhtmltoxdoc/wkhtmltopdf-0.9.9-doc.html
             */
            private string _leftText;
            private string _centerText;
            private string _rightText;

            private string _lineSeparator = "false"; // if "true", line is printed under the header (and on top of the footer) separating header from content
            private string _space; // space between the header and content in pt, e.g. "1.8"

            private string _htmlUrl; // URL for the HTML document to use as a header

            public HeaderSettings SetFontSize(double sizeInPt)
            {
                _fontSize = sizeInPt.ToString("0.##", CultureInfo.InvariantCulture);

                return this;
            }
            public HeaderSettings SetFontName(string fontName)
            {
                _fontName = fontName;

                return this;
            }
            public HeaderSettings SetFont(Font font)
            {
                return SetFontName(font.Name).SetFontSize(font.SizeInPoints);
            }

            /// <summary>
            /// Sets left text for the header/footer. Following replaces occur in this text:
            /// * [page]       Replaced by the number of the pages currently being printed
            /// * [frompage]   Replaced by the number of the first page to be printed
            /// * [topage]     Replaced by the number of the last page to be printed
            /// * [webpage]    Replaced by the URL of the page being printed
            /// * [section]    Replaced by the name of the current section
            /// * [subsection] Replaced by the name of the current subsection
            /// * [date]       Replaced by the current date in system local format
            /// * [time]       Replaced by the current time in system local format
            /// </summary>
            /// <param name="leftText">text for the left part</param>
            /// <returns>config object</returns>
            public HeaderSettings SetLeftText(string leftText)
            {
                _leftText = leftText;

                return this;
            }
            /// <summary>
            /// Sets center text for the header/footer. Following replaces occur in this text:
            /// * [page]       Replaced by the number of the pages currently being printed
            /// * [frompage]   Replaced by the number of the first page to be printed
            /// * [topage]     Replaced by the number of the last page to be printed
            /// * [webpage]    Replaced by the URL of the page being printed
            /// * [section]    Replaced by the name of the current section
            /// * [subsection] Replaced by the name of the current subsection
            /// * [date]       Replaced by the current date in system local format
            /// * [time]       Replaced by the current time in system local format
            /// </summary>
            /// <param name="centerText">text for the center part</param>
            /// <returns>config object</returns>
            public HeaderSettings SetCenterText(string centerText)
            {
                _centerText = centerText;

                return this;
            }
            /// <summary>
            /// Sets right text for the header/footer. Following replaces occur in this text:
            /// * [page]       Replaced by the number of the pages currently being printed
            /// * [frompage]   Replaced by the number of the first page to be printed
            /// * [topage]     Replaced by the number of the last page to be printed
            /// * [webpage]    Replaced by the URL of the page being printed
            /// * [section]    Replaced by the name of the current section
            /// * [subsection] Replaced by the name of the current subsection
            /// * [date]       Replaced by the current date in system local format
            /// * [time]       Replaced by the current time in system local format
            /// </summary>
            /// <param name="rightText">text for the right part</param>
            /// <returns>config object</returns>
            public HeaderSettings SetRightText(string rightText)
            {
                _rightText = rightText;

                return this;
            }
            /// <summary>
            /// Sets the texts for the header/footer. Following replaces occur in each of texts:
            /// * [page]       Replaced by the number of the pages currently being printed
            /// * [frompage]   Replaced by the number of the first page to be printed
            /// * [topage]     Replaced by the number of the last page to be printed
            /// * [webpage]    Replaced by the URL of the page being printed
            /// * [section]    Replaced by the name of the current section
            /// * [subsection] Replaced by the name of the current subsection
            /// * [date]       Replaced by the current date in system local format
            /// * [time]       Replaced by the current time in system local format
            /// </summary>
            /// <param name="leftText">text for the left part</param>
            /// <param name="centerText">text for the center part</param>
            /// <param name="rightText">text for the right part</param>
            /// <returns>config object</returns>
            public HeaderSettings SetTexts(string leftText, string centerText, string rightText)
            {
                return SetLeftText(leftText).SetCenterText(centerText).SetRightText(rightText);
            }

            public HeaderSettings SetLineSeparator(bool useLineSeparator)
            {
                _lineSeparator = useLineSeparator ? "true" : "false";

                return this;
            }

            /// <summary>
            /// Sets the amount of space between header/footer and the page content.
            /// </summary>
            /// <param name="distanceFromContent">amount of space in pt</param>
            /// <returns>config object</returns>
            public HeaderSettings SetContentSpacing(double distanceFromContent)
            {
                _space = distanceFromContent.ToString("0.##", CultureInfo.InvariantCulture);

                return this;
            }

            /// <summary>
            /// Sets the URL for the HTML document to use as a header/footer. The text are ignored in this case.
            /// </summary>
            /// <param name="htmlUri">URI for the document</param>
            /// <returns>config object</returns>
            public HeaderSettings SetHtmlContent(string htmlUri)
            {
                _htmlUrl = htmlUri;

                return this;
            }

            /// <summary>
            /// Sets up the supplied object config.
            /// </summary>
            /// <param name="config">config object pointer</param>
            /// <param name="prefix">property name prefix, must be either "header" or "footer"</param>
            internal void SetUpObjectConfig(IntPtr config, string prefix)
            {
                if (_fontSize != null)
                {
                    PechkinStatic.SetObjectSetting(config, prefix + "." + "fontSize", _fontSize);
                }
                if (_fontName != null)
                {
                    PechkinStatic.SetObjectSetting(config, prefix + "." + "fontName", _fontName);
                }
                if (_leftText != null)
                {
                    PechkinStatic.SetObjectSetting(config, prefix + "." + "left", _leftText);
                }
                if (_centerText != null)
                {
                    PechkinStatic.SetObjectSetting(config, prefix + "." + "center", _centerText);
                }
                if (_rightText != null)
                {
                    PechkinStatic.SetObjectSetting(config, prefix + "." + "right", _rightText);
                }
                if (_lineSeparator != null)
                {
                    PechkinStatic.SetObjectSetting(config, prefix + "." + "line", _lineSeparator);
                }
                if (_space != null)
                {
                    PechkinStatic.SetObjectSetting(config, prefix + "." + "space", _space);
                }
                if (_htmlUrl != null)
                {
                    PechkinStatic.SetObjectSetting(config, prefix + "." + "htmlUrl", _htmlUrl);
                }
            }
        }

        private string _tocUseDottedLines = "false"; // must be either "true" or "false"
        private string _tocCaption; // caption for table of content
        private string _tocCreateLinks = "true"; // should TOC entries link to the content
        private string _tocBackLinks = "false"; // create links from headings back to TOC
        private string _tocIndentation; // indentation value for leveling TOC entries, ex. "2em"
        private string _tocFontScale; // factor of font scaling for the deeper TOC level, ex. "0.8"

        private string _createToc = "false"; // create table of content in the document

        private string _includeInOutline; // include this document into outline and TOC generation
        private string _pagesCount; // count pages in this document for use in outline and TOC generation

        private string _tocXsl; // if it's not empty, this XSL stylesheet is used to convert XML outline into TOC, the page content is ignored

        private string _pageUri; // URL of filename of the page to convert, if "-" then input is read from stdin

        private string _useExternalLinks = "true"; // create external PDF links from external <a> tags in html
        private string _useLocalLinks = "true"; // create PDF links for local anchors in html
        private string _produceForms = "true"; // create PDF forms form html ones

        private string _loadUsername; // username to use in HTTPAuth
        private string _loadPassword; // password to use in HTTPAuth
        private string _loadJsDelay; // amount of time in ms to wait before printing the page
        private string _loadZoomFactor; // zoom factor, e.g. "2.2"
        private string _loadRepeatCustomHeaders = "true"; // repeat custom headers for every request
        private string _loadBlockLocalFileAccess = "false";
        private string _loadStopSlowScript = "true"; // kinda like in a browser
#if DEBUG
        private string _loadDebugJavascript = "true"; // forward javascript warnings and errors into into warning callback
        private string _loadErrorHandling = "abort"; // how we handle objects that are failed to load. "abort" stops conversion, "skip" just omits the object from the output, "ignore" tries to deal with damaged object
#else
        private string _loadDebugJavascript = "false"; // forward javascript warnings and errors into into warning callback
        private string _loadErrorHandling = "ignore"; // how we handle objects that are failed to load. "abort" stops conversion, "skip" just omits the object from the output, "ignore" tries to deal with damaged object
#endif
        private string _loadProxy; // string describing proxy in format http://username:password@host:port, http can be changed to socks5, see http://madalgo.au.dk/~jakobt/wkhtmltoxdoc/wkhtmltopdf-0.9.9-doc.html
        
        /*
        private List<string> _loadCustomHeaders; // not implemented in the C bindings yet
        private List<string> _loadCookies; // not implemented in the C bindings yet
        private List<string> _loadPost; // not implemented in the C bindings yet
        private List<string> _loadRunScript; // not implemented in the C bindings yet
        */

        private string _webPrintBackground = "false"; // print background image
        private string _webLoadImages = "true"; // load images in the document
        private string _webRunJavascript = "true"; // run javascript
        private string _webIntelligentShrinking = "true"; // fits more content in one page
        private string _webMinFontSize; // minimum font size in pt, e.g. "9"
        private string _webPrintMediaType = "true"; // use "print" media instead of "screen" media
        private string _webDefaultEncoding = "utf-8"; // encoding to use, if it's not specified properly
        private string _webUserStylesheetUri; // URL or filename for the user stylesheet
        private string _webEnablePlugins = "false"; // enable some sort of plugins, er...

        private readonly HeaderSettings _header = new HeaderSettings();
        private readonly HeaderSettings _footer = new HeaderSettings();

        public HeaderSettings Header
        {
            get { return _header; }
        }
        public HeaderSettings Footer
        {
            get { return _footer; }
        }

        /// <summary>
        /// Use dotted lines between ToC entry and page number.
        /// </summary>
        /// <param name="useDottedLinesForToc"></param>
        /// <returns>config object</returns>
        [Obsolete("Library has no proper implementation for ToC creation in c bingins. This setting is supported, but it has no effect.")]
        public ObjectConfig SetTocLinkDottedLines(bool useDottedLinesForToc)
        {
            _tocUseDottedLines = useDottedLinesForToc ? "true" : "false";

            return this;
        }

        [Obsolete("Library has no proper implementation for ToC creation in c bingins. This setting is supported, but it has no effect.")]
        public ObjectConfig SetTocCaption(string captionText)
        {
            _tocCaption = captionText;

            return this;
        }

        /// <summary>
        /// Set whether we should create a link from each TOC entry to the place in the document it points to.
        /// </summary>
        /// <param name="createLinksToContent"></param>
        /// <returns>config object</returns>
        [Obsolete("Library has no proper implementation for ToC creation in c bingins. This setting is supported, but it has no effect.")]
        public ObjectConfig SetTocCreateLinks(bool createLinksToContent)
        {
            _tocCreateLinks = createLinksToContent ? "true" : "false";

            return this;
        }

        /// <summary>
        /// Set whether we should create links from content back to the TOC.
        /// </summary>
        /// <param name="createLinksToToc"></param>
        /// <returns>config object</returns>
        [Obsolete("Library has no proper implementation for ToC creation in c bingins. This setting is supported, but it has no effect.")]
        public ObjectConfig SetTocCreateBackLinks(bool createLinksToToc)
        {
            _tocBackLinks = createLinksToToc ? "true" : "false";

            return this;
        }

        /// <summary>
        /// Sets TOC indentation value for leveling the entries.
        /// </summary>
        /// <param name="indent">indent size in hundredth of inches</param>
        /// <returns>config object</returns>
        [Obsolete("Library has no proper implementation for ToC creation in c bingins. This setting is supported, but it has no effect.")]
        public ObjectConfig SetTocIndentation(int indent)
        {
            _tocIndentation = (indent / 100.0).ToString("0.##", CultureInfo.InvariantCulture);

            return this;
        }

        /// <summary>
        /// Sets TOC indentation value for leveling the entries. Gives you more freedom.
        /// </summary>
        /// <param name="indent">indent size with the units, like "2em", or "1in", or "3pt"</param>
        /// <returns>config object</returns>
        [Obsolete("Library has no proper implementation for ToC creation in c bingins. This setting is supported, but it has no effect.")]
        public ObjectConfig SetTocIndentation(string indent)
        {
            _tocIndentation = indent;

            return this;
        }

        /// <summary>
        /// Sets TOC font scale for deeper TOC level links.
        /// </summary>
        /// <param name="scale">scale factor, recommended to be &lt;= 1.0</param>
        /// <returns>config object</returns>
        [Obsolete("Library has no proper implementation for ToC creation in c bingins. This setting is supported, but it has no effect.")]
        public ObjectConfig SetTocFontScale(double scale)
        {
            _tocFontScale = scale.ToString("0.##", CultureInfo.InvariantCulture);

            return this;
        }

        [Obsolete("Library has no proper implementation for ToC creation in c bingins. Turning this on will break the output.")]
        public ObjectConfig SetCreateToc(bool createToc)
        {
            // to fix this, see pdfcommanlineparser.cc:183 for the correct syntax

            _createToc = createToc ? "true" : "false";

            Tracer.Warn("T:" + Thread.CurrentThread.Name + " Table of content generation is turned on. The result may be not as expected");

            return this;
        }

        /// <summary>
        /// Specifies whether this document should be included in the outline
        /// </summary>
        /// <param name="includeDocumentInOutline"></param>
        /// <returns>config object</returns>
        public ObjectConfig SetIncludeInOutline(bool includeDocumentInOutline)
        {
            _includeInOutline = includeDocumentInOutline ? "true" : "false";

            return this;
        }

        /// <summary>
        /// Specifies whether his document's pages are counted for the TOC and outline generation.
        /// </summary>
        /// <param name="affectPageCounts"></param>
        /// <returns>object config</returns>
        public ObjectConfig SetAffectPageCounts(bool affectPageCounts)
        {
            _pagesCount = affectPageCounts ? "true" : "false";

            return this;
        }

        /// <summary>
        /// Sets TOC XSL stylesheet URL or filename. If it's not null, this stylesheet used with the outline XML file to form TOC and page content is ignored completely.
        /// </summary>
        /// <param name="tocXslStylesheetUri">uri of the TOC XSL stylesheet</param>
        /// <returns>config object</returns>
        public ObjectConfig SetTocXsl(string tocXslStylesheetUri)
        {
            _tocXsl = tocXslStylesheetUri;

            return this;
        }

        /// <summary>
        /// Sets page URL or filename that will be converted to PDF. If you want to convert from string or resource, use appropriate method of the Converter instead.
        /// </summary>
        /// <param name="pageUri">URI of the page</param>
        /// <returns>config object</returns>
        public ObjectConfig SetPageUri(string pageUri)
        {
            _pageUri = pageUri;

            return this;
        }

        public ObjectConfig SetCreateExternalLinks(bool createExternalPdfLinks)
        {
            _useExternalLinks = createExternalPdfLinks ? "true" : "false";

            return this;
        }

        public ObjectConfig SetCreateInternalLinks(bool createInternalPdfLinks)
        {
            _useLocalLinks = createInternalPdfLinks ? "true" : "false";

            return this;
        }

        /// <summary>
        /// Specify whether converter should produce PDF forms from HTML ones.
        /// </summary>
        /// <param name="createPdfForms"></param>
        /// <returns>config object</returns>
        public ObjectConfig SetCreateForms(bool createPdfForms)
        {
            _produceForms = createPdfForms ? "true" : "false";

            return this;
        }

        /// <summary>
        /// Sets HTTP Auth username used to load the content
        /// </summary>
        /// <param name="username"></param>
        /// <returns>config object</returns>
        public ObjectConfig SetHttpUsername(string username)
        {
            _loadUsername = username;

            return this;
        }

        /// <summary>
        /// Sets HTTP Auth password used to load the content
        /// </summary>
        /// <param name="password"></param>
        /// <returns>config object</returns>
        public ObjectConfig SetHttpPassword(string password)
        {
            _loadPassword = password;

            return this;
        }

        /// <summary>
        /// Sets HTTP Auth parameters used to load the content
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns>config object</returns>
        public ObjectConfig SetHttpAuth(string username, string password)
        {
            return SetHttpUsername(username).SetHttpPassword(password);
        }

        /// <summary>
        /// Sets render delay for the page. This timeout allows javascripts on the page to finish building the page.
        /// </summary>
        /// <param name="time">time of the delay in milliseconds</param>
        /// <returns>config object</returns>
        public ObjectConfig SetRenderDelay(int time)
        {
            _loadJsDelay = time.ToString();

            return this;
        }

        public ObjectConfig SetZoomFactor(double factor)
        {
            _loadZoomFactor = factor.ToString("0.##", CultureInfo.InvariantCulture);

            return this;
        }

        /// <summary>
        /// Specifies whether custom headers are sent only for the main page or for all the content we're requesting with the page.
        /// </summary>
        /// <param name="repeatCustomHeaders"></param>
        /// <returns>config object</returns>
        [Obsolete("Custom header functionality is not yet implemented in C bindings for the lib.")]
        public ObjectConfig SetRepeatCustomHeaders(bool repeatCustomHeaders)
        {
            _loadRepeatCustomHeaders = repeatCustomHeaders ? "true" : "false";

            return this;
        }

        /// <summary>
        /// Specifies whether file:/// urls are allowed.
        /// </summary>
        /// <param name="allowLocalContent"></param>
        /// <returns>config object</returns>
        public ObjectConfig SetAllowLocalContent(bool allowLocalContent)
        {
            _loadBlockLocalFileAccess = allowLocalContent ? "false" : "true";

            return this;
        }

        [Obsolete("Slow scripts are terminated regardless of this setting")]
        public ObjectConfig SetSlowScriptTermination(bool terminateSlowScripts)
        {
            _loadStopSlowScript = terminateSlowScripts ? "true" : "false";

            return this;
        }

        /// <summary>
        /// Allows to activate Javascript debug mode. In that mode all JS errors and warnings are sent to the Warning event handler.
        /// </summary>
        /// <param name="debugJavascript"></param>
        /// <returns>config object</returns>
        public ObjectConfig SetJavascriptDebugMode(bool debugJavascript)
        {
            _loadDebugJavascript = debugJavascript ? "true" : "false";

            return this;
        }

        public enum ContentErrorHandlingType { Abort, Skip, Ignore }

        /// <summary>
        /// Sets the content error handling policy. Abort stops converson on any error, Skip omits content with errors from output, Ignore tries to process errorneous content anyway.
        /// </summary>
        /// <param name="type">content error policy</param>
        /// <returns>config object</returns>
        public ObjectConfig SetErrorHandlingType(ContentErrorHandlingType type)
        {
            switch (type)
            {
                case ContentErrorHandlingType.Abort:
                    _loadErrorHandling = "abort";
                    break;

                case ContentErrorHandlingType.Skip:
                    _loadErrorHandling = "skip";
                    break;

                case ContentErrorHandlingType.Ignore:
                    _loadErrorHandling = "ignore";
                    break;
            }

            return this;
        }

        /// <summary>
        /// Sets proxy string that specifies the proxy to run any loading operation through.
        /// 
        /// Protocol can be either "http" or "socks5". Refer to the http://madalgo.au.dk/~jakobt/wkhtmltoxdoc/wkhtmltopdf-0.9.9-doc.html for more details
        /// </summary>
        /// <param name="proxyString">String desribing a proxy in the format protocol://username:password@host:port</param>
        /// <returns>config object</returns>
        public ObjectConfig SetProxyString(string proxyString)
        {
            _loadProxy = proxyString;

            return this;
        }

        public ObjectConfig SetPrintBackground(bool printBackground)
        {
            _webPrintBackground = printBackground ? "true" : "false";

            return this;
        }

        public ObjectConfig SetLoadImages(bool loadImages)
        {
            _webLoadImages = loadImages ? "true" : "false";

            return this;
        }

        public ObjectConfig SetRunJavascript(bool enableJavascript)
        {
            _webRunJavascript = enableJavascript ? "true" : "false";

            return this;
        }

        /// <summary>
        /// Allows to enable intelligent shrinking. This feature allows to fit the page content onto the paper by width.
        /// </summary>
        /// <param name="enableIntelligentShrinking"></param>
        /// <returns>config object</returns>
        public ObjectConfig SetIntelligentShrinking(bool enableIntelligentShrinking)
        {
            _webIntelligentShrinking = enableIntelligentShrinking ? "true" : "false";

            return this;
        }

        /// <summary>
        /// Sets minimum font size.
        /// </summary>
        /// <param name="minSize">size in pt</param>
        /// <returns>config object</returns>
        public ObjectConfig SetMinFontSize(double minSize)
        {
            _webMinFontSize = minSize.ToString("0.##", CultureInfo.InvariantCulture);

            return this;
        }

        /// <summary>
        /// If set, converter uses "screen" media type instead of "print".
        /// </summary>
        /// <param name="useScreenMediaType"></param>
        /// <returns>config object</returns>
        public ObjectConfig SetScreenMediaType(bool useScreenMediaType)
        {
            _webPrintMediaType = useScreenMediaType ? "false" : "true";

            return this;
        }

        /// <summary>
        /// Sets the fallback encoding for the conten that is used when no encoding is specified explicitly. Default is UTF-8.
        /// </summary>
        /// <param name="enc">fallback encoding</param>
        /// <returns>config object</returns>
        public ObjectConfig SetFallbackEncoding(Encoding enc)
        {
            _webDefaultEncoding = enc.EncodingName;

            return this;
        }

        /// <summary>
        /// Sets the user stylesheet URL or filename.
        /// </summary>
        /// <param name="userStylesheet"></param>
        /// <returns>config object</returns>
        public ObjectConfig SetUserStylesheetUri(string userStylesheet)
        {
            _webUserStylesheetUri = userStylesheet;

            return this;
        }

        [Obsolete("Documentation is very vague about what these plugins are and they have no meaning for us.")]
        public ObjectConfig SetEnablePlugins(bool enablePlugins)
        {
            _webEnablePlugins = enablePlugins ? "true" : "false";

            return this;
        }

        internal void SetUpObjectConfig(IntPtr config)
        {
            Tracer.Trace("T:" + Thread.CurrentThread.Name + " Setting up object config (many wkhtmltopdf_set_object_setting)");

            if (_tocUseDottedLines != null)
            {
                PechkinStatic.SetObjectSetting(config, "toc.useDottedLines", _tocUseDottedLines);
            }
            if (_tocCaption != null)
            {
                PechkinStatic.SetObjectSetting(config, "toc.captionText", _tocCaption);
            }
            if (_tocCreateLinks != null)
            {
                PechkinStatic.SetObjectSetting(config, "toc.forwardLinks", _tocCreateLinks);
            }
            if (_tocBackLinks != null)
            {
                PechkinStatic.SetObjectSetting(config, "toc.backLinks", _tocBackLinks);
            }
            if (_tocIndentation != null)
            {
                PechkinStatic.SetObjectSetting(config, "toc.indentation", _tocIndentation);
            }
            if (_tocFontScale != null)
            {
                PechkinStatic.SetObjectSetting(config, "toc.fontScale", _tocFontScale);
            }
            if (_createToc != null)
            {
                PechkinStatic.SetObjectSetting(config, "isTableOfContent", _createToc);
            }
            if (_includeInOutline != null)
            {
                PechkinStatic.SetObjectSetting(config, "includeInOutline", _includeInOutline);
            }
            if (_pagesCount != null)
            {
                PechkinStatic.SetObjectSetting(config, "pagesCount", _pagesCount);
            }
            if (_tocXsl != null)
            {
                PechkinStatic.SetObjectSetting(config, "tocXsl", _tocXsl);
            }
            if (_pageUri != null)
            {
                PechkinStatic.SetObjectSetting(config, "page", _pageUri);
            }
            if (_useExternalLinks != null)
            {
                PechkinStatic.SetObjectSetting(config, "useExternalLinks", _useExternalLinks);
            }
            if (_useLocalLinks != null)
            {
                PechkinStatic.SetObjectSetting(config, "useLocalLinks", _useLocalLinks);
            }
            if (_produceForms != null)
            {
                PechkinStatic.SetObjectSetting(config, "produceForms", _produceForms);
            }
            if (_loadUsername != null)
            {
                PechkinStatic.SetObjectSetting(config, "load.username", _loadUsername);
            }
            if (_loadPassword != null)
            {
                PechkinStatic.SetObjectSetting(config, "load.password", _loadPassword);
            }
            if (_loadJsDelay != null)
            {
                PechkinStatic.SetObjectSetting(config, "load.jsdelay", _loadJsDelay);
            }
            if (_loadZoomFactor != null)
            {
                PechkinStatic.SetObjectSetting(config, "load.zoomFactor", _loadZoomFactor);
            }
            if (_loadRepeatCustomHeaders != null)
            {
                PechkinStatic.SetObjectSetting(config, "load.repertCustomHeaders", _loadRepeatCustomHeaders);
            }
            if (_loadBlockLocalFileAccess != null)
            {
                PechkinStatic.SetObjectSetting(config, "load.blockLocalFileAccess", _loadBlockLocalFileAccess);
            }
            if (_loadStopSlowScript != null)
            {
                PechkinStatic.SetObjectSetting(config, "load.stopSlowScript", _loadStopSlowScript);
            }
            if (_loadDebugJavascript != null)
            {
                PechkinStatic.SetObjectSetting(config, "load.debugJavascript", _loadDebugJavascript);
            }
            if (_loadErrorHandling != null)
            {
                PechkinStatic.SetObjectSetting(config, "load.loadErrorHandling", _loadErrorHandling);
            }
            if (_loadProxy != null)
            {
                PechkinStatic.SetObjectSetting(config, "load.proxy", _loadProxy);
            }
            if (_webPrintBackground != null)
            {
                PechkinStatic.SetObjectSetting(config, "web.background", _webPrintBackground);
            }
            if (_webLoadImages != null)
            {
                PechkinStatic.SetObjectSetting(config, "web.loadImages", _webLoadImages);
            }
            if (_webRunJavascript != null)
            {
                PechkinStatic.SetObjectSetting(config, "web.enableJavascript", _webRunJavascript);
            }
            if (_webIntelligentShrinking != null)
            {
                PechkinStatic.SetObjectSetting(config, "web.enableIntelligentShrinking", _webIntelligentShrinking);
            }
            if (_webMinFontSize != null)
            {
                PechkinStatic.SetObjectSetting(config, "web.minimumFontSize", _webMinFontSize);
            }
            if (_webPrintMediaType != null)
            {
                PechkinStatic.SetObjectSetting(config, "web.printMediaType", _webPrintMediaType);
            }
            if (_webDefaultEncoding != null)
            {
                PechkinStatic.SetObjectSetting(config, "web.defaultEncoding", _webDefaultEncoding);
            }
            if (_webUserStylesheetUri != null)
            {
                PechkinStatic.SetObjectSetting(config, "web.userStyleSheet", _webUserStylesheetUri);
            }
            if (_webEnablePlugins != null)
            {
                PechkinStatic.SetObjectSetting(config, "web.enablePlugins", _webEnablePlugins);
            }

            _header.SetUpObjectConfig(config, "header");
            _footer.SetUpObjectConfig(config, "footer");
        }

        internal IntPtr CreateObjectConfig()
        {
            IntPtr config = PechkinStatic.CreateObjectSettings();

            SetUpObjectConfig(config);

            return config;
        }
    }
}
