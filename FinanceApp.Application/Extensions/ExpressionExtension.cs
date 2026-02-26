using System;
using System.Linq.Expressions;

namespace FinanceApp.Infrastructure.Extensions
{
    public static class ExpressionExtensions
    {
        /// <summary>
        /// Combines two expressions with a logical AND (&&).
        /// Useful for building dynamic LINQ queries.
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="expr1">First expression</param>
        /// <param name="expr2">Second expression</param>
        /// <returns>Combined expression</returns>
        public static Expression<Func<T, bool>> AndAlso<T>(
            this Expression<Func<T, bool>> expr1,
            Expression<Func<T, bool>> expr2)
        {
            if (expr1 == null) throw new ArgumentNullException(nameof(expr1));
            if (expr2 == null) throw new ArgumentNullException(nameof(expr2));

            // Create a single parameter for both expressions
            var parameter = Expression.Parameter(typeof(T), "x");

            // Replace parameters in each expression with the new shared parameter
            var leftVisitor = new ReplaceParameterVisitor(expr1.Parameters[0], parameter);
            var left = leftVisitor.Visit(expr1.Body);

            var rightVisitor = new ReplaceParameterVisitor(expr2.Parameters[0], parameter);
            var right = rightVisitor.Visit(expr2.Body);

            // Combine using Expression.AndAlso
            var body = Expression.AndAlso(left!, right!);

            return Expression.Lambda<Func<T, bool>>(body, parameter);
        }

        // Helper class to replace parameters in expression trees
        private class ReplaceParameterVisitor : ExpressionVisitor
        {
            private readonly ParameterExpression _oldParameter;
            private readonly ParameterExpression _newParameter;

            public ReplaceParameterVisitor(ParameterExpression oldParameter, ParameterExpression newParameter)
            {
                _oldParameter = oldParameter ?? throw new ArgumentNullException(nameof(oldParameter));
                _newParameter = newParameter ?? throw new ArgumentNullException(nameof(newParameter));
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                return node == _oldParameter ? _newParameter : base.VisitParameter(node);
            }
        }
    }
}

/*using System;
using System.Linq.Expressions;

namespace FinanceApp.Infrastructure.Extensions
{
    public static class ExpressionExtensions
    {
        public static Expression<Func<T, bool>> AndAlso<T>(
            this Expression<Func<T, bool>> expr1,
            Expression<Func<T, bool>> expr2)
        {
            var parameter = Expression.Parameter(typeof(T));

            var leftVisitor = new ReplaceParameterVisitor(expr1.Parameters[0], parameter);
            var left = leftVisitor.Visit(expr1.Body);

            var rightVisitor = new ReplaceParameterVisitor(expr2.Parameters[0], parameter);
            var right = rightVisitor.Visit(expr2.Body);

            var body = Expression.AndAlso(left!, right!);

            return Expression.Lambda<Func<T, bool>>(body, parameter);
        }

        private class ReplaceParameterVisitor : ExpressionVisitor
        {
            private readonly ParameterExpression _oldParameter;
            private readonly ParameterExpression _newParameter;

            public ReplaceParameterVisitor(ParameterExpression oldParameter, ParameterExpression newParameter)
            {
                _oldParameter = oldParameter;
                _newParameter = newParameter;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                return node == _oldParameter ? _newParameter : base.VisitParameter(node);
            }
        }
    }
}*/