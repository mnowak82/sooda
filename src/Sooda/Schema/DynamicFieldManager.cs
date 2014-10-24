//
// Copyright (c) 2014 Piotr Fusik <piotr@fusik.info>
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

using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

using Sooda;
using Sooda.Logging;
using Sooda.Sql;

namespace Sooda.Schema
{
    public class DynamicFieldManager
    {
        static readonly Logger logger = LogManager.GetLogger("Sooda.Schema.DynamicFieldsManager");

        static void Add(FieldInfo fi)
        {
            ClassInfo ci = fi.ParentClass;

            TableInfo table = new TableInfo();
            table.DBTableName = ci.Name + "_" + fi.Name;

            // copy primary key fields
            FieldInfo[] pks = ci.GetPrimaryKeyFields();
            for (int i = 0; i < pks.Length; i++)
            {
                FieldInfo pk = pks[i];
                table.Fields.Add(new FieldInfo {
                    Name = pk.Name,
                    DataType = pk.DataType,
                    Size = pk.Size,
                    Precision = pk.Precision,
                    References = pk.References,
                    IsPrimaryKey = true,
                    DBColumnName = i == 0 ? "id" : "id" + (i + 1) });
            }

            fi.DBColumnName = "value";
            table.Fields.Add(fi);

            ci.LocalTables.Add(table);
        }

        static void Resolve(SchemaInfo schema, HashSet<ClassInfo> affectedClasses)
        {
            // add subclasses
            foreach (ClassInfo ci in affectedClasses.ToArray())
            {
                foreach (ClassInfo sci in schema.GetSubclasses(ci))
                    affectedClasses.Add(sci);
            }

            schema.Resolve(affectedClasses);
        }

        static void Resolve(ClassInfo ci)
        {
            Resolve(ci.Schema, new HashSet<ClassInfo> { ci });
        }

        internal static void Load(SoodaTransaction transaction)
        {
            SchemaInfo schema = transaction.Schema;
            if (schema.DynamicFieldsLoaded || !schema.DataSources.Any(ds => ds.EnableDynamicFields))
                return;
            
            lock (schema)
            {
                if (schema.DynamicFieldsLoaded)
                    return;

                HashSet<ClassInfo> affectedClasses = new HashSet<ClassInfo>();
                foreach (DataSourceInfo dsi in schema.DataSources)
                {
                    SoodaDataSource ds = transaction.OpenDataSource(dsi);
                    using (IDataReader r = ds.ExecuteRawQuery("select class, field, type, nullable, size, precision from SoodaDynamicField"))
                    {
                        while (r.Read())
                        {
                            string className = r.GetString(0);
                            ClassInfo ci = schema.FindClassByName(className);
                            if (ci == null)
                            {
                                logger.Warn("Ignoring a dynamic field of non-existent class {0} -- see the SoodaDynamicField table", className);
                                continue;
                            }

                            FieldInfo fi = new FieldInfo();
                            fi.ParentClass = ci;
                            fi.Name = r.GetString(1);
                            fi.TypeName = r.GetString(2);
                            fi.IsNullable = r.GetInt32(3) != 0;
                            if (!r.IsDBNull(4))
                                fi.Size = r.GetInt32(4);
                            if (!r.IsDBNull(5))
                                fi.Precision = r.GetInt32(5);
                            Add(fi);

                            affectedClasses.Add(ci);
                        }
                    }
                }

                Resolve(schema, affectedClasses);

                schema.DynamicFieldsLoaded = true;
            }
        }

        public static void Invalidate(SchemaInfo schema)
        {
            if (schema.DynamicFieldsLoaded)
            {
                lock (schema)
                {
                    schema.DynamicFieldsLoaded = false;
                }
            }
        }

        static int? NegativeToNull(int i)
        {
            if (i < 0)
                return null;
            return i;
        }

        public static void Add(FieldInfo fi, SoodaTransaction transaction)
        {
            ClassInfo ci = fi.ParentClass;
            SqlDataSource ds = (SqlDataSource) transaction.OpenDataSource(ci.GetDataSource());
            Add(fi);
            Resolve(ci); // initializes fi.Table

            StringWriter sw = new StringWriter();
            sw.Write("insert into SoodaDynamicField (class, field, type, nullable, size, precision) values ({0}, {1}, {2}, {3}, {4}, {5});\n");
            ds.SqlBuilder.GenerateCreateTable(sw, fi.Table, null, ";\n");
            ds.SqlBuilder.GeneratePrimaryKey(sw, fi.Table, null, ";\n");
            ds.SqlBuilder.GenerateForeignKeys(sw, fi.Table, ";\n");
            ds.ExecuteNonQuery(sw.ToString(), ci.Name, fi.Name, fi.TypeName, fi.IsNullable ? 1 : 0, NegativeToNull(fi.Size), NegativeToNull(fi.Precision));
        }

        public static void Remove(FieldInfo fi, SoodaTransaction transaction)
        {
            ClassInfo ci = fi.ParentClass;
            SoodaDataSource ds = transaction.OpenDataSource(ci.GetDataSource());

            ds.ExecuteNonQuery("delete from SoodaDynamicField where class={0} and field={1};\ndrop table " + fi.Table.DBTableName, ci.Name, fi.Name);

            ci.LocalTables.Remove(fi.Table);
            Resolve(ci);
        }
    }
}

#endif