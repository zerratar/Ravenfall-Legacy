namespace SqlParser.Ast
{
	
	/// <summary>
	/// Transaction mode
	/// </summary>
	public abstract class TransactionMode : IWriteSql
	{
	    public class AccessMode : TransactionMode
	    {
	        public TransactionAccessMode TransactionAccessMode { get; set; }
	
	        public AccessMode(TransactionAccessMode TransactionAccessMode)
	        {
	            this.TransactionAccessMode = TransactionAccessMode;
	        }
	    }
	
	    public class IsolationLevel : TransactionMode
	    {
	        public TransactionIsolationLevel TransactionIsolationLevel { get; set; }
	
	        public IsolationLevel(TransactionIsolationLevel TransactionIsolationLevel)
	        {
	            this.TransactionIsolationLevel = TransactionIsolationLevel;
	        }
	    }
	
	    public void ToSql(SqlTextWriter writer)
	    {
	        if (this is AccessMode a)
	        {
	            writer.WriteSql($"{a.TransactionAccessMode}");
	        }
	        else if (this is IsolationLevel i)
	        {
	            writer.WriteSql($"ISOLATION LEVEL {i.TransactionIsolationLevel}");
	        }
	    }
	}
}