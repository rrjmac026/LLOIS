using System;
using System.Collections.Generic;
using System.Text;

namespace LLOIS;

using PdfSharp.Fonts;
using System.IO;
using System.Windows;

public class PdfFontResolver : IFontResolver
{
    public static void Apply() =>
        GlobalFontSettings.FontResolver = new PdfFontResolver();

    public string DefaultFontName => "Arial";

    public byte[] GetFont(string faceName)
    {
        var path = faceName switch
        {
            "Arial#b"  => @"C:\Windows\Fonts\arialbd.ttf",
            "Arial#bi" => @"C:\Windows\Fonts\arialbi.ttf",
            "Arial#i"  => @"C:\Windows\Fonts\ariali.ttf",
            _          => @"C:\Windows\Fonts\arial.ttf",
        };
        return File.ReadAllBytes(path);
    }

    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        var suffix = (isBold, isItalic) switch
        {
            (true,  true)  => "#bi",
            (true,  false) => "#b",
            (false, true)  => "#i",
            _              => ""
        };
        return new FontResolverInfo($"Arial{suffix}");
    }
}