using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace UMC.Data.Caches
{

    class Comparer : IComparer<byte[][]>
    {
        byte[] _pk;
        int _pkLength;
        public Comparer(byte[] pk, int l)
        {
            this._pk = pk;
            this._pkLength = l;
        }
        public byte[] Index => _pk;
        int pi, xi, yi, ci;

        public int Compare(byte[][] x, byte[][] y)
        {
            for (pi = 0; pi < this._pkLength; pi++)
            {
                for (xi = 0; xi < x.Length; xi++)
                {
                    if (x[xi][0] == _pk[pi])
                    {
                        break;
                    }
                }
                if (xi == x.Length)
                {
                    return 0;
                }
                for (yi = 0; yi < y.Length; yi++)
                {
                    if (y[yi][0] == _pk[pi])
                    {
                        break;
                    }
                }

                if (yi == y.Length)
                {
                    return 0;
                }
                if (x[xi].Length > y[yi].Length)
                {

                    for (ci = 1; ci < y[yi].Length; ci++)
                    {
                        var v = x[xi][ci].CompareTo(y[yi][ci]);
                        if (v != 0)
                        {
                            return v;
                        }
                    }
                    return 1;
                }
                else
                {
                    for (ci = 1; ci < x[xi].Length; ci++)
                    {
                        var v = x[xi][ci].CompareTo(y[yi][ci]);
                        if (v != 0)
                        {
                            return v;
                        }
                    }
                    if (x[xi].Length < y[yi].Length)
                    {
                        return -1;
                    }
                }

            }
            return 0;
        }
    }
    class CacheSet
    {
        byte[][][] _values;
        object _lock = new object();
        byte[] _pk;
        public CacheSet(params byte[] indexs)
        {
            this._pk = indexs;
            _values = new byte[1000][][];
        }
        public void Clear()
        {
            Array.Clear(this._values, 0, this._size);
            this._size = 0;
        }
        public IComparer<byte[][]> Comparer()
        {
            return new Comparer(this._pk, this._pk.Length);
        }
        public void Raise(int len)
        {
            if (len > this._values.Length)
            {
                var localArray2 = new byte[len][][];
                if (this._size > 0)
                {
                    Array.Copy(this._values, 0, localArray2, 0, this._size);
                }
                this._values = localArray2;
            }
        }

        public int Count => _size;

        public bool TryGetValue(byte[][] key, out byte[][] value)
        {

            var index = Search(0, _size, key, new Comparer(this._pk, this._pk.Length));
            if (index > -1)
            {
                var v = _values[index];

                if (v[v.Length - 1].Length > 0)
                {
                    value = v;
                    return true;
                }
            }
            value = null;
            return false;
        }
        public byte[][] Key(int index)
        {
            if (index > -1 && index < _size)
            {
                return _values[index];
            }
            return null;
        }

        public bool TryGetValue(int start, byte[][] key, out byte[][] value, out int index)
        {

            index = Search(start, _size - start, key, new Comparer(_pk, _pk.Length));
            if (index > -1)
            {
                var v = _values[index];

                if (v[v.Length - 1].Length > 0)
                {
                    value = v;
                    return true;
                }
            }
            else
            {
                index = ~index;
            }
            value = null;
            return false;
        }
        int Desc(int index, byte[][] min, byte[][] max, Func<byte[][], bool> action)
        {
            var pk = System.Buffers.ArrayPool<byte>.Shared.Rent(min.Length);
            for (var i = 0; i < min.Length; i++)
            {
                pk[i] = min[i][0];
            }
            var comparer = new Comparer(pk, min.Length);
            try
            {
                var end = index;
                if (index > 0)
                {
                    if (comparer.Compare(_values[index], max) > 0)
                    {
                        end = Search(0, index, max, comparer);

                        if (end < 0)
                        {
                            end = ~end;
                        }
                    }
                }
                else
                {
                    end = Search(0, _size, max, comparer);

                    if (end < 0)
                    {
                        end = (~end) - 1;
                    }
                }
                var start = Search(0, _size - index, min, comparer);


                if (start < 0)
                {
                    start = ~start;
                }

                for (; start <= end; end--)
                {
                    var v = _values[end];
                    if (v[v.Length - 1].Length > 0)
                    {
                        if (action(v))
                        {
                            return end - 1;
                        }
                    }
                }
                return ~start;
            }
            finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(pk);
            }
        }
        int Asc(int index, byte[][] min, byte[][] max, Func<byte[][], bool> action)
        {
            var pk = System.Buffers.ArrayPool<byte>.Shared.Rent(min.Length);
            for (var i = 0; i < min.Length; i++)
            {
                pk[i] = min[i][0];
            }
            var comparer = new Comparer(pk, min.Length);
            try
            {


                var start = index;
                if (index > 0)
                {
                    if (comparer.Compare(_values[index], min) < 0)
                    {
                        start = Search(index, _size - index, min, comparer);

                        if (start < 0)
                        {
                            start = ~start;
                        }
                    }
                }
                else
                {
                    start = Search(index, _size - index, min, comparer);

                    if (start < 0)
                    {
                        start = ~start;
                    }
                }
                var end = Search(index, _size - index, max, comparer);


                if (end < 0)
                {
                    end = ~end;
                }
                for (; start < end; start++)
                {
                    var v = _values[start];
                    if (v[v.Length - 1].Length > 0)
                    {
                        if (action(v))
                        {
                            return start + 1;
                        }
                    }
                }
                return ~end;
            }
            finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(pk);
            }
        }
        public int Search(int index, bool isDesc, byte[][] min, byte[][] max, Func<byte[][], bool> action)
        {

            return isDesc ? Desc(index, min, max, action) : Asc(index, min, max, action);
        }
        public void Search(byte[][] min, byte[][] max, Action<byte[][]> action)
        {

            this.Search(0, false, min, max, row =>
            {
                action(row);
                return false;
            });
        }

        public byte[][] Put(byte[][] value, out byte[][] oldValue)
        {
            oldValue = null;
            lock (_lock)
            {
                var index = Search(0, _size, value, new Comparer(_pk, _pk.Length));
                if (index > -1)
                {

                    var row = _values[index];
                    if (row[row.Length - 1].Length == 0)
                    {
                        _values[index] = value;
                    }
                    else
                    {
                        oldValue = row;
                        var vs = new List<byte[]>();
                        vs.AddRange(value);
                        for (var i = 0; i < row.Length; i++)
                        {
                            if (vs.Exists(v => v[0] == row[i][0]) == false)
                            {
                                vs.Add(row[i]);
                            }
                        }
                        row = vs.OrderBy(r => r[0]).ToArray();
                        _values[index] = row;
                        return row;
                    }
                }
                else
                {
                    Insert(~index, value);
                }
            }
            return value;
        }
        int _size = 0;
        private void Insert(int index, byte[][] value)
        {
            if (this._size == this._values.Length)
            {
                var localArray2 = new byte[this._size + 1000][][];
                if (this._size > 0)
                {
                    Array.Copy(this._values, 0, localArray2, 0, this._size);
                }
                this._values = localArray2;

            }

            if (index < this._size)
            {
                var v = _values[index];

                if (v[v.Length - 1].Length == 0)
                {
                    this._values[index] = value;
                    return;
                }
                Array.Copy(this._values, index, this._values, index + 1, this._size - index);

            }
            this._values[index] = value;
            this._size++;


        }
        int Search(int index, int length, byte[][] value, IComparer<byte[][]> comparer)
        {
            return Utility.Search(_values, index, length, value, comparer);

        }


        public byte[][] Delete(byte[][] key)
        {
            lock (_lock)
            {

                var index = Search(0, _size, key, new Comparer(this._pk, this._pk.Length));
                if (index > -1)
                {

                    var row = this._values[index];
                    if (row[row.Length - 1].Length == 0)
                    {
                        return null;
                    }
                    var ks = key.Where(r => this._pk.Contains(r[0])).ToArray();
                    var vs = new byte[ks.Length + 1][];
                    Array.Copy(ks, vs, ks.Length);
                    vs[ks.Length] = new byte[0];
                    this._values[index] = vs;
                    return row;
                }
            }
            return null;
        }
        public int Search(int index, bool isDesc, Func<byte[][], bool> action)
        {
            if (isDesc)
            {

                for (var i = index == 0 ? (this._size - 1) : index; i >= 0; i--)
                {
                    var v = _values[i];
                    if (v[v.Length - 1].Length > 0)
                    {
                        if (action(v))
                        {
                            return i + 1;
                        }
                    }
                }
            }
            else
            {
                for (var i = index; i < this._size; i++)
                {
                    var v = _values[i];
                    if (v[v.Length - 1].Length > 0)
                    {
                        if (action(v))
                        {
                            return i + 1;
                        }
                    }
                }
            }
            return -1;
        }

    }



}
