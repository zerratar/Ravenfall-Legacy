using SqlParser.Ast;

namespace SqlParser.Ast
{

    /// <summary>
    /// SQL data types
    /// </summary>
    public abstract class DataType : IWriteSql
    {
        /// <summary>
        /// Data type with character length specificity
        /// </summary>
        public abstract class CharacterLengthDataType : DataType
        {
            public CharacterLengthDataType(CharacterLength? CharacterLength)
            {
                this.CharacterLength = CharacterLength;
            }

            public CharacterLength? CharacterLength { get; set; }

            protected void FormatCharacterStringType(SqlTextWriter writer, string sqlType, ulong? length)
            {
                writer.Write(sqlType);

                if (length != null)
                {
                    writer.Write($"({length})");
                }
            }
        }

        /// <summary>
        /// Data type with length specificity
        /// </summary>
        /// <param name="Length">Data type length</param>
        public abstract class LengthDataType : DataType
        {
            public ulong? Length { get; set; }

            public LengthDataType(ulong? Length = null)
            {
                this.Length = Length;
            }
            protected void FormatTypeWithOptionalLength(SqlTextWriter writer, string sqlType, ulong? length, bool unsigned = false)
            {
                writer.Write($"{sqlType}");

                if (length != null)
                {
                    writer.Write($"({length})");
                }
                if (unsigned)
                {
                    writer.Write(" UNSIGNED");
                }
            }
        }
        /// <summary>
        /// Data type with exact number specificity
        /// </summary>
        /// <param name="ExactNumberInfo"></param>
        public abstract class ExactNumberDataType : DataType
        {
            public ExactNumberDataType(ExactNumberInfo? ExactNumberInfo)
            {
                this.ExactNumberInfo = ExactNumberInfo;
            }

            public ExactNumberInfo? ExactNumberInfo { get; set; }
        }
        /// <summary>
        /// Data type with time zone information
        /// </summary>
        /// <param name="TimezoneInfo">Time zone info</param>
        /// <param name="Length"></param>
        public abstract class TimeZoneDataType : DataType
        {
            public TimeZoneDataType(TimezoneInfo TimezoneInfo, ulong? Length = null)
            {
                this.TimezoneInfo = TimezoneInfo;
                this.Length = Length;
            }

            public TimezoneInfo TimezoneInfo { get; set; }
            public ulong? Length { get; set; }

            protected void FormattedDatetimePrecisionAndTz(SqlTextWriter writer, string sqlType)
            {
                writer.Write($"{sqlType}");
                string length = null;

                if (Length != null)
                {
                    length = $"({Length})";
                }

                if (TimezoneInfo == TimezoneInfo.Tz)
                {
                    writer.WriteSql($"{TimezoneInfo}{length}");
                }
                else if (TimezoneInfo != TimezoneInfo.None)
                {
                    writer.WriteSql($"{length} {TimezoneInfo}");
                }
            }
        }
        /// <summary>
        /// Array data type
        /// </summary>
        /// <param name="DataType"></param>
        public class Array : DataType
        {
            public Array(DataType DataType)
            {
                this.DataType = DataType;
            }

            public DataType DataType { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                if (DataType is not None)
                {
                    writer.WriteSql($"{DataType}[]");
                }
                else
                {
                    writer.Write("ARRAY");
                }
            }
        }

