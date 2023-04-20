using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UMC.Data.Sql;

namespace UMC.Data
{
    public class RecordColumn
    {
        string _Name;
        public string Name
        {
            get
            {
                return _Name;
            }
        }
        public byte TypeCode
        {
            get
            {
                return _TypeCode;
            }

        }
        public int Size
        {
            get
            {
                return _Size;
            }

        }
        public bool Signed
        {
            get
            {
                return _Signed;
            }

        }
        byte _TypeCode;
        int _Size;
        bool _Signed;
        Type _type;

        public Type Type
        {
            get
            {
                return _type;
            }
            set
            {
                if (value.IsEnum && _TypeCode == 4)
                {
                    _type = value;
                }
            }
        }
        public RecordColumn(String name, byte typecode)
        {
            this._Name = name;
            _type = CodeToType(typecode);

            this._TypeCode = typecode;
            switch (typecode)
            {
                case 0:
                    _Size = 1;
                    _Signed = true;
                    break;
                case 1:
                    _TypeCode = 1;
                    _Size = 1;
                    break;
                case 2:
                    _TypeCode = 2;
                    _Size = 2;
                    _Signed = true;
                    break;
                case 3:
                    _TypeCode = 3;
                    _Size = 2;
                    break;
                case 4:
                    _TypeCode = 4;
                    _Size = 4;
                    _Signed = true;
                    break;
                case 5:
                    _TypeCode = 5;
                    _Size = 4;
                    break;
                case 6:
                    _TypeCode = 6;
                    _Size = 8;
                    _Signed = true;
                    break;
                case 7:
                    _TypeCode = 7;
                    _Size = 8;
                    break;
                case 8:
                    _TypeCode = 8;
                    _Size = 2;
                    break;
                case 9:
                    _TypeCode = 9;
                    _Size = 4;
                    _Signed = true;
                    break;
                case 10:
                    _TypeCode = 10;
                    _Size = 8;
                    _Signed = true;
                    break;
                case 11:
                    _TypeCode = 11;
                    _Size = 8;
                    _Signed = true;
                    break;
                case 12:
                    _TypeCode = 12;
                    _Size = 1;
                    break;
                case 13:
                    _TypeCode = 13;
                    _Size = 16;
                    break;
                case 14:
                    _TypeCode = 14;
                    _Size = 4;
                    break;
                case 15:
                    _TypeCode = 15;
                    _Size = -1;
                    break;
                case 16:
                    _TypeCode = 16;
                    _Size = -1;
                    break;
            }
        }
        static byte[] Reverse(byte[] value, bool signed)
        {
            Array.Reverse(value);
            if (signed)
            {
                value[0] ^= 128;
            }
            return value;
        }
        public static byte[] Parse(object value)
        {
            if (value != null)
            {
                var type = value.GetType();
                var f = Nullable.GetUnderlyingType(type);
                if (f != null)
                {
                    type = f;
                }
                switch (type.FullName)
                {
                    case "System.SByte":
                        return new byte[] { Convert.ToByte((SByte)value) };
                    case "System.Byte":
                        return new byte[] { (byte)value };
                    case "System.Int16":
                        return Reverse(BitConverter.GetBytes((Int16)value), true);
                    case "System.UInt16":
                        return Reverse(BitConverter.GetBytes((UInt16)value), false);
                    case "System.Int32":
                        return Reverse(BitConverter.GetBytes((Int32)value), true);
                    case "System.UInt32":
                        return Reverse(BitConverter.GetBytes((UInt32)value), false);
                    case "System.Int64":
                        return Reverse(BitConverter.GetBytes((Int64)value), true);
                    case "System.UInt64":
                        return Reverse(BitConverter.GetBytes((UInt64)value), false);
                    case "System.Char":
                        return BitConverter.GetBytes((Char)value);
                    case "System.Single":
                        return Reverse(BitConverter.GetBytes((Single)value), true);
                    case "System.Double":
                        return Reverse(BitConverter.GetBytes((Double)value), true);
                    case "System.Decimal":
                        return Reverse(BitConverter.GetBytes(Decimal.ToDouble((Decimal)value)), true);
                    case "System.Boolean":
                        return BitConverter.GetBytes((Boolean)value);
                    case "System.Guid":
                        return ((Guid)value).ToByteArray();
                    case "System.DateTime":
                        return Reverse(BitConverter.GetBytes(Reflection.TimeSpan((DateTime)value)), true);
                    case "System.String":
                        return System.Text.Encoding.UTF8.GetBytes(value as string);
                }
                if (type.IsEnum)
                {
                    return Reverse(BitConverter.GetBytes(Convert.ToInt32(value)), true);
                }
                else if (type == typeof(byte[]))
                {
                    return (byte[])value;
                }

            }
            return null;
        }

