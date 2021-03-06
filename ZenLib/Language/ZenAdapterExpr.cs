﻿// <copyright file="ZenAdapterExpr.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace ZenLib
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Expression that allows for converting between types.
    /// </summary>
    internal sealed class ZenAdapterExpr<TTo, TFrom> : Zen<TTo>
    {
        private static Dictionary<object, Zen<TTo>> hashConsTable = new Dictionary<object, Zen<TTo>>();

        public override Zen<TTo> Unroll()
        {
            return CreateMulti(this.Expr.Unroll(), this.Converters, true);
        }

        private static Zen<TTo> Simplify(Zen<TFrom> e, ImmutableList<Func<object, object>> converters, bool unroll)
        {
            // adapt(t1, t2, adapt(t2, t1, e)) == e
            if (e is ZenAdapterExpr<TFrom, TTo> inner1)
            {
                return inner1.Expr;
            }

            if (unroll && e is ZenIfExpr<TFrom> inner2)
            {
                var trueBranch = CreateMulti(inner2.TrueExpr, converters);
                var falseBranch = CreateMulti(inner2.FalseExpr, converters);
                return ZenIfExpr<TTo>.Create(inner2.GuardExpr, trueBranch.Unroll(), falseBranch.Unroll());
            }

            return new ZenAdapterExpr<TTo, TFrom>(e, converters);
        }

        public static Zen<TTo> Create(Zen<TFrom> expr, Func<object, object> converter)
        {
            var converters = ImmutableList<Func<object, object>>.Empty.Add(converter);
            return CreateMulti(expr, converters);
        }

        public static Zen<TTo> CreateMulti(Zen<TFrom> expr, ImmutableList<Func<object, object>> converters, bool unroll = false)
        {
            CommonUtilities.ValidateNotNull(expr);
            var key = (expr, ConvertersHashCode(converters), unroll);
            if (hashConsTable.TryGetValue(key, out var value))
            {
                return value;
            }

            var ret = Simplify(expr, converters, unroll);
            hashConsTable[key] = ret;
            return ret;
        }

        private static int ConvertersHashCode(ImmutableList<Func<object, object>> converters)
        {
            int hash = 7;
            foreach (var converter in converters)
            {
                hash = 31 * hash + converter.GetHashCode();
            }

            return hash;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZenAdapterExpr{TTo, TFrom}"/> class.
        /// </summary>
        /// <param name="expr">The expression.</param>
        /// <param name="converters">Converter between types.</param>
        private ZenAdapterExpr(Zen<TFrom> expr, ImmutableList<Func<object, object>> converters)
        {
            this.Expr = expr;
            this.Converters = converters;
        }

        /// <summary>
        /// Gets the expression.
        /// </summary>
        public Zen<TFrom> Expr { get; }

        /// <summary>
        /// Gets the converter.
        /// </summary>
        public ImmutableList<Func<object, object>> Converters { get; }

        /// <summary>
        /// Convert the expression to a string.
        /// </summary>
        /// <returns>The string representation.</returns>
        [ExcludeFromCodeCoverage]
        public override string ToString()
        {
            return $"adapter({this.Expr})";
        }

        /// <summary>
        /// Implementing the visitor interface.
        /// </summary>
        /// <param name="visitor">The visitor object.</param>
        /// <param name="parameter">The visitor parameter.</param>
        /// <typeparam name="TParam">The visitor parameter type.</typeparam>
        /// <typeparam name="TReturn">The visitor return type.</typeparam>
        /// <returns>A return value.</returns>
        internal override TReturn Accept<TParam, TReturn>(IZenExprVisitor<TParam, TReturn> visitor, TParam parameter)
        {
            return visitor.VisitZenAdapterExpr(this, parameter);
        }
    }
}