        /// <summary>
        /// Big integer with optional display width e.g. BIGINT or BIGINT(20)
        /// </summary>
        /// <param name="Length">Length</param>
        public class BigInt : LengthDataType
        {
            public BigInt(ulong? Length = null) : base(Length) { }
            public override void ToSql(SqlTextWriter writer)
            {
                FormatTypeWithOptionalLength(writer, "BIGINT", Length);
            }
        }
        /// <summary>
        /// This is alias for `BigNumeric` type used in BigQuery
        ///
        /// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/data-types#decimal_types"/>
        /// </summary>
        /// <param ExactNumberInfo="Exact number"></param>
        public class BigNumeric : ExactNumberDataType
        {
            public BigNumeric(ExactNumberInfo ExactNumberInfo) : base(ExactNumberInfo) { }
            public override void ToSql(SqlTextWriter writer)
            {
                // ReSharper disable once StringLiteralTypo
                writer.WriteSql($"BIGNUMERIC{ExactNumberInfo}");
            }
        }
        /// <summary>
        /// Fixed-length binary type with optional length e.g.
        ///
        /// <see href="https://jakewheat.github.io/sql-overview/sql-2016-foundation-grammar.html#binary-string-type"/>
        /// <see href="https://learn.microsoft.com/pt-br/sql/t-sql/data-types/binary-and-varbinary-transact-sql?view=sql-server-ver16"/>
        /// </summary>
        /// <param name="Length">Length</param>
        public class Binary : LengthDataType
        {
            public Binary(ulong? Length = null) : base(Length) { }
            public override void ToSql(SqlTextWriter writer)
            {
                FormatTypeWithOptionalLength(writer, "BINARY", Length);
            }
        }
        /// <summary>
        /// Large binary object with optional length e.g. BLOB, BLOB(1000)
        /// 
        /// <see href="https://jakewheat.github.io/sql-overview/sql-2016-foundation-grammar.html#binary-large-object-string-type"/>
        /// <see href="https://docs.oracle.com/javadb/10.8.3.0/ref/rrefblob.html"/>
        /// </summary>
        public class Blob : LengthDataType
        {
            public Blob(ulong? Length = null) : base(Length) { }
            public override void ToSql(SqlTextWriter writer)
            {
                FormatTypeWithOptionalLength(writer, "BLOB", Length);
            }
        }
        /// <summary>
        /// Boolean data type
        /// </summary>
        public class Boolean : DataType
        {
            public override void ToSql(SqlTextWriter writer)
            {
                writer.Write("BOOLEAN");
            }
        }
        /// <summary>
        /// Binary string data type
        /// </summary>
        // ReSharper disable IdentifierTypo
        public class Bytea : DataType
        {
            public override void ToSql(SqlTextWriter writer)
            {
                // ReSharper disable once StringLiteralTypo
                writer.Write("BYTEA");
            }
        }
        /// <summary>
        /// Fixed-length char type e.g. CHAR(10)
        /// </summary>
        public class Char : CharacterLengthDataType
        {
            public Char(CharacterLength? CharacterLength = null) : base(CharacterLength) { }
            public override void ToSql(SqlTextWriter writer)
            {
                FormatCharacterStringType(writer, "CHAR", CharacterLength?.Length);
            }
        }
        /// <summary>
        /// Fixed-length character type e.g. CHARACTER(10)
        /// </summary>
        public class Character : CharacterLengthDataType
        {
            public Character(CharacterLength? CharacterLength = null) : base(CharacterLength) { }
            public override void ToSql(SqlTextWriter writer)
            {
                FormatCharacterStringType(writer, "CHARACTER", CharacterLength?.Length);
            }
        }
        /// <summary>
        /// Large character object with optional length e.g. CHARACTER LARGE OBJECT, CHARACTER LARGE OBJECT(1000)
        ///
        /// <see href="https://jakewheat.github.io/sql-overview/sql-2016-foundation-grammar.html#character-large-object-type"/>
        /// </summary>
        /// <param name="Length">Length</param>
        public class CharacterLargeObject : LengthDataType
        {
            public CharacterLargeObject(ulong? Length = null) : base(Length) { }
            public override void ToSql(SqlTextWriter writer)
            {
                FormatTypeWithOptionalLength(writer, "CHARACTER", Length);
            }
        }
        /// <summary>
        /// Character varying type e.g. CHARACTER VARYING(10)
        /// </summary>
        public class CharacterVarying : CharacterLengthDataType
        {
            public CharacterVarying(CharacterLength? CharacterLength = null) : base(CharacterLength) { }
            public override void ToSql(SqlTextWriter writer)
            {
                if (CharacterLength != null)
                {
                    FormatCharacterStringType(writer, "CHARACTER VARYING", CharacterLength.Length);
                }
            }
        }
        /// <summary>
        /// Large character object with optional length e.g. CHAR LARGE OBJECT, CHAR LARGE OBJECT(1000)
        ///
        /// <see href="https://jakewheat.github.io/sql-overview/sql-2016-foundation-grammar.html#character-large-object-type"/>
        /// </summary>
        /// <param name="Length">Length</param>
        public class CharLargeObject : LengthDataType
        {
            public CharLargeObject(ulong? Length = null) : base(Length) { }
            public override void ToSql(SqlTextWriter writer)
            {
                FormatTypeWithOptionalLength(writer, "CHARACTER LARGE OBJECT", Length);
            }
        }
        /// <summary>
        /// Char varying type e.g. CHAR VARYING(10)
        /// </summary>
        public class CharVarying : CharacterLengthDataType
        {
            public CharVarying(CharacterLength? CharacterLength = null) : base(CharacterLength) { }
            public override void ToSql(SqlTextWriter writer)
            {
                if (CharacterLength != null)
                {
                    FormatCharacterStringType(writer, "CHAR VARYING", CharacterLength.Length);
                }
            }
        }

