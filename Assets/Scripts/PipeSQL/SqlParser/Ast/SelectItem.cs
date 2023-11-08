namespace SqlParser.Ast
{
	
	/// <summary>
	/// One item of the comma-separated list following SELECT
	/// </summary>
	public abstract class SelectItem : IWriteSql, IElement
	{
	    /// <summary>
	    ///  Any expression, not followed by [ AS ] alias
	    /// </summary>
	    /// <param name="Expression">Select expression</param>
	    public class UnnamedExpression : SelectItem
	    {
	        public UnnamedExpression(SqlExpression Expression)
	        {
	            this.Expression = Expression;
	        }
	
	        public SqlExpression Expression { get; }
	
	        public override void ToSql(SqlTextWriter writer)
	        {
	            Expression.ToSql(writer);
	        }
	    }
	    /// <summary>
	    /// alias.* or even schema.table.*
	    /// </summary>
	    /// <param name="Name">Object name</param>
	    /// <param name="WildcardAdditionalOptions">Select options</param>
	    public class QualifiedWildcard : SelectItem
	    {
	        public QualifiedWildcard(ObjectName Name, WildcardAdditionalOptions WildcardAdditionalOptions)
	        {
	            this.Name = Name;
	            this.WildcardAdditionalOptions = WildcardAdditionalOptions;
	        }
	
	        public ObjectName Name { get; }
	        public WildcardAdditionalOptions WildcardAdditionalOptions { get; }
	
	        public override void ToSql(SqlTextWriter writer)
	        {
	            writer.WriteSql($"{Name}.*{WildcardAdditionalOptions}");
	        }
	    }
	    /// <summary>
	    /// An expression, followed by [ AS ] alias
	    /// </summary>
	    /// <param name="Expression">Select expression</param>
	    /// <param name="Alias">Select alias</param>
	    public class ExpressionWithAlias : SelectItem
	    {
	        public ExpressionWithAlias(SqlExpression Expression, Ident Alias)
	        {
	            this.Expression = Expression;
	            this.Alias = Alias;
	        }
	
	        public SqlExpression Expression { get; }
	        public Ident Alias { get; }
	
	        public override void ToSql(SqlTextWriter writer)
	        {
	            writer.WriteSql($"{Expression} AS {Alias}");
	        }
	    }
	    /// <summary>
	    /// An unqualified *
	    /// </summary>
	    /// <param name="WildcardAdditionalOptions"></param>
	    public class Wildcard : SelectItem
	    {
	        public Wildcard(WildcardAdditionalOptions WildcardAdditionalOptions)
	        {
	            this.WildcardAdditionalOptions = WildcardAdditionalOptions;
	        }
	
	        public WildcardAdditionalOptions WildcardAdditionalOptions { get; }
	
	        public override void ToSql(SqlTextWriter writer)
	        {
	            writer.WriteSql($"*{WildcardAdditionalOptions}");
	        }
	    }
	
	    public abstract void ToSql(SqlTextWriter writer);
	
	    public T As<T>() where T : SelectItem
	    {
	        return (T)this;
	    }
	
	    public UnnamedExpression AsUnnamed()
	    {
	        return As<UnnamedExpression>();
	    }
	
	    public Wildcard AsWildcard()
	    {
	        return As<Wildcard>();
	    }
	}
	
	/// <summary>
	/// Excluded select item
	/// </summary>
	public abstract class ExcludeSelectItem : IWriteSql, IElement
	{
	    /// <summary>
	    /// Single exclusion
	    /// </summary>
	    /// <param name="Name">Name identifier</param>
	    public class Single : ExcludeSelectItem
	    {
	        public Single(Ident Name)
	        {
	            this.Name = Name;
	        }
	
	        public Ident Name { get; }
	
	        public override void ToSql(SqlTextWriter writer)
	        {
	            writer.WriteSql($"EXCLUDE {Name}");
	        }
	    }
	    /// <summary>
	    /// Multiple exclusions
	    /// </summary>
	    /// <param name="Columns">Name identifiers</param>
	    public class Multiple : ExcludeSelectItem
	    {
	        public Multiple(Sequence<Ident> Columns)
	        {
	            this.Columns = Columns;
	        }
	
	        public Sequence<Ident> Columns { get; }
	
	        public override void ToSql(SqlTextWriter writer)
	        {
	            writer.WriteSql($"EXCLUDE ({Columns})");
	        }
	    }
	
	    public abstract void ToSql(SqlTextWriter writer);
	}
	
	/// <summary>
	/// Rename select item
	/// </summary>
	public abstract class RenameSelectItem : IWriteSql, IElement
	{
	    /// <summary>
	    /// Single rename
	    /// </summary>
	    /// <param name="Name">Name identifier</param>
	    public class Single : RenameSelectItem
	    {
	        public Single(IdentWithAlias Name)
	        {
	            this.Name = Name;
	        }
	
	        public IdentWithAlias Name { get; }
	
	        public override void ToSql(SqlTextWriter writer)
	        {
	            writer.WriteSql($"RENAME {Name}");
	        }
	    }
	    /// <summary>
	    /// Multiple exclusions
	    /// </summary>
	    /// <param name="Columns">Name identifiers</param>
	    public class Multiple : RenameSelectItem
	    {
	        public Multiple(Sequence<IdentWithAlias> Columns)
	        {
	            this.Columns = Columns;
	        }
	
	        public Sequence<IdentWithAlias> Columns { get; }
	
	        public override void ToSql(SqlTextWriter writer)
	        {
	            writer.WriteSql($"RENAME ({Columns})");
	        }
	    }
	
	    public abstract void ToSql(SqlTextWriter writer);
	}
	
	/// <summary>
	/// Expected select item
	/// </summary>
	/// <param name="FirstElement">First item in the list</param>
	/// <param name="AdditionalElements">Additional items</param>
	public class ExceptSelectItem : IWriteSql, IElement
	{
	    public ExceptSelectItem(Ident FirstElement, Sequence<Ident> AdditionalElements)
	    {
	        this.FirstElement = FirstElement;
	        this.AdditionalElements = AdditionalElements;
	    }
	
	    public Ident FirstElement { get; }
	    public Sequence<Ident> AdditionalElements { get; }
	
	    public void ToSql(SqlTextWriter writer)
	    {
	        writer.Write("EXCEPT ");
	
	        if (AdditionalElements.SafeAny())
	        {
	            writer.WriteSql($"({FirstElement}, {AdditionalElements})");
	        }
	        else
	        {
	            writer.WriteSql($"({FirstElement})");
	        }
	    }
	}
	
}