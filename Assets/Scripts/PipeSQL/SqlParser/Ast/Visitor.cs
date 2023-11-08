﻿using System.Linq;
using System.Reflection;

namespace SqlParser.Ast
{

    public enum ControlFlow
    {
        Continue,
        Break
    }

    public class ElementVisitor
    {
        public static ControlFlow Visit(IElement @this, Visitor visitor)
        {
            switch (@this)
            {
                case TableFactor t:
                    {
                        var flow = visitor.PreVisitRelation(t);

                        if (flow == ControlFlow.Break)
                        {
                            return flow;
                        }

                        VisitChildren(@this, visitor);
                        return visitor.PostVisitRelation(t);
                    }

                case SqlExpression e:
                    {
                        var flow = visitor.PreVisitExpression(e);

                        if (flow == ControlFlow.Break)
                        {
                            return flow;
                        }
                        VisitChildren(@this, visitor);
                        return visitor.PostVisitExpression(e);
                    }

                case Statement s:
                    {
                        var flow = visitor.PreVisitStatement(s);
                        if (flow == ControlFlow.Break)
                        {
                            return flow;
                        }

                        VisitChildren(@this, visitor);
                        return visitor.PostVisitStatement(s);
                    }

                default:
                    VisitChildren(@this, visitor);
                    return ControlFlow.Continue;
            }
        }

        private static void VisitChildren(IElement element, Visitor visitor)
        {
            var properties = GetVisitableChildProperties(element);

            foreach (var property in properties)
            {
                //if (!property.PropertyType.IsAssignableTo(typeof(IElement)))
                //{
                //    continue;
                //}

                if (!typeof(IElement).IsAssignableFrom(property.PropertyType))
                {
                    continue;
                }

                var value = property.GetValue(element);

                if (value == null)
                {
                    continue;
                }

                var child = (IElement)value;
                ElementVisitor.Visit(child, visitor);
            }
        }

        internal static PropertyInfo[] GetVisitableChildProperties(IElement element)
        {
            var elementType = element.GetType();

            // Public and not static
            var properties = elementType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var decorated = properties.Where(p => p.GetCustomAttribute<VisitAttribute>() != null)
                .OrderBy(p => p.GetCustomAttribute<VisitAttribute>()!.Order)
                .ToList();

            // No decorated properties uses the default visit order.
            // No need to look for additional properties
            if (!decorated.Any())
            {
                return properties.ToArray();
            }

            // Visit orders are not specified in the constructor; return the decorated list.
            if (decorated.Count() == properties.Length)
            {
                return decorated.ToArray();
            }

            // Although identified as properties, primary constructor parameters 
            // use parameter attributes, not property attributes and must be identified
            // apart from the property list. This find their order and inserts
            // the missing properties into the decorated property list.
            try
            {
                var constructors = elementType.GetConstructors();
                var primaryConstructor = constructors.Single();
                var constructorParams = primaryConstructor.GetParameters();

                var decoratedParameters = constructorParams.Where(p => p.GetCustomAttribute<VisitAttribute>() != null)
                    .OrderBy(p => p.GetCustomAttribute<VisitAttribute>()!.Order)
                    .Select(p => (Property: p, p.GetCustomAttribute<VisitAttribute>()!.Order))
                    .ToList();

                foreach (var param in decoratedParameters)
                {
                    var property = properties.FirstOrDefault(p => p.Name == param.Property.Name);

                    if (property != null)
                    {
                        decorated.Insert(param.Order, property);
                    }
                }
            }
            catch { }

            return decorated.ToArray();
        }
    }

    public interface IElement
    {
    }

    public abstract class Visitor
    {
        public virtual ControlFlow PreVisitRelation(TableFactor relation)
        {
            return ControlFlow.Continue;
        }

        public virtual ControlFlow PostVisitRelation(TableFactor relation)
        {
            return ControlFlow.Continue;
        }

        public virtual ControlFlow PreVisitExpression(SqlExpression expression)
        {
            return ControlFlow.Continue;
        }

        public virtual ControlFlow PostVisitExpression(SqlExpression expression)
        {
            return ControlFlow.Continue;
        }

        public virtual ControlFlow PreVisitStatement(Statement statement)
        {
            return ControlFlow.Continue;
        }

        public virtual ControlFlow PostVisitStatement(Statement statement)
        {
            return ControlFlow.Continue;
        }
    }
}