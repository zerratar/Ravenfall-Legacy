﻿namespace SqlParser.Ast
{

    /// <summary>
    /// Represents a table with joins and table relationships
    /// </summary>
    /// <param name="Relation">Relation table factor</param>
    public class TableWithJoins : IWriteSql, IElement
    {

        public TableWithJoins(TableFactor Relation)
        {
            this.Relation = Relation;
        }

        [Visit(0)] public TableFactor? Relation { get; set; }

        [Visit(1)] public Sequence<Join>? Joins { get; set; }

        public void ToSql(SqlTextWriter writer)
        {
            Relation?.ToSql(writer);

            if (Joins.SafeAny())
            {
                writer.WriteList(Joins);
            }
        }
    }
}