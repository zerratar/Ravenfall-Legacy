using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System;
using System.Linq;

namespace RavenfallDataPipe
{
    public class QueryEngineContext
    {
        private readonly ConcurrentDictionary<string, SqlPipeDataContextTable> tables
            = new ConcurrentDictionary<string, SqlPipeDataContextTable>();

        public void Register<T>(string name, Func<IEnumerable<T>> getDataSource, params SqlPipeDataContextRowColumn<T>[] columns)
        {
            tables[name] = new SqlPipeDataContextTable<T>(name, getDataSource, columns);
        }

        public static SqlPipeDataContextRowColumn<T, T2> Column<T, T2>(string header, Func<T, T2> getValue)
        {
            return new SqlPipeDataContextRowColumn<T, T2>(header, getValue);
        }

        public IReadOnlyList<SqlPipeDataContextTable> GetTables()
        {
            // get all tables except for self.
            return tables.Values.Where(x => x.Name != "tables").ToList();
        }

        public SqlPipeDataContextRowColumn<SqlPipeDataContextTable>[] GetColumns()
        {
            var columns = new List<SqlPipeDataContextRowColumn<SqlPipeDataContextTable>>();
            columns.Add(Column<SqlPipeDataContextTable, string>("name", x => x.Name));
            columns.Add(Column<SqlPipeDataContextTable, string>("columns", x => string.Join(", ", x.ColumnNames)));
            return columns.ToArray();
        }

        public SqlPipeDataContextTable this[string from]
        {
            get
            {
                if (string.IsNullOrEmpty(from)) return null;
                if (tables.TryGetValue(from.ToLower(), out var table)) return table;
                return null;
            }
        }

        internal Type FindColumnType(string targetName)
        {
            foreach (var t in tables)
            {
                var c = t.Value.GetColumn(targetName);
                if (c != null)
                {
                    return c.ColumnType;
                }
            }

            return null;
        }
    }
}