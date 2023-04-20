using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UMC.Data.Sql;
using UMC.Net;

namespace UMC.Data.Caches
{
    public sealed class Cache : ICacheSet
    {
        RecordColumn[] _Columns;
        List<byte[]> MKeys = new List<byte[]>();

        DateTime _ShrinkTime;
        String DbFile;

        public Cache(string name, Record record, params String[] primaryKeys)
        {

            this._Name = name;
            var keys = new List<byte>();
            var signed = new List<RecordColumn>();
            var sb = new List<byte>();

            sb.AddRange(System.Text.Encoding.UTF8.GetBytes(this._Name));

            this.DbFile = Reflection.ConfigPath(String.Format("Database\\{0}.udb", this._Name));

            if (System.IO.File.Exists(this.DbFile))
            {
                try
                {
                    using (System.IO.FileStream image = System.IO.File.OpenRead(this.DbFile))
                    {
                        byte bf = 0;
                        var IsFrist = true;
                        var data = new List<byte>();
                        image.Seek(16, System.IO.SeekOrigin.Begin);
                        var buffer = new byte[1];
                        while (image.Read(buffer, 0, 1) == 1)
                        {
                            var v = buffer[0];
                            data.Add(v);

                            if (v == 10 && bf == 13)
                            {
                                if (IsFrist)
                                {
                                    IsFrist = false;
                                    data.Clear();
                                }
                                else if (data.Count == 2)
                                {
                                    break;
                                }
                                else
                                {
                                    var strField = Encoding.UTF8.GetString(data.ToArray(), 1, data.Count - 3);
                                    var column = new RecordColumn(strField, data[0]);
                                    sb.Add((byte)signed.Count);
                                    sb.Add(column.TypeCode);
                                    signed.Add(column);
                                    data.Clear();
                                }
                            }
                            bf = buffer[0];
                        }
                    }
                }
                catch
                {

                }
                var rcols = record.GetColumns();
                for (byte i = 0; i < rcols.Length; i++)
                {
                    var column = signed.FirstOrDefault(s => String.Equals(s.Name, rcols[i].Name, StringComparison.CurrentCultureIgnoreCase));
                    if (column != null)
                    {
                        column.Type = column.Type;
                    }
                    else
                    {

                        column = rcols[i];
                        sb.Add((byte)signed.Count);
                        sb.Add(column.TypeCode);
                        signed.Add(column);
                    }
                }
            }
            else
            {

                var rcols = record.GetColumns();
                for (byte i = 0; i < rcols.Length; i++)
                {
                    var column = rcols[i];
                    sb.Add(i);
                    sb.Add(column.TypeCode);
                    signed.Add(column);
                }
            }
            _Columns = signed.ToArray();

            using (var md5 = new System.Security.Cryptography.MD5CryptoServiceProvider())
            {
                var d = md5.ComputeHash(sb.ToArray());

                for (var i = 1; i < 4; i++)
                {
                    d[0] += d[i * 4];
                    d[1] += d[i * 4 + 1];
                    d[2] += d[i * 4 + 2];
                    d[3] += d[i * 4 + 3];
                }
                this._NameCode = BitConverter.ToInt32(d, 0);
            }
            foreach (var k in primaryKeys)
            {
                for (byte i = 0; i < this._Columns.Length; i++)
                {
                    if (String.Equals(_Columns[i].Name, k))
                    {
                        keys.Add(i);
                        break;
                    }
                }
            }
            if (keys.Count > 0 && keys.Count < 5)
            {
                this.MKeys.Add(keys.ToArray());
                dataCache = new CacheSet(keys.ToArray());
            }


        }

        int _NameCode;
        String _Name;

        public Cache Register(params String[] indexKeys)
        {
            if (indexKeys.Length > 0)
            {
                var keys = new List<byte>();
                foreach (var k in indexKeys)
                {
                    for (byte i = 0; i < this._Columns.Length; i++)
                    {
                        if (String.Equals(_Columns[i].Name, k))
                        {
                            keys.Add(i);
                            break;
                        }
                    }
                }
                if (keys.Count > 0 && keys.Count < 5)
                {
                    this.MKeys.Add(keys.ToArray());
                    this.indexKeys.Add(new CacheSet(keys.ToArray()));
                }
            }
            return this;
        }

        System.IO.FileStream log;

