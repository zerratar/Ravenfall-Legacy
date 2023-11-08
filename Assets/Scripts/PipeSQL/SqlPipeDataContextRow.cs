using System.Collections.Generic;
using System.Linq;

namespace RavenfallDataPipe
{
    public abstract class SqlPipeDataContextRow
    {
        public string Key { get; protected set; }
        public object SourceObject { get; set; }
        public SqlPipeDataContextRow(object sourceObject)
        {
            SourceObject = sourceObject;
        }

        public SqlPipeDataContextRow<T> As<T>()
        {
            return this as SqlPipeDataContextRow<T>;
        }

        public abstract Dictionary<string, object> GetValues(IReadOnlyList<SqlPipeDataContextRowColumn> columns);
    }


    public class SqlPipeDataContextRow<T> : SqlPipeDataContextRow
    {
        public T Source { get; set; }
        private Dictionary<string, SqlPipeDataContextRowColumn<T>> columns;

        public SqlPipeDataContextRow(T source, params SqlPipeDataContextRowColumn<T>[] columns)
            : base(source)
        {
            Source = source;
            this.columns = columns.ToDictionary(x => x.Name, x => x);
            // we don't want primary Keys to change, so we will try and extract it now if possible.
            this.Key = this.GetPrimaryKeyImpl();
        }

        public object this[string column]
        {
            get
            {
                if (columns.TryGetValue(column.ToLower(), out var c))
                {
                    return c.GetValue(Source);
                }

                return null;
            }
        }

        public string GetPrimaryKey(bool invalidate = false)
        {
            if (invalidate || string.IsNullOrEmpty(Key))
            {
                return Key = GetPrimaryKeyImpl();
            }

            return Key;
        }

        private string GetPrimaryKeyImpl()
        {
            var primaryKey = columns.Values.FirstOrDefault(x => x.IsPrimaryKey);
            if (primaryKey == null) return Source.GetHashCode().ToString();
            return primaryKey.GetValue(Source)?.ToString();
        }

        public override Dictionary<string, object> GetValues(IReadOnlyList<SqlPipeDataContextRowColumn> columns)
        {
            var dict = new Dictionary<string, object>();
            foreach (var c in columns)
            {
                dict[c.Name.ToLower()] = c.GetValue(Source);
            }
            return dict;
        }
    }

}