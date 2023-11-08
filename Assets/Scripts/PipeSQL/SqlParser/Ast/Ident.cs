namespace SqlParser.Ast
{
	
	/// <summary>
	/// Named identifier
	/// </summary>
	/// <param name="Value">Name name value</param>
	/// <param name="QuoteStyle">Surrounding quotation style</param>
	public class Ident : IWriteSql
	{
	    public string Value { get; set; }
	    public char? QuoteStyle { get; set; }
	
	    public Ident(string Value, char? QuoteStyle = null)
	    {
	        this.Value = Value;
	        this.QuoteStyle = QuoteStyle;
	    }
	
	    /// <summary>
	    /// Implicit conversion from an Ident to a string.
	    /// </summary>
	    /// <param name="name">Ident string value</param>
	    public static implicit operator string(Ident name)
	    {
	        return name.ToString();
	    }
	    /// <summary>
	    /// Implicit conversion from a string to an Ident. Assumes
	    /// no surrounding quotation.
	    /// </summary>
	    /// <param name="value" >Unquoted Ident</param>
	    public static implicit operator Ident(string value)
	    {
	        return new Ident(value);
	    }
	
	    public override string ToString()
	    {
	        return this.ToSql();
	    }
	
	    /// <summary>
	    /// ToString pass through
	    /// </summary>
	    /// <param name="writer">Sql writer instance</param>
	    public void ToSql(SqlTextWriter writer)
	    {
	        switch (QuoteStyle)
	        {
	            case Symbols.DoubleQuote:
	            case Symbols.SingleQuote:
	            case Symbols.Backtick:
	
	                writer.WriteSql($"{QuoteStyle}{Value.EscapeQuotedString(QuoteStyle.Value)}{QuoteStyle}");
	                break;
	
	            case Symbols.SquareBracketOpen:
	                writer.Write($"[{Value}]");
	                break;
	
	            default:
	                writer.Write(Value);
	                break;
	        }
	    }
	}
	/// <summary>
	/// Name with named alias
	/// </summary>
	/// <param name="Name">Name identifier</param>
	/// <param name="Alias">Alias identifier</param>
	public class IdentWithAlias : IWriteSql, IElement
	{
	    public IdentWithAlias(Ident Name, Ident Alias)
	    {
	        this.Name = Name;
	        this.Alias = Alias;
	    }
	
	    public Ident Name { get; set; }
	    public Ident Alias { get; set; }
	
	    public void ToSql(SqlTextWriter writer)
	    {
	        writer.WriteSql($"{Name} AS {Alias}");
	    }
	}
}