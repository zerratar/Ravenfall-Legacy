using System;
using System.Collections.Generic;
using System.Linq;

namespace RavenfallDataPipe
{
    public abstract class SqlPipeDataContextTable
    {
        public IReadOnlyList<string> ColumnNames { get; set; }
        public string Name { get; set; }

        public abstract void LoadRows();
        public abstract int GetRowCount();
        public abstract SqlPipeDataContextRowColumn GetColumn(string name);
        public abstract SqlPipeDataContextRow GetRow(int index);
    }


    public class SqlPipeDataContextTable<T> : SqlPipeDataContextTable
    {
        public Func<IEnumerable<T>> DataSource { get; set; }
        public SqlPipeDataContextRowColumn<T>[] Columns { get; set; }
        private Dictionary<string, SqlPipeDataContextRow<T>> rows = new Dictionary<string, SqlPipeDataContextRow<T>>();
        private SqlPipeDataContextRowColumn<T> primaryKeyColumn;

        public SqlPipeDataContextTable(string name, Func<IEnumerable<T>> dataSource, params SqlPipeDataContextRowColumn<T>[] columns)
        {
            Name = name;
            DataSource = dataSource;
            Columns = columns;
            ColumnNames = columns.Select(x => x.Name).ToList();
            primaryKeyColumn = this.Columns.FirstOrDefault(x => x.IsPrimaryKey);
            Invalidate();
        }

        /// <summary>
        /// This is slow, allocates, and not cached, make sure to only retrieve once before use.
        /// Note: this can be cached if data source does not change or generate a key for a referenced source
        /// </summary>
        public IReadOnlyList<SqlPipeDataContextRow<T>> Rows
        {
            get
            {
                Invalidate();
                return rows.Values.ToList();
            }
        }

        public override void LoadRows()
        {
            Invalidate();
        }

        private void Invalidate()
        {
            if (this.rows == null || this.rows.Count == 0)
            {
                var source = DataSource();
                foreach (var s in source)
                {
                    var row = new SqlPipeDataContextRow<T>(s, Columns);
                    var key = row.GetPrimaryKey();
                    rows[key] = row;
                }
            }
            else
            {
                // check if there has been changes in the data
                var source = DataSource();
                var keepers = new HashSet<string>();
                var hasPrimaryKey = primaryKeyColumn != null;
                foreach (var s in source)
                {
                    if (hasPrimaryKey)
                    {
                        var key = primaryKeyColumn.GetValue(s)?.ToString();
                        if (rows.ContainsKey(key))
                        {
                            keepers.Add(key);
                            continue;
                        }

                        rows[key] = new SqlPipeDataContextRow<T>(s, Columns);
                        keepers.Add(key);
                    }
                    else
                    {
                        var row = new SqlPipeDataContextRow<T>(s, Columns);
                        rows[row.GetPrimaryKey()] = row;
                    }
                }

                if (hasPrimaryKey)
                {
                    var keys = rows.Keys.ToList();
                    foreach (var v in keys)
                    {
                        if (!keepers.Contains(v))
                            rows.Remove(v);
                    }
                }
            }
        }

        public override SqlPipeDataContextRowColumn GetColumn(string name)
        {
            return Columns.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public override int GetRowCount()
        {
            return this.rows?.Count ?? 0;
        }

        public override SqlPipeDataContextRow GetRow(int index)
        {
            return Rows[index];
        }
    }

}