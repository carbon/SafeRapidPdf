﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

using SafeRapidPdf.Parsing;
using SafeRapidPdf.Services;

namespace SafeRapidPdf.Objects
{
    /// <summary>
    /// Represents the physical structure of a PDF. Contains the objects present
    /// in the file and allows direct retrieval of indirect references.
    /// The file itself is considered as a PDF object.
    /// </summary>
    public class PdfFile : IPdfObject, IIndirectReferenceResolver
    {
        private readonly Dictionary<(int, int), PdfIndirectObject> _indirectObjects = new Dictionary<(int, int), PdfIndirectObject>();

        private PdfFile(IReadOnlyList<IPdfObject> objects)
        {
            Items = objects;

            // build up the fast object lookup dictionary
            foreach (var obj in Items.OfType<PdfIndirectObject>())
            {
                InsertObject(obj);
            }

            SetResolver(this);
        }

        /// <summary>
        /// Gets the parsing time
        /// </summary>
        public TimeSpan ParsingTime { get; private set; }

        public string Version => Items[0].ToString();

        public IReadOnlyList<IPdfObject> Items { get; private set; }

        public string Text => "File";

        public bool IsContainer => true;

        public PdfObjectType ObjectType => PdfObjectType.File;

        public PdfXRef XRef { get; private set; }

        private void SetResolver(IPdfObject obj)
        {
            if (obj.IsContainer)
            {
                foreach (IPdfObject item in obj.Items)
                {
                    if (item is PdfIndirectReference iref)
                    {
                        iref.Resolver = this;
                    }
                    else
                    {
                        SetResolver(item);
                    }
                }
            }
        }

        public static PdfFile Parse(Stream reader, EventHandler<ProgressChangedEventArgs> progress = null)
        {
            progress?.Invoke(null, new ProgressChangedEventArgs(0, null));

            var watch = Stopwatch.StartNew();

            var lexer = new Lexer(reader);

            lexer.Expects("%"); // Ensure the first byte matches the PDF marker

            var objects = new List<IPdfObject>();

            PdfComment comment = PdfComment.Parse(lexer);

            if (!comment.Text.StartsWith("%PDF-", StringComparison.Ordinal))
            {
                throw new ParsingException("PDF header missing");
            }

            objects.Add(comment);

            bool lastObjectWasEOF = false;

            while (true)
            {
                var obj = PdfObject.ParseAny(lexer);

                if (obj is null)
                {
                    if (lastObjectWasEOF)
                    {
                        break;
                    }
                    else
                    {
                        throw new ParsingException("End of file reached without EOF marker");
                    }
                }

                objects.Add(obj);

                progress?.Invoke(null, new ProgressChangedEventArgs(lexer.Percentage, null));

                // a linearized or updated document might contain several EOF markers
                lastObjectWasEOF = obj is PdfComment cmt && cmt.IsEOF;
            }

            progress?.Invoke(null, new ProgressChangedEventArgs(100, null));
            watch.Stop();

            var file = new PdfFile(objects)
            {
                ParsingTime = watch.Elapsed
            };

            // copy over xref
            file.XRef = lexer.IndirectReferenceResolver.XRef;

            return file;
        }

        public static PdfFile Parse(string pdfFilePath, EventHandler<ProgressChangedEventArgs> progress = null)
        {
            using (Stream reader = File.Open(pdfFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return Parse(reader, progress);
            }
        }

        private void InsertObject(PdfIndirectObject obj)
        {
            if (obj is null)
                throw new Exception("This object must be an indirect object");

            var key = PdfXRef.BuildKey(obj.ObjectNumber, obj.GenerationNumber);

            _indirectObjects[key] = obj;
        }

        public PdfIndirectObject GetObject(int objectNumber, int generationNumber)
        {
            var key = PdfXRef.BuildKey(objectNumber, generationNumber);

            return _indirectObjects[key];
        }
    }
}
