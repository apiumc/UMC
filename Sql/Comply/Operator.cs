using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;

namespace UMC.Data.Sql
{
    class Operator<T> : IOperator<T> where T : Record, new()
    {
        IWhere<T> constr;
        Operator opor;
        public Operator(Operator c, IWhere<T> cons)
        {
            this.opor = c;
            this.constr = cons;
        }
        #region IOperator<T> Members

        IWhere<T> IOperator<T>.Unequal(string field, object value)
        {
            this.opor.Unequal(field, value);
            return constr;
        }

        IWhere<T> IOperator<T>.Equal(string field, object value)
        {
            this.opor.Equal(field, value);
            return constr;
        }

        IWhere<T> IOperator<T>.Greater(string field, object value)
        {
            this.opor.Greater(field, value);
            return constr;
        }

        IWhere<T> IOperator<T>.Smaller(string field, object value)
        {
            this.opor.Smaller(field, value);
            return constr;
        }

        IWhere<T> IOperator<T>.GreaterEqual(string field, object value)
        {
            this.opor.GreaterEqual(field, value);
            return constr;
        }

        IWhere<T> IOperator<T>.SmallerEqual(string field, object value)
        {
            this.opor.SmallerEqual(field, value);
            return constr;
        }

        IWhere<T> IOperator<T>.Like(string field, string value)
        {

            this.opor.Like(field, value);
            return constr;
        }

        IWhere<T> IOperator<T>.In(string field, params object[] values)
        {
            this.opor.In(field, values);
            return constr;
        }

        IWhere<T> IOperator<T>.NotIn(string field, params object[] values)
        {
            this.opor.NotIn(field, values);
            return constr;
        }


        IWhere<T> IOperator<T>.Unequal(T value)
        {
            value.GetValues((t, v) => this.opor.Unequal(t, v));
            return constr;
        }

        IWhere<T> IOperator<T>.Equal(T value)
        {
            value.GetValues((t, v) => this.opor.Equal(t, v));
            
            return constr;
        }

        IWhere<T> IOperator<T>.Greater(T value)
        {
            value.GetValues((t, v) => this.opor.Greater(t, v));
            
            return constr;
        }

        IWhere<T> IOperator<T>.Smaller(T value)
        {
            value.GetValues((t, v) => this.opor.Smaller(t, v));
            
            return constr;
        }

        IWhere<T> IOperator<T>.GreaterEqual(T value)
        { 
            value.GetValues((t, v) => this.opor.GreaterEqual(t, v)); 
            return constr;
        }

        IWhere<T> IOperator<T>.SmallerEqual(T value)
        {
            value.GetValues((t, v) => this.opor.SmallerEqual(t, v));
            return constr;
        }



        IWhere<T> IOperator<T>.In(string field, Script script)
        {
            this.opor.In(field, script);
            return constr;
        }



        IWhere<T> IOperator<T>.In(T field, params object[] values)
        {
            field.GetValues((t, v) =>
            {
                var list = new List<object>();
                list.AddRange(values);
                list.Add(v);
                this.opor.In(t, list.ToArray());

            });
            return constr;
        }

        IWhere<T> IOperator<T>.NotIn(T field, params object[] values)
        {
            field.GetValues((t, v) =>
            {
                var list = new List<object>();
                list.AddRange(values);
                list.Add(v);
                this.opor.NotIn(t, list.ToArray());
            });
            return constr;
        }
        IWhere<T> IOperator<T>.In(T field, Script script)
        {
            field.GetValues((t, v) => opor.In(t, script));
            
            return constr;
        }

        IWhere<T> IOperator<T>.NotIn(string field, Script script)
        {
            this.opor.NotIn(field, script);
            return constr;
        }

        IWhere<T> IOperator<T>.NotIn(T field, Script script)
        {
            field.GetValues((t, v) => opor.NotIn(t, script));
            return constr;
        }

        #endregion




        #region IOperator Members




        IWhere<T> IOperator<T>.NotLike(string field, string value)
        {
            this.opor.NotLike(field, value);
            return constr;
        }



        IWhere<T> IOperator<T>.Like(T field, bool schar)
        {
            field.GetValues((t, v) =>
            {
                string from = "{0}";
                if (schar)
                {
                    from = "{0}%";
                }
                this.opor.Like(t, String.Format(from, v));
            });
            return constr;
        }
        IWhere<T> IOperator<T>.Like(T field)
        {

            field.GetValues((t, v) =>
            {
                string from = "%{0}%";
                this.opor.Like(t, String.Format(from, v));
            });
            return constr;
        }

