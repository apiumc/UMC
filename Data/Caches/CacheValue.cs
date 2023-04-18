using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UMC.Data.Sql;

namespace UMC.Data.Caches
{

    class SearchValue
    {
        
        bool Like(byte[] array, byte[] candidate)
        {
            if (candidate.Length > array.Length)
                return false;
            _len = candidate.Length - 1;
            _size = array.Length;
            for (int _index1 = 1; _size - _index1 >= _len; _index1++)
            {
                if (array[_index1].Equals(candidate[1]))
                {
                    for (_index2 = 1; _index2 < _len; _index2++)
                    {
                        if (array[_index1 + _index2].Equals(candidate[_index2 + 1]) == false)
                        {
                            break;
                        }
                    }
                    if (_index2 == _len)
                        return true;

                }
            }
            return false;
        }

        int _index1, _index2, _size, _len, _index;
        bool InString(byte[] array, byte[] candidate)
        {
            if (candidate.Length > array.Length)
                return false;
            _len = candidate.Length - 1;
            _size = array.Length;
            _index1 = 1;
            while (_index1 < _size)
            {
                if (array[_index1].Equals(candidate[1]))
                {
                    for (_index2 = 1; _index2 < _len; _index2++)
                    {
                        if (array[_index1 + _index2].Equals(candidate[_index2 + 1]) == false)
                        {
                            break;
                        }
                    }
                    if (_index2 == _len)
                        return true;

                }

                while (_index1 < _size)
                {
                    if (array[_index1].Equals(13))
                    {
                        break;
                    }
                    _index1++;
                }

                _index1++;
            }
            return false;
        }
        bool InLen(byte[] array, byte[] candidate)
        {
            if (candidate.Length > array.Length)
                return false;
            _len = candidate.Length - 1;

            _index1 = 1;
            while (_index1 < array.Length)
            {
                if (array[_index1].Equals(candidate[1]))
                {
                    for (_index2 = 1; _index2 < _len; _index2++)
                    {
                        if (array[_index1 + _index2].Equals(candidate[_index2 + 1]) == false)
                        {
                            break;
                        }
                    }
                    if (_index2 == _len)
                        return true;

                }

                _index1 += _len;

            }
            return false;
        }
        public byte[] Value;
        public DbOperator Operat;



