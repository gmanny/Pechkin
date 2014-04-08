Pechkin
=======

.NET Wrapper for [WkHtmlToPdf](http://github.com/antialize/wkhtmltopdf) DLL, a library that uses the Webkit engine to convert HTML pages to PDF. This fork supports .NET 2.0 and up, but unfortunately it will only run in 32-bit application pools as wkhtmltopdf does not come in a 64-bit flavor at this time.

New in 0.9.1
------
The five unmanaged DLLs that Pechkin depends upon have been packaged as *embedded resources* so you don't have to worry about messing around with pre- or post-build events to copy the files wherever they go in your solution. When the library is first accessed in the application lifetime, it will copy the embedded resources to the working directory (i.e., ```/bin```.)

TuesPechkin 0.9.1 is also now available as a *NuGet package* (see: https://www.nuget.org/packages/TuesPechkin/) for your convenience.

If you experience any problems using TuesPechkin, please be sure to create the issues on TuesPechkin and not the original Pechkin branch. I am so grateful to gmanny for his contribution but at this point I see the codebase evolving further away from the original and I would like to continue to support it for you (and myself! ;))


FAQ
---

### Q: Why does the produced PDF lack background images and colors? ###

**A:** By default, all backgrounds will be omitted from the document (note: this is similar to how Google Chrome operates when printing to PDF.)

You can override this setting by calling `SetPrintBackground(true)` on the `ObjectConfig` supplied with the HTML document to the `Convert()` method of the converter.

### Q: Do I need to install wkhtmltopdf on the machine for the library to work? ###

**A:** No. The latest version of the wkhtmltopdf DLL is included in the project (and in NuGet package) along with its dependencies, and those are all copied into the build output directory when the project is built.

### Q: How do I build the library from source? ###

**A:** To build the library from the code, you'll need Visual Studio 2010 with SP1 or Visual Studio 2012 with NuGet package manager [installed](http://docs.nuget.org/docs/start-here/installing-nuget). Then, after checking out the code and opening it in VS you should restore NuGet packages: right click on the solution, select **Manage NuGet Packages...** and in the opened window you should see notification that some packages are missing with the button that restores them.

[Alternatively](http://stackoverflow.com/questions/6876732/how-do-i-get-nuget-to-install-update-all-the-packages-in-the-packages-config) you can run ```
nuget install packages.config
``` for every project in the solution. (Two test projects with xunit, others supporting Common.Logging.)

And then you should be able to build everything with **Build** > **Build Solution** menu item.

Usage
-----

Pechkin is both easy to use....

```csharp
String html = "<html><body><h1>Hello world!</h1></body></html>";

byte[] pdfBuf = Factory.Create(new GlobalConfig()).Convert(html);
```

...and functional:

```csharp
// create global configuration object
GlobalConfig gc = new GlobalConfig();

// set it up using fluent notation
gc.SetMargins(new Margins(300, 100, 150, 100))
  .SetDocumentTitle("Test document")
  .SetPaperSize(PaperKind.Letter);
//... etc

// create converter
IPechkin pechkin = Factory.Create(gc);

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
  .SetLoadImages(false)
  .SetPageUri("http://google.com");
//... etc

// convert document
byte[] pdfBuf = pechkin.Convert(oc);
```

License
-------

This work, "TuesPechkin", is a derivative of "Pechkin" by gmanny (Slava Kolobaev) used under the Creative Commons Attribution 3.0 license. This work is made available under the terms of the Creative Commons Attribution 3.0 license (viewable at http://creativecommons.org/licenses/by/3.0/) by tuespetre (Derek Gray.)
