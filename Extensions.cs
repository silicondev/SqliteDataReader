using Org.BouncyCastle.Asn1.X509.Qualified;
using Org.BouncyCastle.Crypto;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SqliteDataReader
{
    public static class Extensions
    {
        public static void ShowMessage(this Exception e)
        {
            var result = MessageBox.Show(
                $"An error has occurred.{Environment.NewLine}" +
                $"{e.Message}{Environment.NewLine}{e.StackTrace}{Environment.NewLine}{Environment.NewLine}" +
                "Would you like to continue running the application?",
                "Error!",
                MessageBoxButton.YesNo);

            if (result == MessageBoxResult.No)
                Application.Current.Shutdown();
        }

        public static async Task<DataTable?> ToDataTable(this IDataReader reader)
        {
            var sw = new Stopwatch();
            var msl1 = new List<double>();
            var msl2 = new List<double>();

            DataTable? dt = null;
            var columns = new List<string>();
            while (reader.Read())
            {
                if (dt == null)
                {
                    sw.Restart();
                    dt = new DataTable();
                    for (int i = 0; true; i++)
                    {
                        try
                        {
                            string name = reader.GetName(i);
                            if (string.IsNullOrEmpty(name))
                                break;

                            columns.Add(name);
                        }
                        catch (Exception)
                        {
                            break;
                        }
                    }
                    columns.ForEach(x => dt.Columns.Add(x));
                    sw.Stop();
                    msl1.Add(sw.Elapsed.TotalMilliseconds);
                }

                sw.Restart();
                var row = dt.NewRow();

                var tasks = columns.Select<string, Func<Task>>(x => () =>
                {
                    int ord = reader.GetOrdinal(x);

                    if (reader.IsDBNull(ord))
                        return Task.CompletedTask;

                    Type type = reader.GetFieldType(ord);

                    try
                    {
                        if (x.Contains("date") && type != typeof(DateTime))
                        {
                            object dtVal =
                                _conversionMatrix.ContainsKey(type) ?
                                _conversionMatrix[type](reader, ord) : _conversionMatrix[typeof(object)](reader, ord);

                            if (DateTime.TryParse(dtVal.ToString(), out _))
                                type = typeof(DateTime);
                        }

                        object val = _conversionMatrix.ContainsKey(type) ?
                        _conversionMatrix[type](reader, ord) : _conversionMatrix[typeof(object)](reader, ord);
                        row[x] = val;
                    }
                    catch (Exception e)
                    {
                        //throw new Exception($"Error trying to parse column {x} of type {type.Name} content '{reader.GetValue(ord)}'", e);
                        row[x] = null;
                    }
                    return Task.CompletedTask;
                });
                await Task.WhenAll(tasks.Select(x => x()));

                dt.Rows.Add(row);
                sw.Stop();
                msl2.Add(sw.Elapsed.TotalMilliseconds);
            }

            double msla1 = msl1.Sum() / msl1.Count();
            double msla2 = msl2.Sum() / msl2.Count();

            return dt;
        }

        public static Dictionary<string, (Type type, List<object?> rows)> ExtractReader(this IDataReader reader)
        {
            var dict = new Dictionary<string, (Type type, List<object?> rows)>();
            var names = new Dictionary<int, string>();
            reader.Read();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                string name = reader.GetName(i);
                names.Add(i, name);
                if (!dict.ContainsKey(name))
                    dict.Add(name, (reader.GetFieldType(i), new List<object>()));
            }

            var values = new List<object?[]>();

            do
            {
                var vals = new object?[reader.FieldCount];
                reader.GetValues(vals);
                values.Add(vals);
            }
            while (reader.Read());

            foreach (object[] row in values)
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    dict[names[i]].rows.Add(row[i]);
                }
            }

            return dict;
        }

        public static DataTable ToDataTable(this Dictionary<string, (Type type, List<object?> rows)> dict)
        {
            var dt = new DataTable();

            foreach (string key in dict.Keys)
            {
                //dt.Columns.Add(new DataColumn()
                //{
                //    ColumnName = key,
                //    DataType = dict[key].type,
                //});
                dt.Columns.Add(key);
            }

            int l = dict.First().Value.rows.Count();

            for (int i = 0; i < l; i++)
            {
                var row = dt.NewRow();
                foreach (DataColumn col in dt.Columns)
                {
                    object? val = dict[col.ColumnName].rows[i];
                    if (val == null)
                        continue;
                    var type = dict[col.ColumnName].type;
                    string typeName = type.Name;

                    if (_forceConverstionMatrix.ContainsKey(type))
                        val = _forceConverstionMatrix[type](val);

                    row[col.ColumnName] = val;
                }
                dt.Rows.Add(row);
            }
            return dt;
        }

        private static Dictionary<Type, Func<IDataReader, int, object>> _conversionMatrix = new Dictionary<Type, Func<IDataReader, int, object>>()
        {
            { typeof(object),   (r, o) => r.GetValue(o) },
            { typeof(string),   (r, o) => r.GetString(o) },
            { typeof(short),    (r, o) => r.GetInt16(o) },
            { typeof(int),      (r, o) => r.GetInt32(o) },
            { typeof(long),     (r, o) => r.GetInt64(o) },
            { typeof(float),    (r, o) => r.GetFloat(o) },
            { typeof(double),   (r, o) => r.GetDouble(o) },
            { typeof(decimal),  (r, o) => r.GetDecimal(o) },
            { typeof(bool),     (r, o) => r.GetBoolean(o) },
            { typeof(byte),     (r, o) => r.GetByte(o) },
            { typeof(Guid),     (r, o) => r.GetGuid(o) },
            { typeof(DateTime), (r, o) => 
                {
                    try
                    {
                        return r.GetDateTime(o);
                    }
                    catch (Exception)
                    {
                        long epoch = r.GetInt64(o);
                        var dto = DateTimeOffset.FromUnixTimeMilliseconds(epoch);
                        return dto.DateTime;
                    }
                }
            },
            { typeof(byte[]),   (r, o) => 
                {
                    var val = (byte[])r.GetValue(o);

                    if (val.Length == 16)
                    {
                        // I have no idea why the sqlite file holds guids like this
                        var newOrder = new byte[]
                        {
                            val[3], val[2], val[1], val[0],
                            val[5], val[4],
                            val[7], val[6],
                            val[8], val[9],
                            val[10], val[11], val[12], val[13], val[14], val[15]
                        };
                        val = newOrder;
                    }

                    return val.Length == 16 ? new Guid(val) : val;
                } 
            }
        };

        private static Dictionary<Type, Func<object, object>> _forceConverstionMatrix = new Dictionary<Type, Func<object, object>>()
        {
            { typeof(DateTime), (v) =>
                {
                    try
                    {
                        return (DateTime)v;
                    }
                    catch (Exception)
                    {
                        long epoch = (long)v;
                        var dto = DateTimeOffset.FromUnixTimeMilliseconds(epoch);
                        return dto.DateTime;
                    }
                }
            },
            { typeof(byte[]),   (v) =>
                {
                    var val = (byte[])v;

                    if (val.Length == 16)
                    {
                        // I have no idea why the sqlite file holds guids like this
                        var newOrder = new byte[]
                        {
                            val[3], val[2], val[1], val[0],
                            val[5], val[4],
                            val[7], val[6],
                            val[8], val[9],
                            val[10], val[11], val[12], val[13], val[14], val[15]
                        };
                        val = newOrder;
                    }

                    return val.Length == 16 ? new Guid(val) : val;
                }
            }
        };
    }
}
