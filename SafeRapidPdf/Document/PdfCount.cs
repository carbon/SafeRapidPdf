﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SafeRapidPdf.File;

namespace SafeRapidPdf.Document
{
	public class PdfCount : PdfBaseObject
	{
		public PdfCount(PdfNumeric count)
			: base(PdfObjectType.Count)
		{
			Value = Convert.ToInt32(count.Value);
		}

		public int Value
		{
			get;
			private set;
		}

		public override string ToString()
		{
			return String.Format("Count : {0}", Value);
		}
	}
}
