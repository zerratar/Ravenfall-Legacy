namespace SqlParser.Ast
{
	
	/// <summary>
	/// Hive-specific format
	/// </summary>
	public class HiveFormat : IElement
	{
	    public HiveRowFormat? RowFormat { get; internal set; }
	    public HiveIOFormat? Storage { get; internal set; }
	    public string Location { get; internal set; }
	}
	/// <summary>
	/// Hive row format
	/// </summary>
	public abstract class HiveRowFormat
	{
	    /// <summary>
	    /// Hive Serde row format
	    /// </summary>
	    /// <param name="Class">String class name</param>
	    public class Serde : HiveRowFormat
	    {
	        public Serde(string Class)
	        {
	            this.Class = Class;
	        }
	
	        public string Class { get; }
	    }
	    /// <summary>
	    /// Hive delimited row format
	    /// </summary>
	    public class Delimited : HiveRowFormat { }
	}
	/// <summary>
	/// Hive distribution style
	/// </summary>
	public abstract class HiveDistributionStyle : IElement
	{
	    /// <summary>
	    /// Hive partitioned distribution
	    /// </summary>
	    /// <param name="Columns"></param>
	    public class Partitioned : HiveDistributionStyle
	    {
	        public Partitioned(Sequence<ColumnDef> Columns)
	        {
	            this.Columns = Columns;
	        }
	
	        public Sequence<ColumnDef> Columns { get; }
	    }
	    /// <summary>
	    /// Hive clustered distribution
	    /// </summary>
	    public class Clustered : HiveDistributionStyle
	    {
	        public Sequence<Ident>? Columns { get; set; }
	        public Sequence<ColumnDef>? SortedBy { get; set; }
	        public int NumBuckets { get; set; }
	    }
	    /// <summary>
	    /// Hive skewed distribution
	    /// </summary>
	    public class Skewed : HiveDistributionStyle
	    {
	        public Skewed(Sequence<ColumnDef> Columns, Sequence<ColumnDef> On)
	        {
	            this.Columns = Columns;
	            this.On = On;
	        }
	        public bool StoredAsDirectories { get; set; }
	        public Sequence<ColumnDef> Columns { get; }
	        public Sequence<ColumnDef> On { get; }
	    }
	    /// <summary>
	    /// Hive no distribution style
	    /// </summary>
	    public class None : HiveDistributionStyle { }
	}
	
	/// <summary>
	/// Hive IO format
	/// </summary>
	// ReSharper disable once InconsistentNaming
	public abstract class HiveIOFormat
	{
	    /// <summary>
	    /// Hive IOF format
	    /// </summary>
	    // ReSharper disable once InconsistentNaming
	    public class IOF : HiveIOFormat, IElement
	    {
	        public IOF(SqlExpression InputFormat, SqlExpression OutputFormat)
	        {
	            this.InputFormat = InputFormat;
	            this.OutputFormat = OutputFormat;
	        }
	
	        public SqlExpression InputFormat { get; }
	        public SqlExpression OutputFormat { get; }
	    }
	    /// <summary>
	    /// Hive File IO format
	    /// </summary>
	    public class FileFormat : HiveIOFormat
	    {
	        public Ast.FileFormat Format { get; set; }
	    }
	}
	
}