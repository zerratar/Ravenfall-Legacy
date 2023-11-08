using System;

namespace RavenfallDataPipe
{
    public abstract class SqlPipeDataContextRowColumn
    {
        private readonly string header;
        private readonly Type rowType;
        private readonly Type columnType;
        private Func<object, object> objGetValue;

        public SqlPipeDataContextRowColumn(string header, Type rowType, Type columnType, Func<object, object> objGetValue)
        {
            this.rowType = rowType;
            this.columnType = columnType;
            this.header = header;
            this.objGetValue = objGetValue;
        }

        public Type ColumnType => columnType;
        public Type RowType => rowType;
        public string Name => header;

        internal object GetValue(object source)
        {
            return objGetValue(source);
        }
    }


    public class SqlPipeDataContextRowColumn<TSource, TValue> : SqlPipeDataContextRowColumn
    {
        private readonly Func<TSource, TValue> getValue;

        public SqlPipeDataContextRowColumn(string header, Func<TSource, TValue> getValue)
            : base(header, typeof(TSource), typeof(TValue), x => getValue((TSource)x))
        {
            this.getValue = getValue;
        }

        public SqlPipeDataContextRowColumn(string header, Type valueType, Func<TSource, TValue> getValue)
            : base(header, typeof(TSource), valueType, x => getValue((TSource)x))
        {
            this.getValue = getValue;
        }

        public bool IsPrimaryKey { get; protected set; }
        public new Func<TSource, TValue> GetValue => getValue;
        public SqlPipeDataContextRowColumn<TSource, TValue> MakePrimaryKey()
        {
            IsPrimaryKey = true;
            return this;
        }

        public static implicit operator SqlPipeDataContextRowColumn<TSource>(SqlPipeDataContextRowColumn<TSource, TValue> src)
        {
            return new SqlPipeDataContextRowColumn<TSource>(src.Name, typeof(TValue), x => (object)src.GetValue(x), src.IsPrimaryKey);
        }
    }

    public class SqlPipeDataContextRowColumn<T> : SqlPipeDataContextRowColumn<T, object>
    {
        public SqlPipeDataContextRowColumn(string header, Type valueType, Func<T, object> getValue, bool isPrimaryKey)
            : base(header, valueType, getValue)
        {
            IsPrimaryKey = isPrimaryKey;
        }
    }


}