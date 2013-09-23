using System;
using Pechkin.EventHandlers;

namespace Pechkin
{
    public interface IPechkin : IDisposable
    {
        /// <summary>
        /// Runs conversion process.
        /// 
        /// Allows to convert both external HTML resource and HTML string.
        /// </summary>
        /// <param name="doc">document parameters</param>
        /// <param name="html">document body, ignored if <code>ObjectConfig.SetPageUri</code> is set</param>
        /// <returns>PDF document body</returns>
        byte[] Convert(ObjectConfig doc, string html);

        /// <summary>
        /// Runs conversion process.
        /// 
        /// Allows to convert both external HTML resource and HTML string.
        /// 
        /// Takes html source as a byte array for when you don't know the encoding.
        /// </summary>
        /// <param name="doc">document parameters</param>
        /// <param name="html">document body, ignored if <code>ObjectConfig.SetPageUri</code> is set</param>
        /// <returns>PDF document body</returns>
        byte[] Convert(ObjectConfig doc, byte[] html);

        /// <summary>
        /// Converts external HTML resource into PDF.
        /// </summary>
        /// <param name="doc">document parameters, <code>ObjectConfig.SetPageUri</code> should be set</param>
        /// <returns>PDF document body</returns>
        byte[] Convert(ObjectConfig doc);

        /// <summary>
        /// Converts HTML string to PDF with default settings.
        /// </summary>
        /// <param name="html">HTML string</param>
        /// <returns>PDF document body</returns>
        byte[] Convert(string html);

        /// <summary>
        /// Converts HTML string to PDF with default settings.
        /// 
        /// Takes html source as a byte array for when you don't know the encoding.
        /// </summary>
        /// <param name="html">HTML string</param>
        /// <returns>PDF document body</returns>
        byte[] Convert(byte[] html);

        /// <summary>
        /// Converts HTML page at specified URL to PDF with default settings.
        /// </summary>
        /// <param name="url">url of page, can be either http/https or file link</param>
        /// <returns>PDF document body</returns>
        byte[] Convert(Uri url);

        /// <summary>
        /// This event happens every time the conversion starts
        /// </summary>
        event BeginEventHandler Begin;

        /// <summary>
        /// This event handler is called whenever warning happens during conversion process.
        /// 
        /// You can also see javascript errors and warnings if you enable <code>SetJavascriptDebugMode</code> in <code>ObjectConfig</code>
        /// </summary>
        event WarningEventHandler Warning;

        /// <summary>
        /// Event handler is called whenever error happens during conversion process.
        /// 
        /// Error typically means that conversion will be terminated.
        /// </summary>
        event ErrorEventHandler Error;

        /// <summary>
        /// This event handler signals phase change of the conversion process.
        /// </summary>
        event PhaseChangedEventHandler PhaseChanged;

        /// <summary>
        /// This event handler signals progress change of the conversion process.
        /// 
        /// Number of percents is included in text description.
        /// </summary>
        event ProgressChangedEventHandler ProgressChanged;

        /// <summary>
        /// This event handler is fired when conversion is finished.
        /// </summary>
        event FinishEventHandler Finished;

        event DisposedEventHandler Disposed;

        bool IsDisposed { get; }

        /// <summary>
        /// Current phase number for the converter.
        /// 
        /// Intended for use in event handlers.
        /// </summary>
        int CurrentPhase { get; }

        /// <summary>
        /// Phase count for the current conversion process.
        /// 
        /// Intended for use in event handlers.
        /// </summary>
        int PhaseCount { get; }

        /// <summary>
        /// Current phase string description for the converter.
        /// 
        /// Intended for use in event handlers.
        /// </summary>
        string PhaseDescription { get; }

        /// <summary>
        /// Current progress string description. It includes percent count, btw.
        /// 
        /// Intended for use in event handlers.
        /// </summary>
        string ProgressString { get; }

        /// <summary>
        /// Error code returned by server when converter tried to request the page or the resource. Should be available after failed conversion attempt.
        /// </summary>
        int HttpErrorCode { get; }
    }
}
