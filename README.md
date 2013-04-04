Pechkin
=======

.NET Wrapper for [WkHtmlToPdf](http://github.com/antialize/wkhtmltopdf) DLL, library that uses Webkit engine to convert HTML pages to PDF.

FAQ
---

### Q: Why produced PDF lacks background images and colors? ###

**A:** By dafault, all backgrounds will be ommited from the document.

You can override this setting by calling `SetPrintBackground(true)` on the `ObjectConfig` supplied with the HTML document to the `Convert()` method of the converter.

### Q: Do I need to run wkhtmltox installer on the machine for the library to work? ###

**A:** No, latest version of wkhtmltox DLL is included in the project (and in NuGet package) along with its dependencies, and copied into build folder on project build.

So there's no need to install any prerequisites before using the library on the computer.

### Q: How to build library from the code? ###

**A:** To build the library from the code, you'll need Visual Studio 2010 with NuGet package manager [installed](http://docs.nuget.org/docs/start-here/installing-nuget). Then, after checking out the code and opening it in VS you should restore NuGet packages: right click on the solution, select **Manage NuGet Packages...** and in the opened window you should see notification that some packages are missing with the button that restores them.

[Alternatively](http://stackoverflow.com/questions/6876732/how-do-i-get-nuget-to-install-update-all-the-packages-in-the-packages-config) you can run ```
nuget install packages.config
``` for every project in the solution. (Two test projects with xunit, others supporting Common.Logging.)

And then you should be able to build everything with **Build** > **Build Solution** menu item.

### Q: Why my Web App hangs on the "easy to use" code example below? ###

**A:** In Web applications new thread is typically created for each request processed. `SimplePechkin` is designed to work only within one thread, even if you create another object for every thread, so you should use `SynchronizedPechkin` instead. Just install another NuGet package and change every occurence of `SimplePechkin` to `SynchronizedPechkin`, that's it.

I will implement detection of multiple thread use of `SimplePechkin` in the future and there will be exception thrown in that case.

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
  .SetLoadImages(false)
  .SetPageUri("http://google.com");
//... etc

// convert document
byte[] pdfBuf = pechkin.Convert(oc);
```

License
-------

Copyright 2012 by Slava Kolobaev. This work is made available under the terms of the Creative Commons Attribution 3.0 license, http://creativecommons.org/licenses/by/3.0/
