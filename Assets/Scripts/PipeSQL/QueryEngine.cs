using System.Collections.Generic;
using System.Data;
using SqlParser;
using SqlParser.Ast;
using System.Linq.Expressions;
using static SqlParser.Ast.SqlExpression;
using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Linq;

namespace RavenfallDataPipe
{
    public class QueryEngine
    {
        public QueryEngineContext Context { get; }

        private Parser sqlParser;
        private Dictionary<string, Func<Dictionary<string, object>, bool>> predicateCache
            = new Dictionary<string, Func<Dictionary<string, object>, bool>>();

        public QueryEngine(QueryEngineContext context)
        {
            this.Context = context;
            this.sqlParser = new Parser();
        }

        public async Task<List<Dictionary<string, object>>> ProcessAsync(string query)
        {
            List<Dictionary<string, object>> result = null;
            try
            {
                var ast = sqlParser.ParseSql(query);

                if (ast == null)
                {
                    return Error($"AST is null, using query: '{query}'");
                }

                result = await ProcessQueryAsync(ast, query);
            }
            catch (Exception exc)
            {
                result = Error(exc);
            }

            return result;
        }

        private static List<Dictionary<string, object>> Error(object exc)
        {
            return new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        ["error"] = exc.ToString()
                    }
                };
        }

        private async Task<List<Dictionary<string, object>>> ProcessQueryAsync(Sequence<Statement> ast, string sql)
        {
            var lastCodeBlock = "iterate statement ast";
            var rowValues = new List<Dictionary<string, object>>();
            foreach (var statement in ast)
            {
                if (statement is Statement.Select selectStmnt)
                {
                    lastCodeBlock = "select statement start";

                    var limit = selectStmnt.Query.Limit;
                    var offset = selectStmnt.Query.Offset;
                    var orderBy = selectStmnt.Query.OrderBy;

                    var selectExpr = selectStmnt.Query.Body.AsSelectExpression();
                    var select = selectExpr.Select;
                    var source = select.From;
                    var projection = select.Projection;
                    var selection = select.Selection;

                    //var tables = new List<SqlPipeDataContextTable>();

                    lastCodeBlock = "building table selection";
                    if (TryBuildTableSelection(selectStmnt, out var tableSelections, out var error))
                    {
                        try
                        {
                            // take all and build a dataset with the expected columns
                            var inSelection = new HashSet<string>();

                            lastCodeBlock = "traversing table selection";
                            foreach (var ts in tableSelections)
                            {
                                var table = ts.Table;

                                table.LoadRows();

                                var rowCount = table.GetRowCount();
                                for (var i = 0; i < rowCount; i++)
                                {
                                    lastCodeBlock = "get table, " + table.Name + " row data (row: " + i + ")";

                                    var row = table.GetRow(i);

                                    var functions = new List<ColumnSelection>();

                                    // get all values first so we can do proccessing on them
                                    // then remove those that are not in the selection.
                                    var values = row.GetValues(ts.Columns.Select(x => x.Column).ToArray());

                                    foreach (var s in ts.Selection)
                                    {
                                        if (s.IsWildcard)
                                        {
                                            lastCodeBlock = "get table, " + table.Name + " row data (row: " + i + "), wildcard selection";
                                            foreach (var v in values) inSelection.Add(v.Key);
                                            break;
                                        }

                                        var n = s.Alias;
                                        if (string.IsNullOrEmpty(n)) n = s.FunctionName;
                                        if (string.IsNullOrEmpty(n)) n = s.Column.Name;
                                        inSelection.Add(n.ToLower());

                                        // convert to alias column
                                        lastCodeBlock = "get table, " + table.Name + " row data (row: " + i + "), fetching alias";
                                        if (!string.IsNullOrEmpty(s.Alias) && !string.IsNullOrEmpty(s.Column.Name))
                                        {
                                            var k = s.Column.Name.ToLower();
                                            if (values.ContainsKey(k))
                                            {
                                                var v = values[k];
                                                values.Remove(k);
                                                values[s.Alias.ToLower()] = v;
                                            }
                                        }

                                        if (s.IsFunctionCall)
                                        {
                                            // we will only support Count for now, regardless of arguments.
                                            values = InvokeFunctionCall(values, s, table, i, rowCount);
                                        }
                                    }

                                    // remove those not in our selection
                                    rowValues.Add(values);
                                }
                            }

                            // build an expression for filtering the data

                            // where
                            var q = selectStmnt.Query;
                            if (selection != null)
                            {
                                lastCodeBlock = "applying where filter";
                                rowValues = Where(rowValues, selection, sql, out var errString);
                                if (!string.IsNullOrEmpty(errString))
                                {
                                    return Error(errString);
                                }
                            }

                            // order
                            // offset
                            // limit

                            if (orderBy != null)
                            {
                                lastCodeBlock = "applying order by";
                                rowValues = OrderBy(rowValues, orderBy);
                            }

                            lastCodeBlock = "remove entries not part of selection";
                            // remove all keys not in selection
                            foreach (var rv in rowValues)
                            {

                                HashSet<string> toRemove = new HashSet<string>();

                                foreach (var r in rv)
                                {
                                    if (!inSelection.Contains(r.Key))
                                    {
                                        toRemove.Add(r.Key);
                                    }
                                }

                                foreach (var r in toRemove)
                                {
                                    rv.Remove(r);
                                }
                            }

                            // final processing, make sure all rows with compound keys gets grouped into a separate dictionary
                            lastCodeBlock = "grouping compound keys";
                            var resultRows = new List<Dictionary<string, object>>();
                            foreach (var row in rowValues)
                            {
                                var transformed = new Dictionary<string, object>();
                                foreach (var column in row)
                                {
                                    if (!column.Key.Contains('.'))
                                    {
                                        transformed[column.Key] = column.Value;
                                        continue;
                                    }

                                    var path = column.Key.Split('.');
                                    var active = transformed;
                                    // ensure path is created
                                    for (var i = 0; i < path.Length - 1; ++i)
                                    {
                                        var key = path[i];
                                        if (!active.TryGetValue(key, out var value))
                                        {
                                            active[key] = (value = new Dictionary<string, object>());
                                        }
                                        active = (Dictionary<string, object>)value;
                                    }

                                    active[path[^1]] = column.Value;
                                }
                                resultRows.Add(transformed);
                            }
                            rowValues = resultRows;
                        }
                        catch (KeyNotFoundException notFound)
                        {
                            var key = notFound.Message.Split('\'')[1];
                            return Error($"'{key}' is not a valid key in this context. Is it missing from the select?");
                        }
                        catch (Exception exc)
                        {
                            return Error(lastCodeBlock + "\r\n" + exc.ToString());
                        }
                    }
                    else
                    {
                        return Error(lastCodeBlock + "\r\n" + error.ToString());
                    }
                }
            }

            return rowValues;
        }

        private Dictionary<string, object> InvokeFunctionCall(Dictionary<string, object> values, ColumnSelection func, SqlPipeDataContextTable table, int rowIndex, int rowCount)
        {
            if (!func.FunctionName.Equals("count", StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException("Only the function 'count' is supported right now.");
            }

            if (string.IsNullOrEmpty(func.Alias))
            {
                values["count"] = rowCount;
            }
            else
            {
                values[func.Alias] = rowCount;
            }

            return values;
        }

        private List<Dictionary<string, object>> Where(List<Dictionary<string, object>> data, SqlExpression selection, string key, out string error)
        {
            error = null;
            if (predicateCache.TryGetValue(key, out var predicate))
            {
                return data.Where(predicate).ToList();
            }

            var selectionPredicate = BuildWherePredicate(selection, out error);
            predicateCache[key] = selectionPredicate;
            return data.Where(selectionPredicate).ToList();
        }

        private Func<Dictionary<string, object>, bool> BuildWherePredicate(SqlExpression selection, out string error)
        {
            error = null;
            var expression = Expression.Equal(Expression.Constant(1), Expression.Constant(1));
            var sourceParameter = Expression.Parameter(typeof(Dictionary<string, object>), "source");
            expression = Expression.AndAlso(expression, BuildExpression(selection, sourceParameter));
            expression = (BinaryExpression)new ParameterReplacer(sourceParameter).Visit(expression);
            var expr = Expression.Lambda<Func<Dictionary<string, object>, bool>>(expression, sourceParameter);

            try
            {
                return expr.Compile();
            }
            catch (Exception exc)
            {
                error = "Error building Where Predicate: " + exc.ToString();
                return null;
            }
        }

        private Expression BuildExpression(SqlExpression selection, ParameterExpression sourceParameter)
        {
            if (selection is SqlExpression.LiteralValue literalValue)
            {
                var val = literalValue.Value;
                if (val is Value.Number num)
                {
                    return Expression.Constant(double.Parse(num.Value.Replace('.', ','), NumberStyles.Any, CultureInfo.InvariantCulture));
                }
                else if (val is Value.StringBasedValue str)
                {
                    return Expression.Constant(str.Value);
                }

                throw new NotSupportedException("Type type: " + val.GetType().FullName + " is not a supported type.");
            }

            if (selection is SqlExpression.BinaryOp binaryOp)
            {
                var left = BuildExpression(binaryOp.Left, sourceParameter);
                var right = BuildExpression(binaryOp.Right, sourceParameter);


                if (binaryOp.Op == BinaryOperator.And)
                {
                    return Expression.AndAlso(left, right);
                }

                if (binaryOp.Op == BinaryOperator.Or)
                {
                    return Expression.Or(left, right);
                }

                if (left.Type != right.Type)
                {
                    // check which type has more value
                    // then always cast to original value first to avoid cast errors

                    if (GetTypeComparisonValue(left.Type) > GetTypeComparisonValue(right.Type))
                    {
                        right = Expression.Convert(Expression.Convert(right, right.Type), left.Type);
                    }
                    else
                    {

                        left = Expression.Convert(Expression.Convert(left, left.Type), right.Type);
                    }
                }

                if (binaryOp.Op == BinaryOperator.Eq)
                {
                    return Expression.Equal(left, right);
                }

                if (binaryOp.Op == BinaryOperator.NotEq)
                {
                    return Expression.NotEqual(left, right);
                }

                if (binaryOp.Op == BinaryOperator.Gt)
                {
                    return Expression.GreaterThan(left, right);
                }

                if (binaryOp.Op == BinaryOperator.GtEq)
                {
                    return Expression.GreaterThanOrEqual(left, right);
                }

                if (binaryOp.Op == BinaryOperator.Lt)
                {
                    return Expression.LessThan(left, right);
                }

                if (binaryOp.Op == BinaryOperator.LtEq)
                {
                    return Expression.LessThanOrEqual(left, right);
                }
            }

            if (selection is SqlExpression.Like like)
            {
                var target = like.Expression.AsIdentifier();
                var targetName = target.Ident.Value.ToLower();

                dynamic pattern = like.Pattern.AsLiteral().Value;
                string val = pattern.Value.ToLower();

                // Construct the source.Name.ToLower() expression
                var indexExpression = Expression.Call(sourceParameter, typeof(Dictionary<string, object>).GetMethod("get_Item"), Expression.Constant(targetName));
                var toStringMethod = typeof(object).GetMethod("ToString");
                var toStringCall = Expression.Call(indexExpression, toStringMethod);
                var toLowerCall = Expression.Call(toStringCall, typeof(string).GetMethod("ToLower", Type.EmptyTypes));

                // If pattern does not contain any wildcards, simply check for equality
                if (!val.Contains("%"))
                {
                    return Expression.Equal(toLowerCall, Expression.Constant(val));
                }

                // Otherwise, split the pattern by '%' and construct a .Contains() check for each segment
                var segments = val.Split(new[] { '%' }, StringSplitOptions.RemoveEmptyEntries);
                Expression combinedExpression = null;
                foreach (var segment in segments)
                {
                    var containsSegment = Expression.Call(toLowerCall, typeof(string).GetMethod("Contains", new[] { typeof(string) }), Expression.Constant(segment));
                    combinedExpression = combinedExpression == null ? (Expression)containsSegment : Expression.AndAlso(combinedExpression, containsSegment);
                }

                return combinedExpression;
            }

            if (selection is SqlExpression.Identifier id)
            {
                var targetName = id.Ident.Value.ToLower();
                Expression<Func<Dictionary<string, object>, object>> valueSelector = source => source[targetName];
                Type type = Context.FindColumnType(targetName);
                if (type != null)
                {
                    return Expression.Convert(valueSelector.Body, type);
                }
                return valueSelector.Body;
            }

            if (selection is SqlExpression.CompoundIdentifier cid)
            {
                var targetName = string.Join(".", cid.Idents.Select(x => x.Value.ToLower()));
                Expression<Func<Dictionary<string, object>, object>> valueSelector = source => source[targetName];
                Type type = Context.FindColumnType(targetName);
                if (type != null)
                {
                    return Expression.Convert(valueSelector.Body, type);
                }
                return valueSelector.Body;
            }

            throw new NotSupportedException(selection.GetType().FullName + " not supported.");
        }

        private double GetTypeComparisonValue(Type type)
        {
            if (type == typeof(byte)) return 0;
            if (type == typeof(short)) return 1;
            if (type == typeof(int)) return 2;
            if (type == typeof(float)) return 2.5;
            if (type == typeof(long)) return 3;
            if (type == typeof(double)) return 4;
            if (type == typeof(decimal)) return 5;
            return -1;
        }

        private List<Dictionary<string, object>> Where(List<Dictionary<string, object>> data, SqlExpression.BinaryOp binaryOp)
        {
            return data;
        }

        private List<Dictionary<string, object>> Where(List<Dictionary<string, object>> data, SqlExpression.Like like)
        {
            return data;
        }

        private static List<Dictionary<string, object>> OrderBy(
            List<Dictionary<string, object>> rowValues,
            Sequence<OrderByExpression> orderBy)
        {
            IOrderedEnumerable<Dictionary<string, object>> ordered = null;
            foreach (var o in orderBy)
            {
                var asc = o.Asc.GetValueOrDefault();
                var column = "";
                // check if we use compound (path identifiers, eg: players.id, first is table, second is column)
                if (o.Expression is SqlExpression.CompoundIdentifier compound)
                {
                    column = string.Join(".", compound.Idents.Select(x => x.Value.ToLower()));
                }
                else
                {
                    var identifier = o.Expression.AsIdentifier();
                    column = identifier.Ident.Value.ToLower();
                }

                if (ordered == null)
                {
                    ordered = asc ?
                        rowValues.OrderBy(x => x[column]) :
                        rowValues.OrderByDescending(x => x[column]);
                }
                else
                {
                    ordered = asc ?
                        ordered.ThenBy(x => x[column]) :
                        ordered.ThenByDescending(x => x[column]);
                }
            }
            if (ordered != null)
            {
                rowValues = ordered.ToList();
            }

            return rowValues;
        }

        private string GetClosestTable(string target, IReadOnlyList<SqlPipeDataContextTable> tables)
        {
            var score = 999999;
            var selection = "";
            foreach (var t in tables)
            {
                var s = ItemResolver.LevenshteinDistance(t.Name, target);
                if (s < score)
                {
                    selection = t.Name;
                    score = s;
                }
            }
            return selection;
        }

        private bool TryBuildTableSelection(Statement.Select selectStmnt, out List<TableSelection> tableSelections, out Exception error)
        {
            error = null;
            tableSelections = new List<TableSelection>();

            try
            {
                var selectExpr = selectStmnt.Query.Body.AsSelectExpression();
                var select = selectExpr.Select;
                var source = select.From;
                var projection = select.Projection;
                var selection = select.Selection;

                foreach (var src in source)
                {
                    // ignore joins for now
                    // src.Joins

                    var tableName = src.Relation.AsTable().Name;
                    var table = Context[tableName];

                    if (table == null)
                    {
                        var test = GetClosestTable(tableName, Context.GetTables());
                        throw new TableNotFoundException("No table of name '" + tableName + "' available. Did you mean '" + test + "'?");
                    }

                    var columns = new List<ColumnSelection>();
                    var targetColumns = new List<ColumnSelection>();

                    foreach (var t in table.ColumnNames)
                    {
                        columns.Add(new ColumnSelection
                        {
                            Column = table.GetColumn(t),
                            Alias = t
                        });
                    }

                    if (src.Joins != null && src.Joins.Count > 0)
                    {
                        throw new NotSupportedException("joins are not supported.");
                    }

                    foreach (var p in projection)
                    {
                        targetColumns.Add(GetColumnSelection(p, table));
                    }

                    tableSelections.Add(new TableSelection
                    {
                        Columns = columns,
                        Selection = targetColumns,
                        Table = table,
                    });
                }

                return true;
            }
            catch (Exception exc)
            {
                error = exc;
            }

            return false;
        }


        private ColumnSelection GetColumnSelection(SelectItem item, SqlPipeDataContextTable table)
        {
            if (item is SelectItem.Wildcard wildcard)
            {
                return new ColumnSelection
                {
                    Alias = "*",
                    IsWildcard = true
                };
            }

            string a = "";
            SqlExpression selectExpression = null;
            if (item is SelectItem.ExpressionWithAlias alias)
            {
                a = alias.Alias;
                selectExpression = alias.Expression;
            }
            else
            {
                var unnamned = (SelectItem.UnnamedExpression)item;
                selectExpression = unnamned.Expression;
            }

            if (selectExpression is SqlExpression.Function func)
            {
                var funcArgs = func.Args;
                var argList = new List<FunctionCallArgument>();
                if (funcArgs != null)
                {
                    foreach (var arg in funcArgs)
                    {
                        if (arg is FunctionArg.Named namedArg)
                        {
                            var fa = GetArgument(namedArg.Arg, func, table);
                            fa.Name = namedArg.Name.Value;
                            argList.Add(fa);
                        }
                        else
                        {
                            argList.Add(GetArgument((arg as FunctionArg.Unnamed).FunctionArgExpression, func, table));
                        }
                    }
                }

                var name = string.Join(".", func.Name.Values.Select(x => x.Value));

                return new ColumnSelection
                {
                    Alias = a ?? name,
                    //Name = name,
                    FunctionName = name,
                    IsFunctionCall = true,
                    Arguments = argList,
                };
            }

            var ident = "";
            if (selectExpression is CompoundIdentifier cid)
            {
                ident = string.Join(".", cid.Idents.Select(x => x.Value.ToLower()));
            }
            else
            {
                ident = selectExpression.AsIdentifier().Ident.Value.ToLower();
            }

            return new ColumnSelection
            {
                Alias = a ?? ident,
                Column = table.GetColumn(ident)
            };
        }

        private FunctionCallArgument GetArgument(FunctionArgExpression arg, Function func, SqlPipeDataContextTable table)
        {
            if (arg is FunctionArgExpression.Wildcard w) { }
            if (arg is FunctionArgExpression.QualifiedWildcard qw) { }
            if (arg is FunctionArgExpression.FunctionExpression expr) { }
            return new FunctionCallArgument { };
        }

        private class TableSelection
        {
            public SqlPipeDataContextTable Table { get; set; }
            public IReadOnlyList<ColumnSelection> Selection { get; set; }
            public IReadOnlyList<ColumnSelection> Columns { get; set; }
        }

        private class FunctionCallArgument
        {
            public string Name { get; internal set; }
        }

        private class ColumnSelection
        {
            public string Alias { get; set; }
            public SqlPipeDataContextRowColumn Column { get; set; }
            public List<FunctionCallArgument> Arguments { get; internal set; }
            public bool IsWildcard { get; set; }
            public bool IsFunctionCall { get; set; }
            public string FunctionName { get; set; }

            public override string ToString()
            {
                if (!string.IsNullOrEmpty(Alias) && Alias != Column.Name)
                    return "Column: " + Column.Name + " as " + Alias;
                return "Column: " + Column.Name;
            }
        }
        private class ParameterReplacer : ExpressionVisitor
        {
            private readonly ParameterExpression dictionarySource;
            internal ParameterReplacer(ParameterExpression dictionarySource) => this.dictionarySource = dictionarySource;
            protected override Expression VisitParameter(ParameterExpression node)
            {
                // only replace source with this one.
                if (node.Name == "source")
                    return base.VisitParameter(dictionarySource);

                return base.VisitParameter(node);
            }
        }
    }
}