        public bool Check(byte[][] row)
        {
            for (_index = 0; _index < row.Length; _index++)
            {
                if (row[_index][0] == Value[0])
                {
                    break;
                }
            }
            if (_index == row.Length)
            {
                return false;
            }
            switch (Operat)
            {
                case DbOperator.Equal:
                    if (row[_index].Length == Value.Length)
                    {
                        for (_index1 = 1; _index1 < row[_index].Length; _index1++)
                        {
                            if (row[_index][_index1] != Value[_index1])
                            {
                                return false;
                            }
                        }
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                case DbOperator.Greater:
                    if (row[_index].Length == Value.Length)
                    {

                        for (_index1 = 1; _index1 < row[_index].Length; _index1++)
                        {
                            if (row[_index][_index1] > Value[_index1])
                            {
                                return true;
                            }
                            else if (row[_index][_index1] < Value[_index1])
                            {
                                return false;
                            }
                        }
                        return false;

                    }
                    else
                    {
                        return false;
                    }
                case DbOperator.GreaterEqual:
                    if (row[_index].Length == Value.Length)
                    {

                        for (_index1 = 1; _index1 < row[_index].Length; _index1++)
                        {
                            if (row[_index][_index1] > Value[_index1])
                            {
                                return true;
                            }
                            else if (row[_index][_index1] < Value[_index1])
                            {
                                return false;
                            }
                        }
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                case DbOperator.InStr:
                    return InString(Value, row[_index]);
                case DbOperator.In:
                    return InLen(Value, row[_index]);

                case DbOperator.NotInStr:

                    return InString(Value, row[_index]) == false;
                case DbOperator.NotIn:

                    return InLen(Value, row[_index]) == false;

                case DbOperator.Like:
                    return Like(row[_index], Value);
                case DbOperator.NotLike:
                    return Like(row[_index], Value) == false;
                case DbOperator.Smaller:
                    if (row[_index].Length == Value.Length)
                    {
                        for (_index1 = 1; _index1 < row[_index].Length; _index1++)
                        {
                            if (row[_index][_index1] < Value[_index1])
                            {
                                return true;
                            }
                            else if (row[_index][_index1] > Value[_index1])
                            {
                                return false;
                            }
                        }

                        return false;

                    }
                    else
                    {
                        return false;
                    }
                case DbOperator.SmallerEqual:
                    if (row[_index].Length == Value.Length)
                    {

                        for (_index1 = 1; _index1 < row[_index].Length; _index1++)
                        {
                            if (row[_index][_index1] < Value[_index1])
                            {
                                return true;
                            }
                            else if (row[_index][_index1] > Value[_index1])
                            {
                                return false;
                            }
                        }
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                case DbOperator.Unequal:

                    if (row[_index].Length == Value.Length)
                    {
                        for (var i = 1; i < row[_index].Length; i++)
                        {
                            if (row[_index][i] != Value[i])
                            {
                                return true;
                            }
                        }
                        return false;

                    }
                    else
                    {
                        return false;
                    }
            }
            return false;
        }
    }

    class SearchCell
    {
        public byte[][] Values;
        public int IndexKey;
        public bool IsKey;
        public byte[][] Start;
        public byte[][] End;
        public byte[] IndexFields;

        int _index, _index1, _index2;
        public void Get(byte[][] row, Action<byte[][]> action)
        {
            if (_Check(row))
            {
                action(row);
            }
        }
        bool _Check(byte[][] row)
        {
            if (Values.Length > 0)
            {
                _index = 0;

                for (_index1 = 0; _index1 < row.Length; _index1++)
                {
                    if (Values[_index][0] == row[_index1][0])
                    {
                        if (Values[_index].Length == row[_index1].Length)
                        {
                            for (_index2 = 1; _index2 < Values[_index].Length; _index2++)
                            {
                                if (Values[_index][_index2] != row[_index1][_index2])
                                {
                                    return false;
                                }
                            }
                            if (_index == Values.Length - 1)
                            {
                                return true;
                            }
                            _index++;
                        }
                        else
                        {
                            return false;
                        }
                    }

                }
                return false;
            }
            else
            {
                return true;
            }
        }

    }
    class SearchQuery
    {
        public SearchGroup[] Group;

        int _index, _index1, _index2;
        bool _Check(byte[][] row, byte[][] Indexs)
        {
            if (Indexs.Length > 0)
            {
                _index = 0;

                for (_index1 = 0; _index1 < row.Length; _index1++)
                {
                    if (Indexs[_index][0] == row[_index1][0])
                    {
                        if (Indexs[_index].Length == row[_index1].Length)
                        {
                            for (_index2 = 1; _index2 < Indexs[_index].Length; _index2++)
                            {
                                if (Indexs[_index][_index2] != row[_index1][_index2])
                                {
                                    return false;
                                }
                            }
                            if (_index == Indexs.Length - 1)
                            {
                                return true;
                            }
                            _index++;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
                return false;
            }
            else
            {
                return true;
            }
        }

        public void Get(byte[][] row, byte[][] index, Action<byte[][]> action)
        {
            if (_Check(row, index))
            {
                for (_index = 0; _index < Group.Length; _index++)
                {
                    if (Group[_index].Check(row) == false)
                    {
                        return;
                    }
                }
                action(row);

            }

        }
        public void Get(byte[][] row, Action<byte[][]> action)
        {
            for (_index = 0; _index < Group.Length; _index++)
            {
                if (Group[_index].Check(row) == false)
                {
                    return;
                }
            }

            action(row);

        }
    }
    class SearchGroup
    {
        public SearchGroup(JoinVocable joinVocable)
        {
            Vocable = joinVocable;
        }
        List<SearchValue> _searchValues = new List<SearchValue>();
        public List<SearchValue> Values
        {
            get
            {
                return _searchValues;
            }

        }
        public JoinVocable Vocable;
        int iSearch;
        bool And(byte[][] row)
        {
            for (iSearch = 0; iSearch < _searchValues.Count; iSearch++)
            {
                if (_searchValues[iSearch].Check(row) == false)
                {
                    return false;
                }
            }
            return true;


        }

        public bool Check(byte[][] row)
        {
            if (Vocable == JoinVocable.And)
            {
                return And(row);
            }
            else
            {
                return Or(row);
            }
        }
        bool Or(byte[][] row)
        {
            for (iSearch = 0; iSearch < _searchValues.Count; iSearch++)
            {
                if (_searchValues[iSearch].Check(row))
                {
                    return true;
                }
            }
            return false;

        }
    }


    public class Searcher<T> where T : Record, new()
    {
        //int Group;
        SearchGroup searchGroup;
        Cache _cache;
        //PropertyInfo[] properties;
        Searcher(JoinVocable vocable, Cache cache, List<SearchGroup> _searchValues)
        {
            this._searchers = _searchValues;
            this.searchGroup = new SearchGroup(vocable);
            _searchValues.Add(this.searchGroup);
            this._cache = cache;
            
        }
        List<SearchGroup> _searchers;

        internal Searcher(Cache cache)
        {
            this.searchGroup = new SearchGroup(JoinVocable.And);

            _cache = cache;
            _searchers = new List<SearchGroup>();
            _searchers.Add(this.searchGroup);
            //properties = SearchValue.GetProperties(typeof(T));
        }
        internal SearchGroup[] Searches
        {
            get
            {
                return _searchers.Where(r => r.Values.Count > 0).ToArray();
            }
        }
        /// <summary>
        /// 实体不等于 &gt;&gt;
        /// </summary>
        /// <param name="field">非空属性实体</param>
        /// <returns></returns>
        public Searcher<T> Unequal(T field)
        {
            var row = _cache.Split(field);
            foreach (var cell in row)
            {
                searchGroup.Values.Add(new SearchValue { Operat = DbOperator.Unequal, Value = cell });
            }
            return this;
        }
        public Searcher<T> And()
        {
            return new Searcher<T>(JoinVocable.And, _cache, _searchers);
        }
        public Searcher<T> Or()
        {
            return new Searcher<T>(JoinVocable.Or, _cache, _searchers);

        }
        public T[] Query(T value, bool isDesc, int index, int limit, out int nextIndex) 
        {
            return _cache.Search(value, isDesc, index, limit, out nextIndex, this);
        }
        public T[] Query(T value, int index, int limit, out int nextIndex) 
        {
            return _cache.Search(value, false, index, limit, out nextIndex, this);
        }
        public T[] Search(T indexKey, bool isDesc, int index, int limit, out int nextIndex)
        {
            return _cache.Search(isDesc, index, limit, out nextIndex, this, indexKey);
        }

        /// <summary>
        /// 实体等于 =
        /// </summary>
        /// <param name="field">非空属性实体</param>
        public Searcher<T> Equal(T field)
        {
            var row = _cache.Split(field);
            foreach (var cell in row)
            {
                searchGroup.Values.Add(new SearchValue { Operat = DbOperator.Equal, Value = cell });
            }

            return this;
        }
        /// <summary>
        /// 实体大于 &gt;
        /// </summary>
        /// <param name="field">非空属性实体</param>
        public Searcher<T> Greater(T field)
        {
            var row = _cache.Split(field);
            foreach (var cell in row)
            {
                searchGroup.Values.Add(new SearchValue { Operat = DbOperator.Greater, Value = cell });
            }

            return this;
        }
        /// <summary>
        /// 实体小于&lt;
        /// </summary>
        /// <param name="field">非空属性实体</param>
        public Searcher<T> Smaller(T field)
        {
            var row = _cache.Split(field);
            foreach (var cell in row)
            {
                searchGroup.Values.Add(new SearchValue { Operat = DbOperator.Smaller, Value = cell });
            }
            return this;
        }
        /// <summary>
        /// 实体大于等于 &gt;=
        /// </summary>
        /// <param name="field">非空属性实体</param>
        public Searcher<T> GreaterEqual(T field)
        {

            var row = _cache.Split(field);
            foreach (var cell in row)
            {
                searchGroup.Values.Add(new SearchValue { Operat = DbOperator.GreaterEqual, Value = cell });
            }
            return this;
        }
        /// <summary>
        /// 实体小于等于 &lt;=
        /// </summary>
        /// <param name="field">非空属性实体</param>
        public Searcher<T> SmallerEqual(T field)
        {

            var row = _cache.Split(field);
            foreach (var cell in row)
            {
                searchGroup.Values.Add(new SearchValue { Operat = DbOperator.SmallerEqual, Value = cell });
            }
            return this;
        }
        /// <summary>
        /// like
        /// </summary>
        public Searcher<T> Like(T field)
        {

            var row = _cache.Split(field);
            foreach (var cell in row)
            {
                searchGroup.Values.Add(new SearchValue { Operat = DbOperator.Like, Value = cell });
            }
            return this;
        }
        /// <summary>
        /// like
        /// </summary>
        public Searcher<T> NotLike(T field)
        {

            var row = _cache.Split(field);
            foreach (var cell in row)
            {
                searchGroup.Values.Add(new SearchValue { Operat = DbOperator.NotLike, Value = cell });
            }
            return this;
        }
        /// <summary>
        /// In
        /// </summary>
        /// <param name="field">只能一个非空字段的值</param>
        public Searcher<T> In(T field, params object[] values)
        {
            DbOperator dbOperator;
            var cell = _cache.InValue(field, out dbOperator, values);
            if (cell.Length > 0)
            {
                searchGroup.Values.Add(new SearchValue { Operat = dbOperator, Value = cell });
            }
            return this;
        }
        /// <summary>
        /// Not In 
        /// </summary>
        /// <param name="field">只能一个非空字段的值</param>
        public Searcher<T> NotIn(T field, params object[] values)
        {
            DbOperator dbOperator;

            var cell = _cache.InValue(field,  out dbOperator, values);
            if (cell.Length > 0)
            {
                searchGroup.Values.Add(new SearchValue { Operat = dbOperator == DbOperator.InStr ? DbOperator.NotInStr : DbOperator.NotIn, Value = cell });
            }
            return this;
        }
    }


}

