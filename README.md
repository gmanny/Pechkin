Pechkin
=======

.NET Wrapper for [WkHtmlToPdf](http://github.com/antialize/wkhtmltopdf) DLL, library that uses Webkit engine to convert HTML pages to PDF.

FAQ
---

### Q: Why produced PDF lacks background images and colors? ###

**A:** By dafault, all backgrounds will be ommited from the document.

You can override this setting by calling `SetPrintBackground(true)` on the `ObjectConfig` supplied with the HTML document to the `Convert()` method of the converter.

NuGet
-----

Pechkin is available in NuGet repo: in most cases you should use [SynchronizedPechkin](https://nuget.org/packages/Pechkin.Synchronized) as it protects multithreaded code from crashing the lib. But for simple usage from one thread, you can use [SimplePechkin](https://nuget.org/packages/Pechkin) directly.

Usage
-----

Pechkin is both easy to use

```csharp
byte[] pdfBuf = new SimplePechkin(new GlobalConfig()).Convert("<html><body><h1>Hello world!</h1></body></html>");
```

and functional

```csharp
// create global configuration object
GlobalConfig gc = new GlobalConfig();

// set it up using fluent notation
gc.SetMargins(new Margins(300, 100, 150, 100))
  .SetDocumentTitle("Test document")
  .SetPaperSize(PaperKind.Letter);
//... etc

// create converter
IPechkin pechkin = new SynchronizedPechkin(gc);

// subscribe to events
pechkin.Begin += OnBegin;
pechkin.Error += OnError;
pechkin.Warning += OnWarning;
pechkin.PhaseChanged += OnPhase;
pechkin.ProgressChanged += OnProgress;
pechkin.Finished += OnFinished;

// create document configuration object
ObjectConfig oc = new ObjectConfig();

// and set it up using fluent notation too
oc.SetCreateExternalLinks(false)
  .SetFallbackEncoding(Encoding.ASCII)
  .SetLoadImages(false);
  .SetPageUri("http://google.com");
//... etc

// convert document
byte[] pdfBuf = pechkin.Convert(oc);
```