        byte[] SignedReverse(byte[] data, byte[] reverse)
        {
            Array.Copy(data, 1, reverse, 0, data.Length - 1);
            reverse[0] ^= 128;
            Array.Reverse(reverse, 0, data.Length - 1);
            return reverse;
        }
        byte[] Reverse(byte[] data, byte[] reverse)
        {
            Array.Copy(data, 1, reverse, 0, data.Length - 1);

            Array.Reverse(reverse, 0, data.Length - 1);
            return reverse;
        }
        static Type CodeToType(int code)
        {
            switch (code)
            {
                case 0:
                    return typeof(sbyte);
                case 1:
                    return typeof(byte);
                case 2:
                    return typeof(Int16);
                case 3:
                    return typeof(UInt16);
                case 4:
                    return typeof(Int32);
                case 5:
                    return typeof(UInt32);
                case 6:
                    return typeof(Int64);
                case 7:
                    return typeof(UInt64);
                case 8:
                    return typeof(Char);
                case 9:
                    return typeof(float);
                case 10:
                    return typeof(double);
                case 11:
                    return typeof(decimal);
                case 12:
                    return typeof(bool);
                case 13:
                    return typeof(Guid);
                case 14:
                    return typeof(DateTime);
                case 15:
                    return typeof(String);
                case 16:
                    return typeof(byte[]);
            }
            return null;

        }
        public object Parse(byte[] data, byte[] reverse)
        {
            switch (this._TypeCode)
            {
                case 0:
                    return Convert.ToSByte(data[1]);
                case 1:
                    return data[1];
                case 2:

                    return BitConverter.ToInt16(SignedReverse(data, reverse), 0);
                case 3:
                    return BitConverter.ToUInt16(Reverse(data, reverse), 0);
                case 4:
                    if (_type.IsEnum)
                    {
                        return Enum.ToObject(_type, BitConverter.ToInt32(SignedReverse(data, reverse), 0));
                    }
                    else
                    {
                        return BitConverter.ToInt32(SignedReverse(data, reverse), 0);
                    }
                case 5:
                    return BitConverter.ToUInt32(Reverse(data, reverse), 0);
                case 6:
                    return BitConverter.ToInt64(SignedReverse(data, reverse), 0);
                case 7:
                    return BitConverter.ToUInt64(Reverse(data, reverse), 0);
                case 8:
                    return BitConverter.ToChar(data, 1);
                case 9:
                    return BitConverter.ToSingle(SignedReverse(data, reverse), 0);
                case 10:
                    return BitConverter.ToDouble(SignedReverse(data, reverse), 0);
                case 11:
                    return new decimal(BitConverter.ToDouble(SignedReverse(data, reverse), 0));
                case 12:
                    return BitConverter.ToBoolean(data, 1);
                case 13:
                    var g = new byte[16];
                    Array.Copy(data, 1, g, 0, 16);
                    return new Guid(g);
                case 14:
                    return Reflection.TimeSpan(BitConverter.ToInt32(SignedReverse(data, reverse), 0));
                case 15:
                    return Encoding.UTF8.GetString(data, 1, data.Length - 1);
                case 16:
                    var dg = new byte[data.Length - 1];
                    Array.Copy(data, 1, dg, 0, dg.Length);
                    return dg;
            }
            return null;

        }

        public static RecordColumn Column<T>(String name, T v)
        {
            return new RecordColumn(name, typeof(T));

        }
        public RecordColumn(String name, Type type)
        {
            this._Name = name;
            var f = Nullable.GetUnderlyingType(type);
            if (f != null)
            {
                type = f;
            }
            this._type = type;
            if (type.Equals(typeof(System.SByte)))
            {
                _TypeCode = 0;
                _Size = 1;
                _Signed = true;
            }
            else if (type.Equals(typeof(System.Byte)))
            {
                _TypeCode = 1;
                _Size = 1;
            }
            else if (type.Equals(typeof(System.Int16)))
            {
                _TypeCode = 2;
                _Size = 2;
                _Signed = true;
            }
            else if (type.Equals(typeof(System.UInt16)))
            {
                _TypeCode = 3;
                _Size = 2;
            }
            else if (type.Equals(typeof(System.Int32)))
            {
                _TypeCode = 4;
                _Size = 4;
                _Signed = true;
            }
            else if (type.Equals(typeof(System.UInt32)))
            {
                _TypeCode = 5;
                _Size = 4;
            }
            else if (type.Equals(typeof(System.Int64)))
            {
                _TypeCode = 6;
                _Size = 8;
                _Signed = true;
            }
            else if (type.Equals(typeof(System.UInt64)))
            {
                _TypeCode = 7;
                _Size = 8;
            }
            else if (type.Equals(typeof(System.Char)))
            {
                _TypeCode = 8;
                _Size = 2;
            }
            else if (type.Equals(typeof(System.Single)))
            {
                _TypeCode = 9;
                _Size = 4;
                _Signed = true;
            }
            else if (type.Equals(typeof(System.Double)))
            {
                _TypeCode = 10;
                _Size = 8;
                _Signed = true;
            }
            else if (type.Equals(typeof(System.Decimal)))
            {
                _TypeCode = 11;
                _Size = 8;
                _Signed = true;
            }
            else if (type.Equals(typeof(System.Boolean)))
            {
                _TypeCode = 12;
                _Size = 1;
            }
            else if (type.Equals(typeof(System.Guid)))
            {
                _TypeCode = 13;
                _Size = 16;
            }
            else if (type.Equals(typeof(System.DateTime)))
            {
                _TypeCode = 14;
                _Size = 8;
            }
            else if (type.Equals(typeof(System.String)))
            {
                _TypeCode = 15;
                _Size = -1;
            }
            else if (type.IsEnum)
            {
                _type = type;
                _TypeCode = 4;
                _Size = 4;
                _Signed = true;
            }
            else if (type == typeof(byte[]))
            {
                _TypeCode = 16;
                _Size = -1;
            }
        }

    }



}

