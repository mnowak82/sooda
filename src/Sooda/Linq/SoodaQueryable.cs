//
// Copyright (c) 2010 Piotr Fusik <piotr@fusik.info>
//
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions
// are met:
//
// * Redistributions of source code must retain the above copyright notice,
//   this list of conditions and the following disclaimer.
//
// * Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
// THE POSSIBILITY OF SUCH DAMAGE.
//

#if DOTNET35

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using Sooda;
using Sooda.QL;
using Sooda.ObjectMapper;

namespace Sooda.Linq
{
    public class SoodaQueryable<T> : IQueryable<T>, IQueryProvider
    {
        readonly Expression _expr;

        protected SoodaQueryable()
        {
            _expr = Expression.Constant(this);
        }

        SoodaQueryable(Expression expr)
        {
            _expr = expr;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            IEnumerable ie = (IEnumerable) Execute(this.Expression);
            return ie.GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            IEnumerable ie = (IEnumerable) Execute(this.Expression);
            return ie.OfType<T>().GetEnumerator();
        }

        public Type ElementType
        {
            get
            {
                return typeof(T);
            }
        }

        public Expression Expression
        {
            get
            {
                return _expr;
            }
        }

        public IQueryProvider Provider
        {
            get
            {
                return this;
            }
        }

        public IQueryable CreateQuery(Expression expr)
        {
            throw new NotImplementedException(); // TODO
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expr)
        {
            return new SoodaQueryable<TElement>(expr);
        }

        static SoodaQueryableContext<T> GetQueryableContext(MethodCallExpression mc)
        {
            if (mc.Arguments.Count == 2)
            {
                ConstantExpression expr = (ConstantExpression) mc.Arguments[0];
                return (SoodaQueryableContext<T>) expr.Value;
            }
            throw new NotSupportedException();
        }

        static LambdaExpression GetLambdaWithParamCheck(MethodCallExpression mc)
        {
            if (mc.Arguments.Count == 2)
            {
                Expression expr = mc.Arguments[1];
                while (expr.NodeType == ExpressionType.Quote)
                    expr = ((UnaryExpression) expr).Operand;
                LambdaExpression lambda = (LambdaExpression) expr;
                if (lambda.Parameters.Count == 1)
                    return lambda;
            }
            throw new NotSupportedException();
        }

        static SoqlBooleanExpression TranslateAnd(BinaryExpression expr)
        {
            SoqlBooleanExpression left = TranslateBoolean(expr.Left);
            SoqlBooleanExpression right = TranslateBoolean(expr.Right);
            return new SoqlBooleanAndExpression(left, right);
        }

        static SoqlBooleanExpression TranslateOr(BinaryExpression expr)
        {
            SoqlBooleanExpression left = TranslateBoolean(expr.Left);
            SoqlBooleanExpression right = TranslateBoolean(expr.Right);
            return new SoqlBooleanOrExpression(left, right);
        }

        static SoqlBooleanExpression TranslateNot(UnaryExpression expr)
        {
            SoqlBooleanExpression operand = TranslateBoolean(expr.Operand);
            return new SoqlBooleanNegationExpression(operand);
        }

        static SoqlBinaryExpression TranslateBinary(BinaryExpression expr, SoqlBinaryOperator op)
        {
            SoqlExpression left = TranslateExpression(expr.Left);
            SoqlExpression right = TranslateExpression(expr.Right);
            return new SoqlBinaryExpression(left, right, op);
        }

        static SoqlBooleanExpression TranslateRelational(BinaryExpression expr, SoqlRelationalOperator op)
        {
            SoqlExpression left = TranslateExpression(expr.Left).Simplify();
            SoqlExpression right = TranslateExpression(expr.Right).Simplify();
            SoqlLiteralExpression rightConst = right as SoqlLiteralExpression;
            if (rightConst != null && rightConst.GetConstantValue() == null
                && !(left is SoqlLiteralExpression))
            {
                switch (op)
                {
                case SoqlRelationalOperator.Equal:
                    return new SoqlBooleanIsNullExpression(left, false);
                case SoqlRelationalOperator.NotEqual:
                    return new SoqlBooleanIsNullExpression(left, true);
                default:
                    throw new NotSupportedException(op + " NULL");
                }
            }
            return new SoqlBooleanRelationalExpression(left, right, op);
        }

