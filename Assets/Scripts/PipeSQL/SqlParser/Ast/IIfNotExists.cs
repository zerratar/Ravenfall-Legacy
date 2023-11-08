namespace SqlParser.Ast
{
	
	public interface IIfNotExists
	{
	    bool IfNotExists { get; set; }
	
	    string IfNotExistsText { get; }
	}
}