        #endregion


        IWhere<T> IOperator<T>.Contains()
        {
            var t = (Conditions<T>)this.constr.Contains();
            var op = (Operator)this.opor;
            t.Wherer.FristJoin = op.IsOr ? JoinVocable.Or : JoinVocable.And;
            return t;
        }
    }
    class Operator
    {
        public bool IsOr;
        public Conditions condit;
        public Operator(Conditions q, bool IsOr)
        {
            this.condit = q;
            this.IsOr = IsOr;
        }
        #region IOperator Members

        public Conditions Unequal(string field, object value)
        {
            if (string.IsNullOrEmpty(field))
            {
                throw new ArgumentNullException("field");
            }
            if (IsOr)
            {
                return this.condit.Or(field, DbOperator.Unequal, value);
            }
            else
            {
                return this.condit.And(field, DbOperator.Unequal, value);
            }
        }

        public Conditions Equal(string field, object value)
        {
            if (string.IsNullOrEmpty(field))
            {
                throw new ArgumentNullException("field");
            }
            if (IsOr)
            {
                return this.condit.Or(field, DbOperator.Equal, value);
            }
            else
            {
                return this.condit.And(field, DbOperator.Equal, value);
            }
        }

        public Conditions Greater(string field, object value)
        {
            if (string.IsNullOrEmpty(field))
            {
                throw new ArgumentNullException("field");
            }
            if (IsOr)
            {
                return this.condit.Or(field, DbOperator.Greater, value);
            }
            else
            {
                return this.condit.And(field, DbOperator.Greater, value);
            }
        }

        public Conditions Smaller(string field, object value)
        {
            if (string.IsNullOrEmpty(field))
            {
                throw new ArgumentNullException("field");
            }
            if (IsOr)
            {
                return this.condit.Or(field, DbOperator.Smaller, value);
            }
            else
            {
                return this.condit.And(field, DbOperator.Smaller, value);
            }
        }

        public Conditions GreaterEqual(string field, object value)
        {
            if (string.IsNullOrEmpty(field))
            {
                throw new ArgumentNullException("field");
            }
            if (IsOr)
            {
                return this.condit.Or(field, DbOperator.GreaterEqual, value);
            }
            else
            {
                return this.condit.And(field, DbOperator.GreaterEqual, value);
            }
        }

        public Conditions SmallerEqual(string field, object value)
        {
            if (IsOr)
            {
                return this.condit.Or(field, DbOperator.SmallerEqual, value);
            }
            else
            {
                return this.condit.And(field, DbOperator.SmallerEqual, value);
            }
        }

        public Conditions Like(string field, string value)
        {
            if (string.IsNullOrEmpty(field))
            {
                throw new ArgumentNullException("field");
            }
            if (IsOr)
            {
                return this.condit.Or(field, DbOperator.Like, value);
            }
            else
            {
                return this.condit.And(field, DbOperator.Like, value);
            }
        }

        public Conditions In(string field, params object[] values)
        {
            if (string.IsNullOrEmpty(field))
            {
                throw new ArgumentNullException("field");
            }
            if (values.Length == 0)
            {
                throw new ArgumentException("values的长度不能为0");
            }

            if (IsOr)
            {
                return this.condit.Or(field, DbOperator.In, values);
            }
            else
            {
                return this.condit.And(field, DbOperator.In, values);
            }
        }


        #endregion

        #region IOperator Members


        public Conditions NotIn(string field, params object[] values)
        {
            if (string.IsNullOrEmpty(field))
            {
                throw new ArgumentNullException("field");
            }
            if (values.Length == 0)
            {
                throw new ArgumentException("values的长度不能为0");
            }

            if (IsOr)
            {
                return this.condit.Or(field, DbOperator.NotIn, values);
            }
            else
            {
                return this.condit.And(field, DbOperator.NotIn, values);
            }
        }

        #endregion
 
        #region IOperator Members


        public Conditions NotLike(string field, string value)
        {
            if (string.IsNullOrEmpty(field))
            {
                throw new ArgumentNullException("field");
            }
            if (IsOr)
            {
                return this.condit.Or(field, DbOperator.NotLike, value);
            }
            else
            {
                return this.condit.And(field, DbOperator.NotLike, value);
            }
        }

        #endregion
    }
}
