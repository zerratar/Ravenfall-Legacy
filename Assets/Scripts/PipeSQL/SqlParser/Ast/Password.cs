namespace SqlParser.Ast
{
	
	public abstract class Password : IWriteSql
	{
	    public class ValidPassword : Password, IElement
	    {
	        public ValidPassword(SqlExpression Expression)
	        {
	            this.Expression = Expression;
	        }
	
	        public SqlExpression Expression { get; set; }
	    }
	
	    public class NullPassword : Password { }
	
	    public void ToSql(SqlTextWriter writer)
	    {
	        if (this is ValidPassword v)
	        {
	            writer.WriteSql($" PASSWORD {v.Expression}");
	        }
	        else if (this is NullPassword)
	        {
	            writer.WriteSql($" PASSWORD NULL");
	        }
	    }
	}
}