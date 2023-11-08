namespace SqlParser.Ast
{

    /// <summary>
    /// Copy targets
    /// </summary>
    public abstract class CopyTarget : IWriteSql
    {
        /// <summary>
        /// Stdin copy target
        /// </summary>
        public class Stdin : CopyTarget { }
        /// <summary>
        /// Stdin copy target
        /// </summary>
        public class Stdout : CopyTarget { }
        /// <summary>
        /// File copy target
        /// </summary>
        /// <param name="FileName">File name</param>
        public class File : CopyTarget
        {
            public File(string FileName)
            {
                this.FileName = FileName;
            }
            public string FileName { get; set; }
        }
        /// <summary>
        /// Program copy target
        /// </summary>
        /// <param name="Comment">Comment value</param>
        public class Program : CopyTarget
        {
            public Program(string Comment)
            {
                this.Comment = Comment;
            }
            public string Comment { get; set; }
        }

        public void ToSql(SqlTextWriter writer)
        {
            switch (this)
            {
                case Stdin:
                    writer.Write("STDIN");
                    break;

                case Stdout:
                    writer.Write("STDOUT");
                    break;

                case File f:
                    writer.Write($"'{f.FileName.EscapeSingleQuoteString()}'");
                    break;

                case Program p:
                    writer.Write($"PROGRAM '{p.Comment.EscapeSingleQuoteString()}'");
                    break;
            }
        }
    }
    /// <summary>
    /// Copy options
    /// </summary>
    public abstract class CopyOption : IWriteSql
    {
        /// <summary>
        /// <example>
        /// <c>
        /// FORMAT format_name 
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="Name"></param>
        public class Format : CopyOption
        {
            public Format(Ident Name)
            {
                this.Name = Name;
            }
            public Ident Name { get; set; }
        }
        /// <summary>
        /// <example>
        /// <c>
        /// FREEZE [ boolean ] 
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="Frozen"></param>
        public class Freeze : CopyOption
        {
            public Freeze(bool Frozen)
            {
                this.Frozen = Frozen;
            }
            public bool Frozen { get; set; }
        }
        /// <summary>
        /// <example>
        /// <c>
        /// DELIMITER 'delimiter_character'
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="Character"></param>
        public class Delimiter : CopyOption
        {
            public Delimiter(char Character)
            {
                this.Character = Character;
            }
            public char Character { get; set; }
        }
        /// <summary>
        /// <example>
        /// <c>
        /// NULL 'null_string'
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="Value"></param>
        public class Null : CopyOption
        {
            public Null(string Value)
            {
                this.Value = Value;
            }
            public string Value { get; set; }
        }
        /// <summary>
        /// <example>
        /// <c>
        /// HEADER [ boolean ] 
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="HeaderValue"></param>
        public class Header : CopyOption
        {
            public Header(bool HeaderValue)
            {
                this.HeaderValue = HeaderValue;
            }
            public bool HeaderValue { get; set; }
        }
        /// <summary>
        /// <example>
        /// <c>
        /// QUOTE 'quote_character' 
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="Character"></param>
        public class Quote : CopyOption
        {
            public Quote(char Character)
            {
                this.Character = Character;
            }
            public char Character { get; set; }
        }
        /// <summary>
        /// <example>
        /// <c>
        /// ESCAPE 'escape_character'
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="Character"></param>
        public class Escape : CopyOption
        {
            public Escape(char Character)
            {
                this.Character = Character;
            }
            public char Character { get; set; }
        }
        /// <summary>
        /// <example>
        /// <c>
        /// FORCE_QUOTE { ( column_name [, ...] ) | * }
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="Names"></param>
        public class ForceQuote : CopyOption
        {
            public ForceQuote(Sequence<Ident> Names)
            {
                this.Names = Names;
            }
            public Sequence<Ident> Names { get; set; }
        }
        /// <summary>
        /// <example>
        /// <c>
        /// FORCE_NOT_NULL ( column_name [, ...] ) 
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="Names"></param>
        public class ForceNotNull : CopyOption
        {
            public ForceNotNull(Sequence<Ident> Names)
            {
                this.Names = Names;
            }
            public Sequence<Ident> Names { get; set; }
        }
        /// <summary>
        /// <example>
        /// <c>
        /// FORCE_NULL ( column_name [, ...] ) 
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="Names"></param>
        public class ForceNull : CopyOption
        {
            public ForceNull(Sequence<Ident> Names)
            {
                this.Names = Names;
            }
            public Sequence<Ident> Names { get; set; }
        }
        /// <summary>
        /// <example>
        /// <c>
        /// ENCODING 'encoding_name' 
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="Name"></param>
        public class Encoding : CopyOption
        {
            public Encoding(string Name)
            {
                this.Name = Name;
            }
            public string Name { get; set; }
        }

        public void ToSql(SqlTextWriter writer)
        {
            switch (this)
            {
                case Format f:
                    writer.WriteSql($"FORMAT {f.Name}");
                    break;

                case Freeze { Frozen: true }:
                    writer.Write("FREEZE TRUE");
                    break;

                case Freeze { Frozen: false }:
                    writer.Write("FREEZE FALSE");
                    break;

                case Delimiter d:
                    writer.WriteSql($"DELIMITER '{d.Character}'");
                    break;

                case Null n:
                    writer.Write($"NULL '{n.Value.EscapeSingleQuoteString()}'");
                    break;

                case Header { HeaderValue: true }:
                    writer.Write("HEADER");
                    break;

                case Header { HeaderValue: false }:
                    writer.Write("HEADER FALSE");
                    break;

                case Quote q:
                    writer.Write($"QUOTE '{q.Character}'");
                    break;

                case Escape e:
                    writer.Write($"ESCAPE '{e.Character}'");
                    break;

                case ForceQuote fq:
                    writer.WriteSql($"FORCE_QUOTE ({fq.Names})");
                    break;

                case ForceNotNull fnn:
                    writer.WriteSql($"FORCE_NOT_NULL ({fnn.Names})");
                    break;

                case ForceNull fn:
                    writer.WriteSql($"FORCE_NULL ({fn.Names})");
                    break;

                case Encoding en:
                    writer.Write($"ENCODING '{en.Name.EscapeSingleQuoteString()}'");
                    break;
            }
        }
    }
    /// <summary>
    /// Copy legacy options
    /// </summary>
    public abstract class CopyLegacyOption : IWriteSql
    {
        /// <summary>
        /// Binary copy option
        /// <example>
        /// <c>
        /// BINARY
        /// </c>
        /// </example>
        /// </summary>
        public class Binary : CopyLegacyOption { }

        /// <summary>
        /// Delimiter copy option
        /// <example>
        /// <c>
        /// DELIMITER [ AS ] 'delimiter_character'
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="Character">Character delimiter</param>
        public class Delimiter : CopyLegacyOption
        {
            public Delimiter(char Character)
            {
                this.Character = Character;
            }
            public char Character { get; set; }
        }

        /// <summary>
        /// Null copy option
        /// <example>
        /// <c>
        /// NULL [ AS ] 'null_string'
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="Value">String value</param>
        public class Null : CopyLegacyOption
        {
            public Null(string Value)
            {
                this.Value = Value;
            }
            public string Value { get; set; }
        }

        /// <summary>
        /// CSV copy option
        /// <example>
        /// <c>
        /// CSV ...
        /// </c>
        /// </example>
        /// </summary>
        /// <param name="Options">Legacy copy options</param>
        public class Csv : CopyLegacyOption
        {
            public Csv(Sequence<CopyLegacyCsvOption> Options)
            {
                this.Options = Options;
            }
            public Sequence<CopyLegacyCsvOption> Options { get; set; }
        }

        public void ToSql(SqlTextWriter writer)
        {
            switch (this)
            {
                case Binary:
                    writer.Write("BINARY");
                    break;

                case Delimiter d:
                    writer.Write($"DELIMITER '{d.Character}'");
                    break;

                case Null n:
                    writer.Write($"NULL '{n.Value.EscapeSingleQuoteString()}'");
                    break;

                case Csv c:
                    writer.Write("CSV ");
                    writer.WriteDelimited(c.Options, " ");
                    break;
            }
        }
    }

    public abstract class CopyLegacyCsvOption : IWriteSql
    {
        /// <summary>
        /// HEADER
        /// </summary>
        public class Header : CopyLegacyCsvOption { }
        /// <summary>
        /// QUOTE [ AS ] 'quote_character'
        /// </summary>
        /// <param name="Character"></param>
        public class Quote : CopyLegacyCsvOption
        {
            public Quote(char Character)
            {
                this.Character = Character;
            }
            public char Character { get; set; }
        }
        /// <summary>
        /// ESCAPE [ AS ] 'escape_character'
        /// </summary>
        /// <param name="Character"></param>
        public class Escape : CopyLegacyCsvOption
        {
            public Escape(char Character)
            {
                this.Character = Character;
            }
            public char Character { get; set; }
        }
        /// <summary>
        /// FORCE QUOTE { column_name [, ...] | * }
        /// </summary>
        /// <param name="Names"></param>
        public class ForceQuote : CopyLegacyCsvOption
        {
            public ForceQuote(Sequence<Ident> Names)
            {
                this.Names = Names;
            }
            public Sequence<Ident> Names { get; set; }
        }
        /// <summary>
        /// FORCE NOT NULL column_name [, ...]
        /// </summary>
        /// <param name="Names"></param>
        public class ForceNotNull : CopyLegacyCsvOption
        {
            public ForceNotNull(Sequence<Ident> Names)
            {
                this.Names = Names;
            }
            public Sequence<Ident> Names { get; set; }
        }

        public void ToSql(SqlTextWriter writer)
        {
            switch (this)
            {
                case Header:
                    writer.Write("HEADER");
                    break;

                case Quote q:
                    writer.Write($"QUOTE '{q.Character}'");
                    break;

                case Escape e:
                    writer.Write($"ESCAPE '{e.Character}'");
                    break;

                case ForceQuote fq:
                    writer.WriteSql($"FORCE QUOTE {fq.Names}");
                    break;

                case ForceNotNull fn:
                    writer.WriteSql($"FORCE NOT NULL {fn.Names}");
                    break;
            }
        }
    }
}