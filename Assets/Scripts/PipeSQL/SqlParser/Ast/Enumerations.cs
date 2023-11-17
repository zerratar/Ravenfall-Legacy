namespace SqlParser.Ast
{
	
	public enum AddDropSync
	{
	    None,
	    Add,
	    Drop,
	    Sync
	}
	
	public enum AnalyzeFormat
	{
	    None,
	    Text,
	    Graphviz,
	    Json
	}
	
	public enum ArgMode
	{
	    None,
	    In,
	    Out,
	    InOut
	}
	
	public enum BinaryOperator
	{
	    None,
	    // Plus +
	    Plus,
	    // Minus -
	    Minus,
	    // Multiply *
	    Multiply,
	    // Divide /
	    Divide,
	    // Modulo %
	    Modulo,
	    // string concat ||
	    StringConcat,
	    // Greater than >
	    Gt,
	    // Less than <
	    Lt,
	    // Greater than or equal to >=
	    GtEq,
	    // Less than or equal to <=
	    LtEq,
	    // Spaceship <=>
	    Spaceship,
	    // Equal to =
	    Eq,
	    // Not equal to <>
	    NotEq,
	    // AND 
	    And,
	    // OR
	    Or,
	    // XOR
	    Xor,
	    // Bitwise OR
	    BitwiseOr,
	    // Bitwise AND
	    BitwiseAnd,
	    // Bitwise XOR
	    BitwiseXor,
	    // Postgres bitwise XOR
	    PGBitwiseXor,
	    // Postgres shift left <<
	    PGBitwiseShiftLeft,
	    // Postgres shift right >>
	    PGBitwiseShiftRight,
	    // Postgres Exp ^
	    PGExp,
	    // Postgres Regex Match ~
	    PGRegexMatch,
	    // Postgres Regex IMatch ~*
	    PGRegexIMatch,
	    // Postgres Regex not Match !~
	    PGRegexNotMatch,
	    // Postgres Regex not IMatch !~*
	    PGRegexNotIMatch,
	    // PostgreSQL-specific custom operator.
	    //
	    // https://www.postgresql.org/docs/current/sql-createoperator.html
	    PGCustomBinaryOperator,
	    // No binary operation
	}
	
	/// <summary>
	/// Possible units for characters, initially based on 2016 ANSI.
	///
	/// <see href="https://jakewheat.github.io/sql-overview/sql-2016-foundation-grammar.html#char-length-units"/>
	/// </summary>
	public enum CharLengthUnit
	{
	    None,
	    // CHARACTERS unit
	    Characters,
	    // OCTETS unit
	    Octets
	}
	
	public enum CommentObject
	{
	    Column,
	    Table
	}
	
	public enum ContextModifier
	{
	    // No context defined. Each dialect defines the default in this scenario.
	    None,
	    // `LOCAL` identifier, usually related to transactional states.
	    Local,
	    // `SESSION` identifier
	    Session
	}
	
	public enum DataLoadingOptionType
	{
	    String,
	    Boolean,
	    Enum
	}
	
	public enum DateTimeField
	{
	    None,
	    Year,
	    Month,
	    Week,
	    Day,
	    Date,
	    Hour,
	    Minute,
	    Second,
	    Century,
	    Decade,
	    Dow,
	    Doy,
	    Epoch,
	    Isodow,
	    Isoyear,
	    Julian,
	    Microsecond,
	    Microseconds,
	    // ReSharper disable once IdentifierTypo
	    Millenium,
	    Millennium,
	    Millisecond,
	    Milliseconds,
	    Nanosecond,
	    Nanoseconds,
	    Quarter,
	    Timezone,
	    TimezoneHour,
	    TimezoneMinute,
	    NoDateTime
	}
	
	public enum DiscardObject
	{
	    All,
	    Plans,
	    Sequences,
	    Temp
	}
	
	public enum FileFormat
	{
	    None,
	    TextFile,
	    SequenceFile,
	    Orc,
	    Parquet,
	    Avro,
	    RcFile,
	    JsonFile,
	}
	
	public enum FunctionBehavior
	{
	    Immutable,
	    Stable,
	    Volatile
	}
	
	public enum GeneratedAs
	{
	    Always,
	    ByDefault,
	    ExpStored
	}
	
	public enum IndexType
	{
	    None,
	    BTree,
	    Hash
	}
	
	public enum IsLateral
	{
	    Lateral,
	    NotLateral
	}
	
	public enum IsOptional
	{
	    Optional,
	    Mandatory
	}
	
	public enum JsonOperator
	{
	    None,
	    // -> keeps the value as json
	    Arrow,
	    // ->> keeps the value as text or int.
	    LongArrow,
	    // #> Extracts JSON sub-object at the specified path
	    HashArrow,
	    // #>> Extracts JSON sub-object at the specified path as text
	    HashLongArrow,
	    // : Colon is used by Snowflake (Which is similar to LongArrow)
	    Colon,
	    // jsonb @> jsonb -> boolean: Test whether left json contains the right json
	    AtArrow,
	    // jsonb <@ jsonb -> boolean: Test whether right json contains the left json
	    ArrowAt,
	    // jsonb #- text[] -> jsonb: Deletes the field or array element at the specified
	    // path, where path elements can be either field keys or array indexes.
	    HashMinus,
	    // jsonb @? jsonpath -> boolean: Does JSON path return any item for the specified
	    // JSON value?
	    AtQuestion,
	    // jsonb @@ jsonpath → boolean: Returns the result of a JSON path predicate check
	    // for the specified JSON value. Only the first item of the result is taken into
	    // account. If the result is not Boolean, then NULL is returned.
	    AtAt
	}
	
	public enum KeyOrIndexDisplay
	{
	    // Nothing to display
	    None,
	    // Display the KEY keyword
	    Key,
	    // Display the INDEX keyword
	    Index
	}
	
	public enum KillType
	{
	    None,
	    Connection,
	    Query,
	    Mutation
	}
	
	public enum LockType
	{
	    None,
	    Share,
	    Update
	}
	
	public enum NonBlock
	{
	    None,
	    Nowait,
	    SkipLocked
	}
	
	public enum ObjectType
	{
	    Table,
	    View,
	    Index,
	    Schema,
	    Role,
	    Sequence,
	    Stage
	}
	
	public enum OffsetRows
	{
	    None,
	    Row,
	    Rows
	}
	
	public enum OnCommit
	{
	    None,
	    DeleteRows,
	    PreserveRows,
	    Drop
	}
	
	public enum ReferentialAction
	{
	    None,
	    Restrict,
	    Cascade,
	    SetNull,
	    NoAction,
	    SetDefault
	}
	
	public enum SearchModifier
	{
	    None,
	    // IN NATURAL LANGUAGE MODE.
	    InNaturalLanguageMode,
	    // IN NATURAL LANGUAGE MODE WITH QUERY EXPANSION.
	    InNaturalLanguageModeWithQueryExpansion,
	    // IN BOOLEAN MODE.
	    InBooleanMode,
	    // WITH QUERY EXPANSION.
	    WithQueryExpansion
	}
	
	public enum SetOperator
	{
	    Union,
	    Except,
	    Intersect,
	    None
	}
	
	public enum SetQuantifier
	{
	    All,
	    Distinct,
	    None
	}
	
	public enum ShowCreateObject
	{
	    Event,
	    Function,
	    Procedure,
	    Table,
	    Trigger,
	    View
	}
	
	public enum SqliteOnConflict
	{
	    None,
	    Rollback,
	    Abort,
	    Fail,
	    Ignore,
	    Replace
	}
	
	public enum TimezoneInfo
	{
	    // No information about time zone. E.g., TIMESTAMP
	    None,
	    // Temporal type 'WITH TIME ZONE'. E.g., TIMESTAMP WITH TIME ZONE, [standard], [Oracle]
	    //
	    // [standard]: <see href="https://jakewheat.github.io/sql-overview/sql-2016-foundation-grammar.html#datetime-type"/>
	    // [Oracle]:<see href="https://docs.oracle.com/en/database/oracle/oracle-database/12.2/nlspg/datetime-data-types-and-time-zone-support.html#GUID-3F1C388E-C651-43D5-ADBC-1A49E5C2CA05"/>
	    WithTimeZone,
	    // Temporal type 'WITHOUT TIME ZONE'. E.g., TIME WITHOUT TIME ZONE, [standard], [Postgresql]
	    //
	    // [standard]: <see href="https://jakewheat.github.io/sql-overview/sql-2016-foundation-grammar.html#datetime-type"/>
	    // [Postgresql]:<see href="https://www.postgresql.org/docs/current/datatype-datetime.html"/> 
	    WithoutTimeZone,
	    // Postgresql specific WITH TIME ZONE formatting, for both TIME and TIMESTAMP. E.g., TIMETZ, [Postgresql]
	    //
	    // [Postgresql]: <see href="https://www.postgresql.org/docs/current/datatype-datetime.html"/>
	    Tz
	}
	
	public enum TransactionAccessMode
	{
	    ReadOnly,
	    ReadWrite
	}
	
	public enum TransactionIsolationLevel
	{
	    ReadUncommitted,
	    ReadCommitted,
	    RepeatableRead,
	    Serializable
	}
	
	public enum TrimWhereField
	{
	    None,
	    Both,
	    Leading,
	    Trailing
	}
	// ReSharper disable InconsistentNaming
	public enum UnaryOperator
	{
	    Plus,
	    Minus,
	    Not,
	    // Bitwise Not, e.g. ~9 (PostgreSQL-specific)
	    PGBitwiseNot,
	    // Square root, e.g. |/9 (PostgreSQL-specific)
	    PGSquareRoot,
	    // Cube root, e.g. ||/27 (PostgreSQL-specific)
	    PGCubeRoot,
	    // Factorial, e.g. 9! (PostgreSQL-specific)
	    PGPostfixFactorial,
	    // Factorial, e.g. !!9 (PostgreSQL-specific)
	    PGPrefixFactorial,
	    // Absolute value, e.g. @ -9 (PostgreSQL-specific)
	    PGAbs
	}
	// ReSharper restore InconsistentNaming
	
	public enum WindowFrameUnit
	{
	    Rows,
	    Range,
	    Groups
	}
	
}