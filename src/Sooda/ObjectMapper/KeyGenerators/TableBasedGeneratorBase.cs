//
// Copyright (c) 2003-2006 Jaroslaw Kowalski <jaak@jkowalski.net>
// Copyright (c) 2006-2014 Piotr Fusik <piotr@fusik.info>
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
using System.Data;

namespace Sooda.ObjectMapper.KeyGenerators
{
    public abstract class TableBasedGeneratorBase
    {
        readonly string keyName;
        protected readonly int poolSize;
        static readonly Random random = new Random();
        readonly Sooda.Schema.DataSourceInfo dataSourceInfo;
        readonly string table_name;
        readonly string key_name_column;
        readonly string key_value_column;

        protected TableBasedGeneratorBase(string keyName, Sooda.Schema.DataSourceInfo dataSourceInfo)
        {
            this.keyName = keyName;
            this.dataSourceInfo = dataSourceInfo;

            table_name = SoodaConfig.GetString(dataSourceInfo.Name + ".keygentable.name", "KeyGen");
            key_name_column = SoodaConfig.GetString(dataSourceInfo.Name + ".keygentable.keycolumn", "key_name");
            key_value_column = SoodaConfig.GetString(dataSourceInfo.Name + ".keygentable.valuecolumn", "key_value");
            poolSize = Convert.ToInt32(SoodaConfig.GetString(dataSourceInfo.Name + ".keygentable.pool_size", "10"));
        }

        protected long AcquireNextRange()
        {
#if MONO
            return AcquireNextRangeInternal();
#else
            using (System.Transactions.TransactionScope ts = new System.Transactions.TransactionScope(System.Transactions.TransactionScopeOption.Suppress))
            {
                return AcquireNextRangeInternal();
            }
#endif
        }

        long AcquireNextRangeInternal()
        {
            // TODO - fix me, this is hack

            using (Sooda.Sql.SqlDataSource sds = (Sooda.Sql.SqlDataSource)dataSourceInfo.CreateDataSource())
            {
                sds.Open();

                IDbConnection conn = sds.Connection;

                bool gotKey = false;

                bool justInserted = false;
                int maxRandomTimeout = 2;
                for (int i = 0; i < 10 && !gotKey; ++i)
                {
                    string query = "select " + key_value_column + " from " + table_name + " where " + key_name_column + " = '" + keyName + "'";
                    IDbCommand cmd = conn.CreateCommand();

                    if (!sds.DisableTransactions)
                        cmd.Transaction = sds.Transaction;

                    cmd.CommandText = query;
                    long keyValue = -1;

                    using (IDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                            keyValue = Convert.ToInt64(reader.GetValue(0));
                    }

                    if (keyValue == -1)
                    {
                        if (justInserted)
                            throw new Exception("FATAL DATABASE ERROR - cannot get new key value");
                        cmd.CommandText = "insert into " + table_name + "(" + key_name_column + ", " + key_value_column + ") values('" + keyName + "', 1)";
                        cmd.ExecuteNonQuery();
                        justInserted = true;
                        continue;
                    }

                    //Console.WriteLine("Got key: {0}", keyValue);
                    //Console.WriteLine("Press any key to update database (simulating possible race condition here).");
                    //Console.ReadLine();

                    long nextKeyValue = keyValue + poolSize;

                    cmd.CommandText = "update " + table_name + " set " + key_value_column + " = " + nextKeyValue + " where " + key_name_column + " = '" + keyName + "' and " + key_value_column + " = " + keyValue;
                    int rows = cmd.ExecuteNonQuery();
                    // Console.WriteLine("{0} row(s) affected", rows);

                    if (rows != 1)
                    {
                        // Console.WriteLine("Conflict on write, sleeping for random number of milliseconds ({0} max)", maxRandomTimeout);
                        System.Threading.Thread.Sleep(1 + random.Next(maxRandomTimeout));
                        maxRandomTimeout = maxRandomTimeout * 2;
                        // conflict on write
                        continue;
                    }
                    else
                    {
                        sds.Commit();

                        //Console.WriteLine("New key range for {0} [{1}:{2}]", keyName, currentValue, maxValue);
                        return keyValue;
                    }
                }
                throw new Exception("FATAL DATABASE ERROR - cannot get new key value");
            }
        }
    }
}