        /// <summary>
        /// Large character object with optional length e.g. CLOB, CLOB(1000)
        ///
        /// <see href="https://jakewheat.github.io/sql-overview/sql-2016-foundation-grammar.html#character-large-object-type"/>
        /// <see href="https://docs.oracle.com/javadb/10.10.1.2/ref/rrefclob.html"/>
        /// </summary>
        public class Clob : LengthDataType
        {
            public Clob(ulong? Length = null) : base(Length) { }
            public override void ToSql(SqlTextWriter writer)
            {
                FormatTypeWithOptionalLength(writer, "CLOB", Length);
            }
        }
        /// <summary>
        /// Custom type such as enums
        /// </summary>
        public class Custom : DataType, IElement
        {
            public Custom(ObjectName Name, Sequence<string>? Values = null)
            {
                this.Name = Name;
                this.Values = Values;
            }

            public ObjectName Name { get; set; }
            public Sequence<string>? Values { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                if (Values.SafeAny())
                {
                    writer.WriteSql($"{Name}({Values})");
                }
                else
                {
                    Name.ToSql(writer);
                }
            }
        }
        /// <summary>
        /// Date data type
        /// </summary>
        public class Date : DataType
        {
            public override void ToSql(SqlTextWriter writer)
            {
                writer.Write("DATE");
            }
        }
        /// <summary>
        /// Datetime with optional time precision e.g. MySQL
        /// 
        /// <see href="https://dev.mysql.com/doc/refman/8.0/en/datetime.html"/>
        /// </summary>
        public class Datetime : LengthDataType
        {
            public Datetime(ulong? Length = null) : base(Length) { }
            public override void ToSql(SqlTextWriter writer)
            {
                FormatTypeWithOptionalLength(writer, "DATETIME", Length);
            }
        }
        /// <summary>
        /// Dec data type with optional precision and scale e.g. DEC(10,2): DataType
        ///
        /// <see href="https://jakewheat.github.io/sql-overview/sql-2016-foundation-grammar.html#exact-numeric-type"/>
        /// </summary>
        public class Dec : ExactNumberDataType
        {
            public Dec(ExactNumberInfo ExactNumberInfo) : base(ExactNumberInfo) { }
            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"DEC{ExactNumberInfo}");
            }
        }
        /// <summary>
        /// Decimal type with optional precision and scale e.g. DECIMAL(10,2)
        ///
        /// <see href="https://jakewheat.github.io/sql-overview/sql-2016-foundation-grammar.html#exact-numeric-type"/>
        /// </summary>
        public class Decimal : ExactNumberDataType
        {
            public Decimal(ExactNumberInfo ExactNumberInfo) : base(ExactNumberInfo) { }
            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"DECIMAL{ExactNumberInfo}");
            }
        }
        /// <summary>
        /// Double data type
        /// </summary>
        public class Double : DataType
        {
            public override void ToSql(SqlTextWriter writer)
            {
                writer.Write("DOUBLE");
            }
        }
        /// <summary>
        /// Double PRECISION e.g. standard, PostgreSql
        ///
        /// <see href="https://jakewheat.github.io/sql-overview/sql-2016-foundation-grammar.html#approximate-numeric-type"/>
        /// <see href="https://www.postgresql.org/docs/current/datatype-numeric.html"/>
        /// </summary>
        public class DoublePrecision : DataType
        {
            public override void ToSql(SqlTextWriter writer)
            {
                writer.Write("DOUBLE PRECISION");
            }
        }
        /// <summary>
        /// Enum data types 
        /// </summary>
        /// <param name="Values"></param>
        public class Enum : DataType
        {
            public Enum(Sequence<string> Values)
            {
                this.Values = Values;
            }

            public Sequence<string> Values { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.Write("ENUM(");
                for (var i = 0; i < Values.Count; i++)
                {
                    if (i > 0)
                    {
                        writer.Write(", ");
                    }
                    writer.Write($"'{Values[i].EscapeSingleQuoteString()}'");
                }
                writer.Write(")");
            }
        }
        /// <summary>
        /// Floating point with optional precision e.g. FLOAT(8)
        /// </summary>
        public class Float : LengthDataType
        {
            public Float(ulong? Length = null) : base(Length) { }
            public override void ToSql(SqlTextWriter writer)
            {
                FormatTypeWithOptionalLength(writer, "DATETIME", Length);
            }
        }
        /// <summary>
        /// Integer with optional display width e.g. INT or INT(11)
        /// <param name="Length">Length</param>
        /// </summary>
        public class Int : LengthDataType
        {
            public Int(ulong? Length = null) : base(Length) { }
            public override void ToSql(SqlTextWriter writer)
            {
                FormatTypeWithOptionalLength(writer, "INT", Length);
            }
        }
        /// <summary>
        /// Integer with optional display width e.g. INTEGER or INTEGER(11)
        /// </summary>
        public class Integer : LengthDataType
        {
            public Integer(ulong? Length = null) : base(Length) { }
            public override void ToSql(SqlTextWriter writer)
            {
                FormatTypeWithOptionalLength(writer, "INTEGER", Length);
            }
        }
        /// <summary>
        /// Interval data type
        /// </summary>
        public class Interval : DataType
        {
            public override void ToSql(SqlTextWriter writer)
            {
                writer.Write("INTERVAL");
            }
        }
        /// <summary>
        /// Join data type
        /// </summary>
        public class Json : DataType
        {
            public override void ToSql(SqlTextWriter writer)
            {
                writer.Write("JSON");
            }
        }
        /// <summary>
        /// MySQL medium integer ([1]) with optional display width e.g. MEDIUMINT or MEDIUMINT(5)
        ///
        /// <see href="https://dev.mysql.com/doc/refman/8.0/en/integer-types.html"/>
        /// </summary>
        public class MediumInt : LengthDataType
        {
            public MediumInt(ulong? Length = null) : base(Length) { }
            public override void ToSql(SqlTextWriter writer)
            {
                FormatTypeWithOptionalLength(writer, "MEDIUMINT", Length);
            }
        }
        /// <summary>
        /// Empty data type
        /// </summary>
        public class None : DataType
        {
            public override void ToSql(SqlTextWriter writer)
            {
            }
        }
        /// <summary>
        /// Numeric type with optional precision and scale e.g. NUMERIC(10,2) 
        ///
        /// <see href="https://jakewheat.github.io/sql-overview/sql-2016-foundation-grammar.html#exact-numeric-type"/>
        /// </summary>
        public class Numeric : ExactNumberDataType
        {
            public Numeric(ExactNumberInfo ExactNumberInfo) : base(ExactNumberInfo) { }
            public override void ToSql(SqlTextWriter writer)
            {
                writer.WriteSql($"NUMERIC{ExactNumberInfo}");
            }
        }
        /// <summary>
        /// Variable-length character type e.g. NVARCHAR(10)
        /// </summary>
        public class Nvarchar : LengthDataType
        {
            public Nvarchar(ulong? Length = null) : base(Length) { }
            public override void ToSql(SqlTextWriter writer)
            {
                FormatTypeWithOptionalLength(writer, "NVARCHAR", Length);
            }
        }
        /// <summary>
        /// Floating point e.g. REAL
        /// </summary>
        public class Real : DataType
        {
            public override void ToSql(SqlTextWriter writer)
            {
                writer.Write("REAL");
            }
        }
        /// <summary>
        /// Regclass used in postgresql serial
        /// </summary>
        public class Regclass : DataType
        {
            public override void ToSql(SqlTextWriter writer)
            {
                writer.Write("REGCLASS");
            }
        }
        /// <summary>
        /// Set data type
        /// </summary>
        public class Set : DataType
        {
            public Set(Sequence<string> Values)
            {
                this.Values = Values;
            }

            public Sequence<string> Values { get; set; }

            public override void ToSql(SqlTextWriter writer)
            {
                writer.Write("SET(");
                for (var i = 0; i < Values.Count; i++)
                {
                    if (i > 0)
                    {
                        writer.Write(", ");
                    }

                    writer.Write($"'{Values[i].EscapeSingleQuoteString()}'");
                }
                writer.Write(")");
            }
        }
        /// <summary>
        /// Small integer with optional display width e.g. SMALLINT or SMALLINT(5)
        /// </summary>
        public class SmallInt : LengthDataType
        {
            public SmallInt(ulong? Length = null) : base(Length) { }
            public override void ToSql(SqlTextWriter writer)
            {
                FormatTypeWithOptionalLength(writer, "SMALLINT", Length);
            }
        }
        /// <summary>
        /// string data type
        /// </summary>
        public class StringType : DataType
        {
            public override void ToSql(SqlTextWriter writer)
            {
                writer.Write("STRING");
            }
        }
        /// <summary>
        /// Text data type
        /// </summary>
        public class Text : DataType
        {
            public override void ToSql(SqlTextWriter writer)
            {
                writer.Write("TEXT");
            }
        }

        /// <summary>
        /// Time with optional time precision and time zone information
        ///
        /// <see href="https://jakewheat.github.io/sql-overview/sql-2016-foundation-grammar.html#datetime-type"/>
        /// </summary>
        public class Time : TimeZoneDataType
        {
            public Time(TimezoneInfo TimezoneInfo, ulong? When = null) : base(TimezoneInfo, When) { }
            public override void ToSql(SqlTextWriter writer)
            {
                FormattedDatetimePrecisionAndTz(writer, "TIME");
            }
        }

        /// <summary>
        /// Timestamp with optional time precision and time zone information
        ///
        /// <see href="https://jakewheat.github.io/sql-overview/sql-2016-foundation-grammar.html#datetime-type"/>
        /// </summary>
        public class Timestamp : TimeZoneDataType
        {
            public Timestamp(TimezoneInfo TimezoneInfo, ulong? When = null)
                : base(TimezoneInfo, When)
            {
            }
            public override void ToSql(SqlTextWriter writer)
            {
                FormattedDatetimePrecisionAndTz(writer, "TIMESTAMP");
            }
        }
        /// <summary>
        /// Tiny integer with optional display width e.g. TINYINT or TINYINT(3)
        /// </summary>
        public class TinyInt : LengthDataType
        {
            public TinyInt(ulong? Length = null) : base(Length) { }
            public override void ToSql(SqlTextWriter writer)
            {
                FormatTypeWithOptionalLength(writer, "TINYINT", Length);
            }
        }
        /// <summary>
        /// Unsigned big integer with optional display width e.g. BIGINT UNSIGNED or BIGINT(20) UNSIGNED
        /// </summary>
        public class UnsignedBigInt : LengthDataType
        {
            public UnsignedBigInt(ulong? Length = null) : base(Length) { }
            public override void ToSql(SqlTextWriter writer)
            {
                FormatTypeWithOptionalLength(writer, "BIGINT", Length, true);
            }
        }
        /// <summary>
        /// Unsigned integer with optional display width e.g. INT UNSIGNED or INT(11) UNSIGNED
        /// </summary>
        public class UnsignedInt : LengthDataType
        {
            public UnsignedInt(ulong? Length = null) : base(Length) { }
            public override void ToSql(SqlTextWriter writer)
            {
                FormatTypeWithOptionalLength(writer, "INT", Length, true);
            }
        }
        /// <summary>
        /// Unsigned integer with optional display width e.g. INTEGER UNSIGNED or INTEGER(11) UNSIGNED
        /// </summary>
        public class UnsignedInteger : LengthDataType
        {
            public UnsignedInteger(ulong? Length = null) : base(Length) { }
            public override void ToSql(SqlTextWriter writer)
            {
                FormatTypeWithOptionalLength(writer, "INT", Length, true);
            }
        }
        /// <summary>
        /// Unsigned medium integer ([1]) with optional display width e.g. MEDIUMINT UNSIGNED or MEDIUMINT(5) UNSIGNED
        ///
        /// <see href="https://dev.mysql.com/doc/refman/8.0/en/integer-types.html"/>
        /// </summary>
        public class UnsignedMediumInt : LengthDataType
        {
            public UnsignedMediumInt(ulong? Length = null) : base(Length) { }
            public override void ToSql(SqlTextWriter writer)
            {
                FormatTypeWithOptionalLength(writer, "MEDIUMINT", Length, true);
            }
        }
        /// <summary>
        /// Unsigned small integer with optional display width e.g. SMALLINT UNSIGNED or SMALLINT(5) UNSIGNED
        /// </summary>
        public class UnsignedSmallInt : LengthDataType
        {
            public UnsignedSmallInt(ulong? Length = null) : base(Length) { }
            public override void ToSql(SqlTextWriter writer)
            {
                FormatTypeWithOptionalLength(writer, "SMALLINT", Length, true);
            }
        }
        /// <summary>
        /// Unsigned tiny integer with optional display width e.g. TINYINT UNSIGNED or TINYINT(3) UNSIGNED
        /// </summary>
        public class UnsignedTinyInt : LengthDataType
        {
            public UnsignedTinyInt(ulong? Length = null) : base(Length) { }
            public override void ToSql(SqlTextWriter writer)
            {
                FormatTypeWithOptionalLength(writer, "TINYINT", Length, true);
            }
        }
        /// <summary>
        /// UUID data ype
        /// </summary>
        public class Uuid : DataType
        {
            public override void ToSql(SqlTextWriter writer)
            {
                writer.Write("UUID");
            }
        }
        /// <summary>
        /// Variable-length binary with optional length type
        ///
        /// <see href="https://jakewheat.github.io/sql-overview/sql-2016-foundation-grammar.html#binary-string-type"/>
        /// <see href="https://learn.microsoft.com/pt-br/sql/t-sql/data-types/binary-and-varbinary-transact-sql?view=sql-server-ver16"/>
        /// </summary>
        public class Varbinary : LengthDataType
        {
            public Varbinary(ulong? Length = null) : base(Length) { }
            public override void ToSql(SqlTextWriter writer)
            {
                FormatTypeWithOptionalLength(writer, "VARBINARY", Length);
            }
        }
        /// <summary>
        /// Variable-length character type e.g. VARCHAR(10)
        /// </summary>
        public class Varchar : CharacterLengthDataType
        {
            public Varchar(CharacterLength? CharacterLength = null) : base(CharacterLength) { }

            public override void ToSql(SqlTextWriter writer)
            {
                FormatCharacterStringType(writer, "VARCHAR", CharacterLength?.Length);
            }
        }

        public abstract void ToSql(SqlTextWriter writer);
    }
}