        static SoqlExpression TranslateMember(MemberExpression expr)
        {
            string name = expr.Member.Name;
            if (expr.Expression.NodeType == ExpressionType.Parameter)
                return new SoqlPathExpression(name);
            SoqlExpression parent = TranslateExpression(expr.Expression);
            SoqlPathExpression parentPath = parent as SoqlPathExpression;
            if (parentPath != null)
            {
                Type t = expr.Member.ReflectedType;
                if (typeof(SoodaObject).IsAssignableFrom(t))
                    return new SoqlPathExpression(parentPath, name);
                if (typeof(INullable).IsAssignableFrom(t))
                {
                    if (name == "Value")
                        return TranslateExpression(expr.Expression);
                    if (name == "IsNull")
                    {
                        SoqlExpression operand = TranslateExpression(expr.Expression);
                        return new SoqlBooleanIsNullExpression(operand, false);
                    }
                }
                throw new NotSupportedException(string.Format("{0}.{1}", t.FullName, name));
            }
            // partial evaluation
            SoqlLiteralExpression parentConst = parent as SoqlLiteralExpression;
            if (parentConst != null)
            {
                object obj = parentConst.GetConstantValue();
                FieldInfo fi = expr.Member as FieldInfo;
                if (fi != null)
                    return new SoqlLiteralExpression(fi.GetValue(obj));
                PropertyInfo pi = expr.Member as PropertyInfo;
                if (pi != null)
                    return new SoqlLiteralExpression(pi.GetValue(obj, null));
                throw new NotSupportedException(expr.Member.MemberType.ToString());
            }
            throw new NotSupportedException(expr.Expression.ToString());
        }

        static SoqlBooleanExpression TranslateBoolean(Expression expr)
        {
            switch (expr.NodeType)
            {
            case ExpressionType.And:
            case ExpressionType.AndAlso:
                return TranslateAnd((BinaryExpression) expr);
            case ExpressionType.Or:
            case ExpressionType.OrElse:
                return TranslateOr((BinaryExpression) expr);
            case ExpressionType.Not:
                return TranslateNot((UnaryExpression) expr);
            case ExpressionType.Equal:
                return TranslateRelational((BinaryExpression) expr, SoqlRelationalOperator.Equal);
            case ExpressionType.NotEqual:
                return TranslateRelational((BinaryExpression) expr, SoqlRelationalOperator.NotEqual);
            case ExpressionType.LessThan:
                return TranslateRelational((BinaryExpression) expr, SoqlRelationalOperator.Less);
            case ExpressionType.LessThanOrEqual:
                return TranslateRelational((BinaryExpression) expr, SoqlRelationalOperator.LessOrEqual);
            case ExpressionType.GreaterThan:
                return TranslateRelational((BinaryExpression) expr, SoqlRelationalOperator.Greater);
            case ExpressionType.GreaterThanOrEqual:
                return TranslateRelational((BinaryExpression) expr, SoqlRelationalOperator.GreaterOrEqual);
            case ExpressionType.MemberAccess:
                SoqlExpression ql = TranslateMember((MemberExpression) expr);
                SoqlBooleanExpression qlBool = ql as SoqlBooleanExpression;
                if (qlBool != null)
                    return qlBool;
                return new SoqlBooleanRelationalExpression(ql, SoqlBooleanLiteralExpression.True, SoqlRelationalOperator.Equal);
            default:
                throw new NotSupportedException(expr.NodeType.ToString());
            }
        }

        static SoqlExpression TranslateExpression(Expression expr)
        {
            switch (expr.NodeType)
            {
            case ExpressionType.Constant:
                return new SoqlLiteralExpression(((ConstantExpression) expr).Value);
            case ExpressionType.MemberAccess:
                return TranslateMember((MemberExpression) expr);
            case ExpressionType.Add:
                return TranslateBinary((BinaryExpression) expr, SoqlBinaryOperator.Add);
            case ExpressionType.Subtract:
                return TranslateBinary((BinaryExpression) expr, SoqlBinaryOperator.Sub);
            case ExpressionType.Multiply:
                return TranslateBinary((BinaryExpression) expr, SoqlBinaryOperator.Mul);
            case ExpressionType.Divide:
                return TranslateBinary((BinaryExpression) expr, SoqlBinaryOperator.Div);
            case ExpressionType.Modulo:
                return TranslateBinary((BinaryExpression) expr, SoqlBinaryOperator.Mod);
            default:
                return TranslateBoolean(expr);
            }
        }

        public object Execute(Expression expr)
        {
            MethodCallExpression mc = (MethodCallExpression) expr;
            if (mc.Method.DeclaringType != typeof(Queryable))
                throw new NotSupportedException(mc.Method.DeclaringType.FullName);
            switch (mc.Method.Name)
            {
            case "Where":
                SoodaQueryableContext<T> context = GetQueryableContext(mc);
                LambdaExpression lambda = GetLambdaWithParamCheck(mc);
                SoqlBooleanExpression filter = (SoqlBooleanExpression) TranslateBoolean(lambda.Body).Simplify();
                //new SoqlPrettyPrinter(Console.Out).PrintExpression(filter);
                return new SoodaObjectListSnapshot(context.Transaction, new SoodaWhereClause(filter), SoodaOrderBy.Unsorted, -1, context.Options, context.ClassInfo);
            default:
                break;
            }
            throw new NotSupportedException(mc.Method.Name);
        }

        public TResult Execute<TResult>(Expression expr)
        {
            return (TResult) Execute(expr);
        }
    }
}

#endif