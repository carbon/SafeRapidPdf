﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace SafeRapidPdf.Primitives
{
	public class PdfStream : PdfObject
	{
		private PdfStream(PdfDictionary dictionary, byte[] data)
		{
			IsContainer = true;
			StreamDictionary = dictionary;
			Data = data;
		}

		public static PdfStream Parse(PdfDictionary dictionary, Lexical.ILexer lexer)
		{
			lexer.Expects("stream");
			lexer.SkipEol(); // position to begin of stream data

			if (dictionary == null)
				throw new Exception("Parser error: stream needs a dictionary");

			IPdfObject lengthObject = dictionary["Length"];
			if (lengthObject == null)
				throw new Exception("Parser error: stream dictionary requires a Length entry");

			int length = 0;
			if (lengthObject is PdfIndirectReference)
			{
				PdfIndirectReference reference = lengthObject as PdfIndirectReference;
				
				PdfIndirectObject lenobj = lexer.IndirectReferenceResolver
					.GetObject(reference.ObjectNumber, reference.GenerationNumber);

				PdfNumeric len = lenobj.Object as PdfNumeric;
				length = int.Parse(len.ToString());
			}
			else
			{
				length = int.Parse(lengthObject.ToString());
			}

			var data = lexer.ReadBytes(length);
			lexer.Expects("endstream");

			return new PdfStream(dictionary, data);
		}

		public PdfObject StreamDictionary
		{
			get;
			private set;
		}

		public byte[] Data
		{
			get;
			private set;
		}

		public override ReadOnlyCollection<IPdfObject> Items
		{
			get
			{
				var list = new List<IPdfObject>();
				list.Add(StreamDictionary);
				return list.AsReadOnly();
			}
		}

		public override string ToString()
		{
			return "stream";
		}
	}
}
