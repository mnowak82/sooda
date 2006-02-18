// 
// Copyright (c) 2002-2005 Jaroslaw Kowalski <jkowalski.sourceforge.net>
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
// * Neither the name of Jaroslaw Kowalski nor the names of its 
//   contributors may be used to endorse or promote products derived from this
//   software without specific prior written permission. 
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

// automatically generated by makewrappers.pl - do not modify

using System;

namespace Sooda.QL.TypedWrappers
{
    public class SoqlTimeSpanWrapperExpression : SoqlTypedWrapperExpression 
    {
        public SoqlTimeSpanWrapperExpression()
        {
        }

        public SoqlTimeSpanWrapperExpression(SoqlExpression innerExpression) : base(innerExpression) { }

        public static implicit operator SoqlTimeSpanWrapperExpression(TimeSpan v)
        {
            return new SoqlTimeSpanWrapperExpression(new SoqlLiteralExpression(v));
        }

        public static implicit operator SoqlTimeSpanWrapperExpression(SoqlParameterLiteralExpression v)
        {
            return new SoqlTimeSpanWrapperExpression(v);
        }

        public static SoqlBooleanExpression operator ==(SoqlTimeSpanWrapperExpression left, SoqlTimeSpanWrapperExpression right) { return new Sooda.QL.SoqlBooleanRelationalExpression(left, right, Sooda.QL.SoqlRelationalOperator.Equal); }
        public static SoqlBooleanExpression operator !=(SoqlTimeSpanWrapperExpression left, SoqlTimeSpanWrapperExpression right) { return new Sooda.QL.SoqlBooleanRelationalExpression(left, right, Sooda.QL.SoqlRelationalOperator.NotEqual); }
        
        public SoqlBooleanExpression In(params SoqlTimeSpanWrapperExpression[] inExpressions)
        {
            SoqlExpressionCollection rhs = new SoqlExpressionCollection();
            foreach (SoqlTimeSpanWrapperExpression e in inExpressions)
            {
                rhs.Add(e);
            }
            return new SoqlBooleanInExpression(this, rhs);
        }

        public override bool Equals(object o) { return Object.ReferenceEquals(this, o); }
        public override int GetHashCode() { return base.GetHashCode(); }
        public static SoqlBooleanExpression operator <=(SoqlTimeSpanWrapperExpression left, SoqlTimeSpanWrapperExpression right) { return new Sooda.QL.SoqlBooleanRelationalExpression(left, right, Sooda.QL.SoqlRelationalOperator.LessOrEqual); }
        public static SoqlBooleanExpression operator >=(SoqlTimeSpanWrapperExpression left, SoqlTimeSpanWrapperExpression right) { return new Sooda.QL.SoqlBooleanRelationalExpression(left, right, Sooda.QL.SoqlRelationalOperator.GreaterOrEqual); }
        public static SoqlBooleanExpression operator <(SoqlTimeSpanWrapperExpression left, SoqlTimeSpanWrapperExpression right) { return new Sooda.QL.SoqlBooleanRelationalExpression(left, right, Sooda.QL.SoqlRelationalOperator.Less); }
        public static SoqlBooleanExpression operator >(SoqlTimeSpanWrapperExpression left, SoqlTimeSpanWrapperExpression right) { return new Sooda.QL.SoqlBooleanRelationalExpression(left, right, Sooda.QL.SoqlRelationalOperator.Greater); }
    }

}
