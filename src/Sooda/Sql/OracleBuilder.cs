// 
// Copyright (c) 2003-2006 Jaroslaw Kowalski <jaak@jkowalski.net>
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

using System;

using Sooda.Schema;

namespace Sooda.Sql
{
    public class OracleBuilder : SqlBuilderNamedArg
    {
        private int _internalCounter = 0;

        public override string GetDDLCommandTerminator()
        {
            return Environment.NewLine + "GO" + Environment.NewLine + Environment.NewLine;
        }

        public override string GetSQLDataType(Sooda.Schema.FieldInfo fi)
        {
            switch (fi.DataType)
            {
                case FieldDataType.Integer:
                    return "integer";

                case FieldDataType.AnsiString:
                    if (fi.Size >= 4000)
                        return "clob";
                    else
                        return "varchar2(" + fi.Size + ")";

                case FieldDataType.String:
                    if (fi.Size >= 4000)
                        return "nclob";
                    else
                        return "nvarchar2(" + fi.Size + ")";

                case FieldDataType.Decimal:
                    if (fi.Size < 0)
                        return "number";
                    else if (fi.Precision < 0)
                        return "number(" + fi.Size + ")";
                    else
                        return "number(" + fi.Size + "," + fi.Precision + ")";

                case FieldDataType.Double:
                    if (fi.Size < 0)
                        return "float";
                    else if (fi.Precision < 0)
                        return "float(" + fi.Size + ")";
                    else
                        return "float(" + fi.Size + "," + fi.Precision + ")";

                case FieldDataType.Float:
                    if (fi.Size < 0)
                        return "float";
                    else if (fi.Precision < 0)
                        return "float(" + fi.Size + ")";
                    else
                        return "float(" + fi.Size + "," + fi.Precision + ")";

                case FieldDataType.DateTime:
                    return "date";

                case FieldDataType.Image:
                    return "blob";

                case FieldDataType.BooleanAsInteger:
                    return "integer";

                case FieldDataType.TimeSpan:
                    return "integer";

                case FieldDataType.Long:
                    return "integer";

                case FieldDataType.Boolean:
                    return "byte";

                default:
                    throw new NotImplementedException(String.Format("Datatype {0} not supported for this database", fi.DataType.ToString()));
            }
        }

		// truncate identifier and add a generated number to have unique identifiers
        private string TruncateIdentifier(string identifier, int length)
        {
            if (identifier.Length < length)
                return identifier;
            string num = "_" + _internalCounter.ToString();
            _internalCounter ++;
            return identifier.Substring(0, length - num.Length) + num;
            
        }

        public override string GetConstraintName(string tableName, string foreignKey)
        {
        	// we have to truncate FK name - length of object name must be < 30
            string res = base.GetConstraintName(tableName, foreignKey);
            return TruncateIdentifier(res, 30);
        }

        protected override string GetNameForParameter(int pos)
        {
            return ":p" + pos.ToString();
        }

        public override string QuoteFieldName(string s)
        {
            return String.Concat("\"", s, "\"");
        }

        public override SqlTopSupportMode TopSupport
        {
            get
            {
                return SqlTopSupportMode.Oracle;
            }
        }


        public override SqlOuterJoinSyntax OuterJoinSyntax
        {
            get
            {
                return SqlOuterJoinSyntax.Oracle;
            }
        }

    }
}
