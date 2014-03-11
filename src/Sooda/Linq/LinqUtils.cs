//
// Copyright (c) 2012 Piotr Fusik <piotr@fusik.info>
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
using System.Collections.Generic;
using System.Linq;
using Sooda;
using Sooda.ObjectMapper;
using Sooda.QL;

namespace Sooda.Linq
{
    public static class LinqUtils
    {
        public static bool Like(this string text, string pattern)
        {
            return Sooda.QL.SoqlUtils.Like(text, pattern);
        }

        public static ISoodaObjectList ToSoodaObjectList<T>(this IEnumerable<T> source) where T : SoodaObject
        {
            ISoodaObjectList list = source as ISoodaObjectList;
            if (list != null)
                return list;

            SoodaQueryable<T> query = source as SoodaQueryable<T>;
            if (query != null)
                return query.Provider.Execute<ISoodaObjectList>(query.Expression);

            SoodaObjectListSnapshot snapshot = new SoodaObjectListSnapshot();
            foreach (SoodaObject o in source)
                snapshot.Add(o);
            return snapshot;
        }

        /// <summary>
        /// Converts SoodaQueryable (IQueryable) to SoqlQueryExpression
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static SoqlQueryExpression TranslateExpression(this IQueryable query)
        {
            var provider = query.Provider as SoodaQueryProvider;
            if (provider == null)
                throw new NotSupportedException(string.Format("TranslateExpression(query): provider '{0}' is NOT supported", query.Provider.GetType().FullName));

            return provider.GetSoqlQuery(query.Expression);
        }
    }
}

#endif
