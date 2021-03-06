﻿namespace SafeRapidPdf.Objects
{
    public sealed class PdfNull : PdfObject
    {
        public static readonly PdfNull Null = new PdfNull();

        private PdfNull()
            : base(PdfObjectType.Null)
        {
        }

        public static PdfNull Parse(Parsing.Lexer lexer)
        {
            lexer.Expects("null");
            return Null;
        }

        public override string ToString() => "null";
    }
}