        void Sync(byte[] buffer, int offset, int count)
        {
            var start = offset;
            var end = offset + count;
            var row = new List<byte[]>();
            while (offset < end)
            {
                byte pindex = buffer[offset];
                offset++;
                if (pindex >= 16)
                {
                    var len = BitConverter.ToInt32(buffer, offset);
                    offset += 4;
                    var buffer2 = new byte[len + 1];
                    Array.Copy(buffer, offset, buffer2, 1, len);

                    pindex -= 16;

                    buffer2[0] = pindex;
                    row.Add(buffer2);
                    offset += len + 2;


                }
                else if (offset < end)
                {
                    if (buffer[offset] == 10)
                    {
                        switch (pindex)
                        {
                            case 13:
                                this.Put(row.ToArray());
                                if (_isLoad)
                                {
                                    log.Write(buffer, start, count);
                                    log.Flush();
                                }
                                break;
                            case 14:
                                this.Delete(row.ToArray());
                                if (_isLoad)
                                {
                                    log.Write(buffer, start, count);
                                    log.Flush();
                                }
                                break;
                            case 12:
                                this.Put(row.ToArray());
                                break;
                            default:
                                break;
                        }
                    }

                }
            }
        }
        void Log(byte[][] row, bool isPut)
        {
            if (_isLoad)
            {
                var bytes = new List<byte>();
                foreach (var cell in row)
                {
                    byte index = cell[0];
                    index += 16;
                    bytes.Add(index);
                    var l = cell.Length - 1;
                    bytes.AddRange(BitConverter.GetBytes(l));
                    for (var i = 1; i <= l; i++)
                    {
                        bytes.Add(cell[i]);
                    }
                    bytes.Add(13);
                    bytes.Add(10);
                }
                if (isPut)
                {
                    bytes.Add(13);
                    bytes.Add(10);
                }
                else
                {
                    bytes.Add(14);
                    bytes.Add(10);
                }
                var bs = bytes.ToArray();
                _logbuffers.Enqueue(bs);
                if (_isWriteLog == false && _isSaving == false)
                {
                    _isWriteLog = true;
                    System.Threading.Tasks.Task.Factory.StartNew(_SaveLog);
                }
                NetSubscribe.Publish(this._NameCode, bs, 0, bs.Length);

            }
        }
        int _change = 0;
        void _SaveLog()
        {
            byte[] blog;
            try
            {
                while (_logbuffers.TryPeek(out blog) && !_isSaving)
                {

                    log.Write(blog, 0, blog.Length);
                    _logbuffers.TryDequeue(out blog);
                    _change++;

                }

                log.Flush();
            }
            catch
            {
                return;
            }
            if (_change > 10000 && !_isSaving)
            {
                _change = 0;
                ICacheSet lset = this;
                lset.Save();
                _isWriteLog = false;
            }
            else
            {

                _isWriteLog = false;
            }

        }
        ConcurrentQueue<byte[]> _logbuffers = new ConcurrentQueue<byte[]>();
        object _lock = new object();
        bool _isLoad, _isSaving, _isWriteLog;
        void _Save()
        {

            var filename = Reflection.ConfigPath(String.Format("Database\\{0}.udb.loading", this._Name));

            if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(filename)))
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filename));
            }

            if (System.IO.File.Exists(filename))
            {
                return;
            }
            _change = 0;
            _isSaving = true;


            if (log != null)
            {
                log.Flush();
                log.Close();
                log.Dispose();
            }


            _ShrinkTime = DateTime.Now;
            using (var writer = System.IO.File.OpenWrite(filename))
            {
                writer.Write(BitConverter.GetBytes(dataCache.Count), 0, 4);
                writer.Write(BitConverter.GetBytes(UMC.Data.Utility.TimeSpan(_ShrinkTime)), 0, 4);

                writer.Seek(16, System.IO.SeekOrigin.Begin);
                foreach (var k in this.MKeys)
                {
                    var mk = new byte[4];
                    var len = mk.Length - k.Length;
                    for (var m = 0; m < k.Length; m++)
                    {
                        mk[len + m] = k[m];
                        mk[len + m] += 16;
                    }
                    writer.Write(mk, 0, 4);
                }

                writer.Write(new byte[] { 13, 10 }, 0, 2);

                for (var i = 0; i < this._Columns.Length; i++)
                {
                    var col = this._Columns[i];
                    writer.Write(new byte[] { col.TypeCode }, 0, 1);
                    var pdata = Encoding.UTF8.GetBytes(col.Name);
                    writer.Write(pdata, 0, pdata.Length);
                    writer.Write(new byte[] { 13, 10 }, 0, 2);
                }

                writer.Write(new byte[] { 13, 10 }, 0, 2);

                writer.Seek(256 * 256, System.IO.SeekOrigin.Begin);

                dataCache.Search(0, false, row =>
                {

                    foreach (var cell in row)
                    {
                        byte index = cell[0];
                        index += 16;
                        writer.Write(new byte[] { index }, 0, 1);
                        var l = cell.Length - 1;
                        writer.Write(BitConverter.GetBytes(l), 0, 4);
                        writer.Write(cell, 1, l);
                        writer.Write(new byte[] { 13, 10 }, 0, 2);
                    }
                    writer.Write(new byte[] { 13, 10 }, 0, 2);

                    return false;
                });
                writer.Flush();
                writer.Close();
                writer.Dispose();
            }

            if (System.IO.File.Exists(this.DbFile))
            {
                System.IO.File.Move(this.DbFile, this.DbFile + ".detting");
                System.IO.File.Move(filename, this.DbFile);
                System.IO.File.Delete(this.DbFile + ".detting");
            }
            else
            {
                System.IO.File.Move(filename, this.DbFile);
            }
            this.log = System.IO.File.Open(this.DbFile, System.IO.FileMode.Append);

            this._isSaving = false;

        }
        CacheSet dataCache;

        List<CacheSet> indexKeys = new List<CacheSet>();

        int ICacheSet.Count => dataCache.Count;

        string ICacheSet.Name => _Name;
        DateTime ICacheSet.ShrinkTime => _ShrinkTime;

        int ICacheSet.NameCode => _NameCode;

        internal byte[][] Split(Hashtable vale)
        {
            var row = new List<byte[]>();
            for (byte i = 0; i < this._Columns.Length; i++)
            {
                var cl = this._Columns[i];
                if (vale.ContainsKey(cl.Name))
                {
                    var value = vale[cl.Name];
                    if (value.GetType() == cl.Type)
                    {
                        var v = RecordColumn.Parse(value);

                        byte[] cell = new byte[v.Length + 1];
                        cell[0] = i;
                        Array.Copy(v, 0, cell, 1, v.Length);
                        row.Add(cell);
                    }
                    else if (value is String)
                    {
                        var v = RecordColumn.Parse(Reflection.Parse(value as string, cl.Type));
                        if (v != null)
                        {
                            byte[] cell = new byte[v.Length + 1];
                            cell[0] = i;
                            Array.Copy(v, 0, cell, 1, v.Length);
                            row.Add(cell);
                        }
                    }
                }

            }
            return row.ToArray();
        }

        internal byte[][] Split(Record vale)
        {
            var row = new List<byte[]>();
            for (byte i = 0; i < this._Columns.Length; i++)
            {
                vale.GetValues((t, tv) =>
                {
                    if (String.Equals(this._Columns[i].Name, t))
                    {
                        var v = RecordColumn.Parse(tv);
                        byte[] cell = new byte[v.Length + 1];
                        cell[0] = i;
                        Array.Copy(v, 0, cell, 1, v.Length);
                        row.Add(cell);
                    }
                });

            }
            return row.ToArray();
        }
        internal byte[] InValue(Record vale, out DbOperator dbOperator, params object[] values)
        {
            dbOperator = DbOperator.In;
            var inValue = new List<byte>();
            var row = Split(vale);
            if (row.Length == 1)
            {
                inValue.AddRange(row[0]);
                var fieldIndex = row[0][0];

                if (this._Columns[fieldIndex].TypeCode == 15)
                {
                    dbOperator = DbOperator.InStr;
                    inValue.Add(13);
                    for (var i = 0; i < values.Length; i++)
                    {
                        inValue.AddRange(RecordColumn.Parse(values[i]));
                        inValue.Add(13);

                    }

                }
                else
                {
                    for (var i = 0; i < values.Length; i++)
                    {
                        inValue.AddRange(RecordColumn.Parse(values[i]));

                    }
                }
            }
            return inValue.ToArray();
        }
        public void Put(Record vale)
        {
            var bValue = Split(vale);
            if (Put(bValue))
            {
                Log(bValue, true);
            }
        }
        bool Put(byte[][] value)
        {
            int keyIndex;
            var mainKey = GetKey(value, out keyIndex);
            if (mainKey.Length > 0 && keyIndex == 0)
            {
                byte[][] oldValue;
                byte[][] row = dataCache.Put(value, out oldValue);
                if (oldValue != null)
                {
                    this.RemoveIndex(oldValue);
                }
                RegisterIndex(row);
                return true;

            }
            return false;


        }

        T Clone<T>(byte[][] values, byte[] rs) where T : Record, new()
        {
            if (values != null)
            {
                T newObject = new T();
                for (var i = 0; i < values.Length; i++)
                {
                    var index = values[i][0];
                    var cln = _Columns[index];
                    try
                    {
                        newObject.SetValue(cln.Name, cln.Parse(values[i], rs));
                    }
                    catch //(Exception ex)
                    {
                        //Utility.Error("Cache", this._Name, cln.Name, ex.Message);
                    }
                }
                return newObject;


            }
            return default(T);
        }
        Hashtable Clone(byte[][] values, byte[] rs)
        {
            if (values != null)
            {
                var value = new Hashtable();
                for (var i = 0; i < values.Length; i++)
                {
                    var index = values[i][0];
                    var cln = _Columns[index];
                    value[cln.Name] = cln.Parse(values[i], rs);

                }
                return value;


            }
            return null;
        }




        public T Get<T>(T t) where T : Record, new()
        {
            var value = Get(Split(t));
            if (value != null)
            {

                return Clone<T>(value, new byte[8]);

            }
            return default(T);
        }
        byte[][] Get(byte[][] value)
        {
            int keyIndex;
            var key = GetKey(value, out keyIndex);
            if (key.Length > 0)
            {
                if (keyIndex == 0)
                {
                    byte[][] sortedValue;
                    if (dataCache.TryGetValue(key, out sortedValue))
                    {
                        return sortedValue;
                    }
                }
                else
                {
                    var indexSet = indexKeys[keyIndex - 1];
                    byte[][] sortedValue;
                    if (indexSet.TryGetValue(key, out sortedValue))
                    {
                        return sortedValue;
                    }
                }
            }
            return null;
        }

        int Find(SearchCell[] searchs, bool isAsc, int nextIndex, int limit, Action<byte[][]> action)
        {
            var search = searchs[0];

            int time = 0;
            switch (search.IndexKey)
            {
                case -1:



                    break;
                case 0:
                    if (search.IsKey)
                    {
                        byte[][] value;

                        foreach (var se in searchs)
                        {
                            if (dataCache.TryGetValue(nextIndex, se.Start, out value, out nextIndex))
                            {
                                se.Get(value, action);
                            }
                        }
                    }
                    else
                    {
                        if (nextIndex > 0 && dataCache.Count > nextIndex)
                        {
                            var compare = dataCache.Comparer();
                            var kkey = dataCache.Key(nextIndex);
                            searchs = searchs.Where(r => compare.Compare(r.End, kkey) > 0).ToArray();
                        }

                        foreach (var se in searchs)
                        {
                            nextIndex = dataCache.Search(nextIndex, isAsc, se.Start, se.End, v =>
                             {
                                 se.Get(v, row =>
                                 {
                                     time++;
                                     action(row);
                                 });
                                 return time >= limit;
                             });
                            if (nextIndex > 0)
                            {
                                return nextIndex;
                            }
                            else
                            {
                                nextIndex = ~nextIndex;
                            }
                        }

                    }
                    break;
                default:
                    var cacheSet = indexKeys[search.IndexKey - 1];
                    if (search.IsKey)
                    {
                        byte[][] value;
                        foreach (var se in searchs)
                        {
                            if (cacheSet.TryGetValue(nextIndex, se.Start, out value, out nextIndex))
                            {
                                se.Get(value, action);
                            }
                        }

                    }
                    else
                    {
                        if (nextIndex > 0 && cacheSet.Count > nextIndex)
                        {
                            var kkey = cacheSet.Key(nextIndex);

                            var compare = dataCache.Comparer();
                            searchs = searchs.Where(r => compare.Compare(r.End, kkey) > 0).ToArray();
                        }
                        foreach (var se in searchs)
                        {
                            nextIndex = cacheSet.Search(nextIndex, isAsc, se.Start, se.End, v =>
                            {
                                se.Get(v, row =>
                                {
                                    time++;
                                    action(row);

                                });
                                return time >= limit;
                            });
                            if (nextIndex >= 0)
                            {
                                return nextIndex;
                            }
                            else
                            {
                                nextIndex = ~nextIndex;
                            }
                        }
                    }
                    break;
            }
            return -1;

        }
        int Find(SearchCell search, bool isDesc, int nextIndex, int limit, Action<byte[][]> action)
        {
            int time = 0;
            switch (search.IndexKey)
            {
                case -1:
                    if (search.Values.Length > 0)
                    {
                        return dataCache.Search(nextIndex, isDesc, value =>
                        {
                            search.Get(value, row =>
                            {
                                time++;
                                action(row);

                            });
                            return time >= limit;
                        });
                    }
                    else
                    {

                        return dataCache.Search(nextIndex, isDesc, row =>
                        {

                            time++;
                            action(row);
                            return time >= limit;
                        });
                    }


                case 0:
                    if (search.IsKey)
                    {
                        byte[][] value;
                        if (dataCache.TryGetValue(search.Start, out value))
                        {
                            search.Get(value, action);
                        }
                    }
                    else
                    {
                        return dataCache.Search(nextIndex, isDesc, search.Start, search.End, v =>
                        {
                            search.Get(v, row =>
                            {
                                time++;
                                action(row);

                            });
                            return time >= limit;
                        });
                    }
                    break;
                default:
                    if (search.IsKey)
                    {
                        byte[][] value;
                        if (indexKeys[search.IndexKey - 1].TryGetValue(search.Start, out value))
                        {
                            search.Get(value, action);

                        }
                    }
                    else
                    {
                        return this.indexKeys[search.IndexKey - 1].Search(nextIndex, isDesc, search.Start, search.End, v =>
                        {
                            search.Get(v, row =>
                            {
                                time++;
                                action(row);

                            });
                            return time >= limit;
                        });
                    }
                    break;
            }
            return -1;
        }

        int Search(SearchCell cellSearch, bool isDesc, int nextIndex, int limit, SearchQuery query, Action<byte[][]> action)
        {

            int time = 0;
            switch (cellSearch.IndexKey)
            {
                case -1:
                    return dataCache.Search(nextIndex, isDesc, value =>
                    {
                        query.Get(value, cellSearch.Values, row =>
                        {
                            time++;
                            action(row);

                        });
                        return time >= limit;
                    });

                case 0:
                    if (cellSearch.IsKey)
                    {
                        byte[][] value;
                        if (dataCache.TryGetValue(cellSearch.Start, out value))
                        {
                            query.Get(value, cellSearch.Values, action);
                        }
                    }
                    else
                    {
                        return dataCache.Search(nextIndex, isDesc, cellSearch.Start, cellSearch.End, v =>
                        {
                            query.Get(v, cellSearch.Values, row =>
                            {
                                time++;
                                action(row);

                            });
                            return time >= limit;
                        });
                    }
                    break;
                default:
                    if (cellSearch.IsKey)
                    {
                        byte[][] value;
                        if (indexKeys[cellSearch.IndexKey - 1].TryGetValue(cellSearch.Start, out value))
                        {
                            query.Get(value, cellSearch.Values, action);

                        }
                    }
                    else
                    {
                        return this.indexKeys[cellSearch.IndexKey - 1].Search(nextIndex, isDesc, cellSearch.Start, cellSearch.End, v =>
                        {
                            query.Get(v, cellSearch.Values, row =>
                            {
                                time++;
                                action(row);

                            });
                            return time >= limit;
                        });
                    }
                    break;
            }
            return -1;
        }
        int Find(SearchCell search, bool isDesc, int nextIndex, int limit, SearchValue searchValue, Action<byte[][]> action)
        {
            int time = 0;
            switch (search.IndexKey)
            {
                case -1:
                    if (search.Values.Length > 0)
                    {
                        return dataCache.Search(nextIndex, isDesc, value =>
                        {
                            search.Get(value, row =>
                            {
                                if (searchValue.Check(row))
                                {
                                    time++;
                                    action(row);

                                }
                            });
                            return time >= limit;
                        });
                    }
                    else
                    {

                        return dataCache.Search(nextIndex, isDesc, value =>
                        {
                            if (searchValue.Check(value))
                            {
                                time++;
                                action(value);
                            }
                            return time >= limit;
                        });
                    }

                case 0:
                    if (search.IsKey)
                    {
                        byte[][] value;
                        if (dataCache.TryGetValue(search.Start, out value))
                        {
                            search.Get(value, r =>
                             {
                                 if (searchValue.Check(r))
                                 {
                                     action(r);
                                 }
                             });
                        }
                    }
                    else
                    {
                        return dataCache.Search(nextIndex, isDesc, search.Start, search.End, v =>
                        {
                            search.Get(v, r =>
                            {
                                if (searchValue.Check(r))
                                {
                                    time++;
                                    action(r);
                                }
                            });
                            return time >= limit;
                        });
                    }
                    break;
                default:
                    if (search.IsKey)
                    {
                        byte[][] value;
                        if (indexKeys[search.IndexKey - 1].TryGetValue(search.Start, out value))
                        {
                            search.Get(value, r =>
                            {
                                if (searchValue.Check(r))
                                {
                                    action(r);
                                }
                            });

                        }
                    }
                    else
                    {
                        return this.indexKeys[search.IndexKey - 1].Search(nextIndex, isDesc, search.Start, search.End, v =>
                        {
                            search.Get(v, r =>
                            {
                                if (searchValue.Check(r))
                                {
                                    time++;
                                    action(r);
                                }
                            });
                            return time >= limit;
                        });
                    }
                    break;
            }
            return -1;
        }
        internal T[] Search<T>(bool isDesc, int index, int limit, out int nextIndex, Searcher<T> searcher, T t) where T : Record, new()
        {
            //var properties = SearchValue.GetProperties(typeof(T));
            var res = new List<T>();
            var k = GetIndex(t);
            var rs = new byte[8];
            SearchQuery searchQuery = new SearchQuery() { Group = searcher.Searches };
            switch (k.IndexKey)
            {
                case -1:
                case 0:
                    {
                        int time = 0;
                        nextIndex = dataCache.Search(index, isDesc, value =>
                        {
                            searchQuery.Get(value, row =>
                            {
                                time++;
                                res.Add(Clone<T>(row, rs));
                            });
                            return time >= limit;
                        });
                    }
                    break;
                default:
                    {
                        int time = 0;
                        nextIndex = this.indexKeys[k.IndexKey - 1].Search(index, isDesc, value =>
                        {
                            searchQuery.Get(value, row =>
                            {
                                time++;
                                res.Add(Clone<T>(row, rs));
                            });
                            return time >= limit;
                        });
                    }
                    break;
            }
            return res.ToArray();
        }

        internal T[] Search<T>(T t, bool isDesc, int index, int limit, out int nextIndex, Searcher<T> searcher) where T : Record, new()
        {
            //var properties = SearchValue.GetProperties(typeof(T));
            var res = new List<T>();
            var k = GetIndex(t);
            var rs = new byte[8];
            SearchQuery searchQuery = new SearchQuery() { Group = searcher.Searches };
            if (k.Values.Length > 0)
            {
                nextIndex = this.Search(k, isDesc, index, limit, searchQuery, row => res.Add(Clone<T>(row, rs)));
            }
            else
            {
                int time = 0;
                nextIndex = dataCache.Search(index, isDesc, value =>
                {
                    searchQuery.Get(value, row =>
                    {
                        time++;
                        res.Add(Clone<T>(row, rs));
                    });
                    return time >= limit;
                });
            }
            return res.ToArray();
        }
        public T[] Find<T>(T t, int index, out int nextIndex) where T : Record, new()
        {
            return Find(t, index, 500, out nextIndex);
        }
        public T[] Find<T>(T t, int index, int limit, out int nextIndex) where T : Record, new()
        {
            return Find(t, false, index, limit, out nextIndex);
        }
        public T[] Find<T>(T t, bool isDesc, int index, int limit, out int nextIndex) where T : Record, new()
        {
            return Find<T>(t, isDesc, index, limit, out nextIndex, String.Empty, new byte[0]);
        }
        public T[] Find<T>(T t, String field, IEnumerable e) where T : Record, new()
        {
            int nextIndex;
            return Find<T>(t, false, 0, 500, out nextIndex, field, e);
        }
        public T[] Find<T>(T t, int index, int limit, out int nextIndex, String field, IEnumerable e) where T : Record, new()
        {
            return Find(t, false, index, limit, out nextIndex, field, e);
        }
        public T[] Find<T>(T t, int index, out int nextIndex, String field, IEnumerable e) where T : Record, new()
        {
            return Find(t, false, index, 500, out nextIndex, field, e);
        }
        public Searcher<T> Search<T>() where T : Record, new()
        {
            return new Searcher<T>(this);
        }
        public T[] Find<T>(T t, bool isDesc, int index, int limit, out int nextIndex, String field, IEnumerable fieldVals) where T : Record, new()
        {
            var fieldValues = new HashSet<object>();
            foreach (var vv in fieldVals)
            {
                fieldValues.Add(vv);
            }
            int fieldIndex = -1;
            //var 
            //var properties = t.Columns();
            for (var i = 0; i < this._Columns.Length; i++)
            {
                if (String.Equals(this._Columns[i].Name, field, StringComparison.CurrentCultureIgnoreCase))
                {
                    fieldIndex = i;
                    break;
                }
            }

            var res = new List<T>();
            var rs = new byte[8];
            if (fieldIndex > -1)
            {
                var pr = this._Columns[fieldIndex];

                t.GetValues((k, v) =>
                {
                    if (String.Equals(pr.Name, k))
                    {
                        fieldValues.Add(v);
                    }
                });
                var fVas = fieldValues.ToArray();
                if (fVas.Length > 0)
                {
                    t.SetValue(pr.Name, fVas[0]);
                    var k = GetIndex(t);
                    if (k.IndexKey > -1)
                    {
                        if (k.Start.Any(r => r[0] == fieldIndex))
                        {
                            var search = new List<SearchCell>();
                            search.Add(k);
                            for (var i = 1; i < fVas.Length; i++)
                            {
                                t.SetValue(pr.Name, fVas[i]);
                                search.Add(GetIndex(t));
                            }
                            //HashSet<>
                            var comparer = new Comparer(k.IndexFields, k.IndexFields.Length);
                            if (isDesc)
                            {
                                nextIndex = this.Find(search.OrderByDescending(r => r.Start, comparer).ToArray(), isDesc, index, limit, row => res.Add(Clone<T>(row, rs)));
                            }
                            else
                            {
                                nextIndex = this.Find(search.OrderBy(r => r.Start, comparer).ToArray(), isDesc, index, limit, row => res.Add(Clone<T>(row, rs)));
                            }

                        }
                        else
                        {

                            SearchValue searchValue = new SearchValue();
                            var inValue = new List<byte>();
                            var cell = k.Values.First(r => r[0] == fieldIndex);
                            inValue.AddRange(cell);
                            k.Values = k.Values.Where(r => r[0] != fieldIndex).ToArray();
                            if (this._Columns[fieldIndex].TypeCode == 15)
                            {
                                inValue.Add(13);
                                for (var i = 1; i < fVas.Length; i++)
                                {
                                    //t.SetValue(pr.Name, fVas[i]);
                                    //pr.SetValue(t, fVas[i]);
                                    inValue.AddRange(RecordColumn.Parse(fVas[i]));
                                    inValue.Add(13);

                                }
                                searchValue.Operat = Sql.DbOperator.InStr;

                            }
                            else
                            {
                                for (var i = 1; i < fVas.Length; i++)
                                {
                                    //pr.SetValue(t, fVas[i]);

                                    inValue.AddRange(RecordColumn.Parse(fVas[i]));
                                }
                                searchValue.Operat = Sql.DbOperator.In;
                            }
                            searchValue.Value = inValue.ToArray();
                            nextIndex = this.Find(k, isDesc, index, limit, searchValue, row => res.Add(Clone<T>(row, rs)));

                        }
                    }
                    else
                    {
                        SearchValue searchValue = new SearchValue();
                        var inValue = new List<byte>();
                        var cell = k.Values.First(r => r[0] == fieldIndex);
                        inValue.AddRange(cell);
                        k.Values = k.Values.Where(r => r[0] != fieldIndex).ToArray();
                        if (this._Columns[fieldIndex].TypeCode == 15)
                        {
                            inValue.Add(13);
                            for (var i = 1; i < fVas.Length; i++)
                            {
                                //pr.SetValue(t, fVas[i]);
                                inValue.AddRange(RecordColumn.Parse(fVas[i]));
                                inValue.Add(13);

                            }
                            searchValue.Operat = Sql.DbOperator.InStr;

                        }
                        else
                        {
                            for (var i = 1; i < fVas.Length; i++)
                            {
                                //pr.SetValue(t, fVas[i]);

                                inValue.AddRange(RecordColumn.Parse(fVas[i]));
                            }
                            searchValue.Operat = Sql.DbOperator.In;
                        }
                        searchValue.Value = inValue.ToArray();

                        nextIndex = this.Find(k, isDesc, index, limit, searchValue, row => res.Add(Clone<T>(row, rs)));
                    }
                }
                else
                {

                    nextIndex = this.Find(GetIndex(t), isDesc, index, limit, row => res.Add(Clone<T>(row, rs)));

                }
            }
            else
            {
                nextIndex = this.Find(GetIndex(t), isDesc, index, limit, row => res.Add(Clone<T>(row, rs)));

            }


            return res.ToArray();
        }
        SearchCell GetIndex(byte[][] searchValue)
        {
            int indexKey = -1;

            var max = 0;
            for (var i = 0; i < this.MKeys.Count; i++)
            {
                int ct = 0;
                foreach (var c in this.MKeys[i])
                {
                    if (searchValue.FirstOrDefault(r => r[0] == c) != null)
                    {
                        ct++;
                    }
                    else
                    {
                        break;
                    }
                }
                if (max < ct)
                {
                    indexKey = i;
                    max = ct;
                }
            }

            var search = new SearchCell();
            search.IndexKey = indexKey;
            if (indexKey > -1)
            {
                var pk = new List<byte[]>();
                var fields = new List<byte>();
                foreach (var c in this.MKeys[indexKey])
                {
                    var cell = searchValue.FirstOrDefault(r => r[0] == c);
                    if (cell != null)
                    {
                        pk.Add(cell);
                        fields.Add(c);
                    }
                    else
                    {
                        break;
                    }
                }
                search.IndexFields = fields.ToArray();
                search.IsKey = pk.Count == this.MKeys[indexKey].Length;
                if (search.IsKey)
                {
                    search.End = search.Start = pk.ToArray();
                }
                else
                {
                    search.Start = Reduce(pk.ToArray());
                    search.End = Increment(pk.ToArray());
                }
            }
            search.Values = searchValue;
            return search;
        }
        SearchCell GetIndex<T>(T searchValue) where T : Record
        {
            return GetIndex(Split(searchValue));
        }
        byte[][] Increment(byte[][] pk)
        {
            var vs = new byte[pk.Length][];
            for (int i = 0; i < pk.Length; i++)
            {
                byte[] r = pk[i];
                var bs = new byte[r.Length];
                Array.Copy(r, bs, bs.Length);
                vs[i] = bs;
            }

            for (var i = vs.Length - 1; i > -1; i--)
            {
                var cell = vs[i];
                for (var c = cell.Length - 1; c > 0; c--)
                {
                    if (cell[c] == 255)
                    {
                        if (i == 0 && c == 1)
                        {
                            return pk;
                        }
                        cell[c] = 0;
                    }
                    else
                    {
                        cell[c]++;
                        return vs;
                    }

                }
            }
            return vs;
        }
        byte[][] Reduce(byte[][] pk)
        {
            var vs = new byte[pk.Length][];
            for (int i = 0; i < pk.Length; i++)
            {
                byte[] r = pk[i];
                var bs = new byte[r.Length];
                Array.Copy(r, bs, bs.Length);
                vs[i] = bs;
            }

            for (var i = vs.Length - 1; i > -1; i--)
            {
                var cell = vs[i];
                for (var c = cell.Length - 1; c > 0; c--)
                {
                    if (cell[c] == 0)
                    {
                        if (i == 0 && c == 1)
                        {
                            return pk;
                        }
                        cell[c] = 255;
                    }
                    else
                    {
                        cell[c]--;
                        return vs;
                    }

                }
            }
            return vs;
        }

        void RemoveIndex(byte[][] row)
        {

            for (var i = 1; i < MKeys.Count; i++)
            {
                var pk = MKeys[i];
                var values = new List<byte[]>();
                foreach (var p in pk)
                {
                    var va = row.FirstOrDefault(r => r[0] == p);
                    if (va != null)
                    {
                        values.Add(va);
                    }
                    else
                    {
                        values.Clear();
                        break;
                    }
                }
                if (values.Count > 0)
                {
                    this.indexKeys[i - 1].Delete(row);
                }
            }
        }
        void RegisterIndex(byte[][] row)
        {
            for (var i = 1; i < MKeys.Count; i++)
            {
                var pk = MKeys[i];
                var values = new List<byte[]>();
                foreach (var p in pk)
                {
                    var va = row.FirstOrDefault(r => r[0] == p);
                    if (va != null)
                    {
                        values.Add(va);
                    }
                    else
                    {
                        values.Clear();
                        break;
                    }
                }
                if (values.Count > 0)
                {
                    byte[][] oldValue;
                    indexKeys[i - 1].Put(row, out oldValue);
                }
            }
        }

        bool Delete(byte[][] value)
        {
            int keyIndex;
            var key = GetKey(value, out keyIndex);
            if (key.Length > 0 && keyIndex == 0)
            {
                var row = dataCache.Delete(key);
                if (row != null)
                {

                    RemoveIndex(row);
                    return true;
                }
            }
            return false;


        }
        public void Delete<T>(T value) where T : Record
        {
            var bValue = Split(value);
            if (Delete(bValue))
            {
                Log(bValue, false);

            }

        }
        byte[][] GetKey(byte[][] row, out int keyIndex)
        {
            List<byte[]> values = new List<byte[]>();
            keyIndex = -1;
            for (var i = 0; i < MKeys.Count; i++)
            {
                keyIndex = i;
                var pk = MKeys[i];

                foreach (var p in pk)
                {
                    var va = row.FirstOrDefault(r => r[0] == p);
                    if (va != null)
                    {
                        values.Add(va);
                    }
                    else
                    {
                        values.Clear();
                        break;
                    }
                }
                if (values.Count > 0)
                {
                    break;
                }
            }
            return values.ToArray();

        }

        void ICacheSet.Load()
        {
            if (_isLoad)
            {
                return;
            }
            else
            {
                _isLoad = true;
            }
            ICacheSet sacheSet = this;

            if (System.IO.File.Exists(this.DbFile))
            {
                var data = new List<byte>();
                using (System.IO.FileStream image = System.IO.File.OpenRead(this.DbFile))
                {
                    byte bf = 0;
                    var IsFrist = true;
                    var buffer = new byte[1];
                    var contBuffer = new byte[4];
                    image.Read(contBuffer, 0, 4);
                    var lenImage = BitConverter.ToInt32(contBuffer, 0);
                    dataCache.Raise(lenImage);
                    while (image.Read(buffer, 0, 1) == 1)
                    {
                        data.Add(buffer[0]);

                        if (buffer[0] == 10 && bf == 13)
                        {
                            if (IsFrist)
                            {
                                IsFrist = false;
                                data.Clear();
                            }
                            else if (data.Count == 2)
                            {
                                break;
                            }
                            else
                            {

                                data.Clear();
                            }
                        }
                        bf = buffer[0];
                    }

                    image.Seek(256 * 256, System.IO.SeekOrigin.Begin);
                    buffer = new byte[2];

                    var row = new List<byte[]>();
                    var lenBuffer = new byte[4];

                    while (image.Read(buffer, 0, 2) == 2)
                    {
                        byte pindex = buffer[0];
                        if (pindex >= 16)
                        {
                            if (image.Read(lenBuffer, 1, 3) == 3)
                            {
                                lenBuffer[0] = buffer[1];
                                var len = BitConverter.ToInt32(lenBuffer, 0);
                                var buffer2 = new byte[len + 1];
                                if (image.Read(buffer2, 1, len) == len)
                                {
                                    pindex -= 16;
                                    buffer2[0] = pindex;
                                    row.Add(buffer2);
                                    image.Position += 2;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                        else if (pindex == 13 && buffer[1] == 10)
                        {
                            this.Put(row.ToArray());
                            row.Clear();
                        }
                        else if (pindex == 14 && buffer[1] == 10)
                        {
                            this.Delete(row.ToArray());
                            row.Clear();

                        }
                        else
                        {
                            break;

                        }
                    }
                }
                this.log = System.IO.File.Open(this.DbFile, System.IO.FileMode.Append);
                if (this.log.Position < 256 * 256)
                {
                    this.log.Seek(256 * 256, System.IO.SeekOrigin.Begin);
                }
            }
            else
            {
                sacheSet.Save();
            }


        }
        void ICacheSet.Flush()
        {
            _SaveLog();
        }
        void ICacheSet.Save()
        {
            lock (_lock)
            {
                try
                {
                    _Save();
                }
                catch (Exception ex)
                {
                    UMC.Data.Utility.Error("Cache", DateTime.Now, ex.ToString());
                }
            }
        }
        void ICacheSet.Export(Action<byte[][]> export)
        {
            dataCache.Search(0, false, r =>
            {
                export(r);
                return false;
            });

        }
        void IDataSubscribe.Subscribe(byte[] buffer, int offset, int count)
        {
            this.Sync(buffer, offset, count);
        }

        object ICacheSet.Cache(Hashtable value)
        {

            if (value != null)
            {
                var v = Split(value);
                if (v != null && v.Length > 0)
                {
                    var val = Get(v);
                    if (val != null)
                    {
                        return Clone(val, new byte[8]);
                    }
                }
            }
            return null;

        }
    }

}
