﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace UMC.Data
{
    /// <summary>
    /// 通用的函数
    /// </summary>
    public class Utility
    {
        public static int Search<T>(T[] values, T value, IComparer<T> comparer)
        {
            return Search(values, 0, values.Length, value, comparer);
        }

        public static int Search<T>(T[] values, int index, int length, T value, IComparer<T> comparer)
        {

            int lo = index;
            int hi = index + length - 1;
            while (lo <= hi)
            {
                int i = lo + ((hi - lo) >> 1);
                int order = comparer.Compare(values[i], value);

                if (order == 0) return i;
                if (order < 0)
                {
                    lo = i + 1;
                }
                else
                {
                    hi = i - 1;
                }
            }

            return ~lo;
        }
        public static Dictionary<String, Object> AppendDictionary(UMC.Data.Record record)
        {
            var vs = new Dictionary<String, Object>();
            record.GetValues((t, v) => vs.Add(t, v));
            return vs;
        }
        public static void AppendDictionary(UMC.Data.Record record, IDictionary vs)
        {
            record.GetValues((t, v) => vs[t] = v);
        }
        static bool IgnoreCase(byte bf, byte b)
        {
            if (bf == b)
            {
                return true;
            }
            else if (bf > 96 && bf < 123)
            {
                return bf - 32 == b;
            }
            else if (b > 96 && b < 123)
            {
                return b - 32 == bf;
            }
            else
            {
                return false;
            }
        }
        static String[] units = new String[] { "B", "KB", "MB", "GB", "TB", "PB" };
        public static string GetBitSize(long c)
        {
            var b = Math.Abs(c);
            long mod = 1000;
            var lmod = 1L;
            int i = 0;
            while (b >= mod)
            {
                lmod *= mod;
                b /= mod;
                i++;
            }
            if (c < 0)
            {
                b = -b;
            }
            if (i > 0)
            {
                var v = (Math.Abs(c) % lmod) / (lmod / 10);
                return $"{b}.{v}{units[i]}";
            }
            return b + units[i];

        }
        public static int FindIndex(byte[] bf, int offset, int end, byte[] search)
        {
            if (end > bf.Length)
            {
                end = bf.Length;
            }
            for (; offset < end; offset++)
            {
                if (bf[offset] == search[0])
                {
                    if (offset + search.Length - 1 < end)
                    {
                        bool IsFind = true;
                        for (int c = 1; c < search.Length; c++)
                        {
                            if (search[c] != bf[offset + c])
                            {
                                IsFind = false;
                                break;
                            }
                        }
                        if (IsFind)
                        {
                            return offset;
                        }

                    }
                    else
                    {
                        return -1;
                    }
                }
            }
            return -1;
        }
        public static int FindIndexIgnoreCase(byte[] bf, int offset, int end, byte[] search)
        {
            if (end > bf.Length)
            {
                end = bf.Length;
            }
            for (; offset < end; offset++)
            {
                if (IgnoreCase(bf[offset], search[0]))
                {
                    if (offset + search.Length - 1 < end)
                    {
                        bool IsFind = true;
                        for (int c = 1; c < search.Length; c++)
                        {
                            if (IgnoreCase(search[c], bf[offset + c]) == false)
                            {
                                IsFind = false;
                                break;
                            }
                        }
                        if (IsFind)
                        {
                            return offset;
                        }

                    }
                    else
                    {
                        return -1;
                    }
                }
            }
            return -1;
        }
        public static void GetStartEndDate(DateTime date, string type, out DateTime start, out DateTime end)
        {
            switch (type)
            {
                default:
                case "day":
                    start = date;
                    end = date.AddDays(1);
                    break;
                case "week":
                    int Week = Convert.ToInt32(date.DayOfWeek);
                    start = date.AddDays(0 - Week);
                    end = date.AddDays(7 - Week);
                    break;
                case "month":
                    start = new DateTime(date.Year, date.Month, 1);
                    end = start.AddMonths(1);
                    break;
            }

        }
        public static Type GetType(string type)
        {
            var als = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var a in als)//mscorlib, 
            {
                var type2 = a.GetType(type);
                if (type2 != null)
                {
                    return type2;
                }
            }
            return null;
        }
        /// <summary>
        /// 某日期是本月的第几周
        /// </summary>
        /// <param name="dtSel"></param>
        /// <param name="sundayStart"></param>
        /// <returns></returns>
        public static int WeekOfMonth(DateTime dtSel, bool sundayStart)
        {
            //如果要判断的日期为1号，则肯定是第一周了 
            if (dtSel.Day == 1) return 1;
            else
            {
                //得到本月第一天 
                DateTime dtStart = new DateTime(dtSel.Year, dtSel.Month, 1);
                //得到本月第一天是周几 
                int dayofweek = (int)dtStart.DayOfWeek;
                //如果不是以周日开始，需要重新计算一下dayofweek，详细风DayOfWeek枚举的定义 
                if (!sundayStart)
                {
                    dayofweek = dayofweek - 1;
                    if (dayofweek < 0) dayofweek = 7;
                }
                //得到本月的第一周一共有几天 
                int startWeekDays = 7 - dayofweek;
                //如果要判断的日期在第一周范围内，返回1 
                if (dtSel.Day <= startWeekDays) return 1;
                else
                {
                    int aday = dtSel.Day + 7 - startWeekDays;
                    return aday / 7 + (aday % 7 > 0 ? 1 : 0);
                }
            }
        }
        public static string QRUrl(string chl)
        {
            return String.Format("https://res.apiumc.com/QR/{0}.svg?chl={1}", Parse62Encode(IntParse(Guid(chl, true).Value)), Uri.EscapeDataString(chl));
        }
        public static string QR128Url(string chl)
        {
            return String.Format("https://res.apiumc.com/QR/{0}.svg?t=128&chl={1}", Parse62Encode(IntParse(Guid(chl, true).Value)), Uri.EscapeDataString(chl));
        }


        public static int TimeSpan()
        {
            return TimeSpan(DateTime.Now);
        }
        /// <summary>
        /// 转化离1970年1月1日0：00：00的秒数
        /// </summary>
        /// <param name="time">时间</param>
        /// <returns></returns>
        public static int TimeSpan(DateTime time)
        {
            return UMC.Data.Reflection.TimeSpan(time);
        }
        /// <summary>
        /// 转化离1970年1月1日0：00：00的秒数
        /// </summary>
        /// <param name="time">时间</param>
        /// <returns></returns>
        public static DateTime TimeSpan(int time)
        {
            return UMC.Data.Reflection.TimeSpan(time);
        }
        public static string GetDate(DateTime? date)
        {
            if (date.HasValue == false)
            {
                return "";
            }
            var date1 = DateTime.Now.Date;
            var now = date1 - date.Value.Date;
            if (now.Days > 0)
            {
                switch (now.Days)
                {
                    case 1:
                        return string.Format("昨天");
                    case 2:
                        return string.Format("前天");
                    default:
                        if (now.Days > 3)
                        {
                            if (date1.Year != date.Value.Year)
                            {
                                return date.Value.ToShortDateString();
                            }
                            else
                            {
                                return string.Format("{0:MM月d日}", date);
                            }
                        }
                        else
                        {
                            return string.Format("{0}天前", now.Days);
                        }

                }
            }
            else
            {
                now = DateTime.Now - date.Value;
                if (now.Hours > 0)
                {
                    if (date1.Hour > now.Hours)
                    {
                        return string.Format("{0}点{1}分", date.Value.Hour, date.Value.Minute);
                    }
                    else
                    {
                        return string.Format("{0}小时前", now.Hours);
                    }
                }
                else if (now.Minutes > 0)
                {
                    return string.Format("{0}分钟前", now.Minutes);
                }
                else
                {
                    return string.Format("刚刚", now.Seconds);
                }
            }
        }
        public static bool IsEmail(string email)
        {
            if (String.IsNullOrEmpty(email))
            {
                return false;
            }

            if (System.Text.RegularExpressions.Regex.IsMatch(email, @"^([a-zA-Z0-9_\.\-])+\@(([a-zA-Z0-9\-])+\.)+([a-zA-Z0-9]{2,4})+$"))
            {
                return true;
            }
            return false;
        }
        public static bool IsPhone(string phone)
        {
            if (String.IsNullOrEmpty(phone))
            {
                return false;
            }

            if (System.Text.RegularExpressions.Regex.IsMatch(phone, @"^(1[3-8])\d{9}$"))
            {
                return true;
            }
            else if (System.Text.RegularExpressions.Regex.IsMatch(phone, @"^\d{7,12}$"))
            {
                switch (phone.Length)
                {
                    case 7:
                    case 8:
                        return phone.StartsWith("0") == false;
                    case 11:
                    case 12:
                        return phone.StartsWith("0");
                }
            }
            return false;
        }
        public static string GetRoot(Uri uri)
        {
            var root = uri.AbsolutePath.Substring(1);
            int i = root.IndexOf('/');
            if (i > -1)
            {
                return root.Substring(0, i);
            }
            else if (root.IndexOf('.') > 0)
            {
                return "UMC";// UMC.Security.Membership.Sharename;
            }
            return root;
        }
        /// <summary>
        /// 半角转全角
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static String SBC(String input)
        {
            // 半角转全角：
            char[] c = input.ToCharArray();
            for (int i = 0; i < c.Length; i++)
            {
                if (c[i] == 32)
                {
                    c[i] = (char)12288;
                    continue;
                }
                if (c[i] < 127)
                    c[i] = (char)(c[i] + 65248);
            }
            return new String(c);
        }

        /// <summary>
        /// 全角转半角
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static String DBC(String input)
        {
            char[] c = input.ToCharArray();
            for (int i = 0; i < c.Length; i++)
            {
                if (c[i] == 12288)
                {
                    c[i] = (char)32;
                    continue;
                }
                if (c[i] > 65280 && c[i] < 65375)
                    c[i] = (char)(c[i] - 65248);
            }
            return new String(c);
        }
        /// <summary>
        ///  获取汉字拼音首字母
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string Spell(String str)
        {
            return ChineseSpell.GetChineseSpell(str);
        }
        /// <summary>
        /// 数字码
        /// </summary>
        /// <param name="v"></param>
        /// <param name="l"></param>
        /// <returns></returns>
        public static string NumberCode(uint v, int l)
        {
            var code = v.ToString();
            var sb = new StringBuilder();
            sb.Append(code);
            while (sb.Length < l)
            {
                sb.Insert(0, "0");
            }
            //if (code.Length > l)
            //{
            //    return code.Substring(0, l);
            //}
            return sb.ToString(0, l);
        }
        /// <summary>
        /// 数字码
        /// </summary>
        /// <param name="i"></param>
        /// <param name="l"></param>
        /// <returns></returns>
        public static string NumberCode(int i, int l)
        {
            uint v = BitConverter.ToUInt32(BitConverter.GetBytes(i), 0);
            return NumberCode(v, l);
        }
        public static void Move(String sfile, string dfile)
        {
            if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(dfile)))
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dfile));
            }
            if (File.Exists(dfile))
            {
                File.Delete(dfile);
            }
            //if(String.)
            File.Move(sfile, dfile);
        }
        public static void Copy(System.IO.Stream d, string file)
        {
            if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(file)))
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(file));
            }
            if (File.Exists(file))
            {
                File.Delete(file);
            }

            using (System.IO.Stream sWriter = File.Open(file, FileMode.Create))
            {
                d.CopyTo(sWriter);
                sWriter.Flush();
                sWriter.Close();
            }
        }
        /// <summary>
        /// GUID转化为22位base64,注意：其中标准的“+”变成“.”“/”变成“_”
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static string Guid(Guid id)
        {
            var config = Convert.ToBase64String(id.ToByteArray());
            var sb = new StringBuilder();
            foreach (var v in config)
            {
                switch (v)
                {
                    case '+':
                        sb.Append('-');
                        break;
                    case '/':
                        sb.Append('_');
                        break;
                    case '=':
                        break;
                    default:
                        sb.Append(v);
                        break;
                }
            }
            return sb.ToString();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="g1"></param>
        /// <param name="g2"></param>
        /// <returns></returns>
        public static Guid Guid(Guid g1, Guid g2)
        {
            var bStr = g1.ToByteArray();
            var bKey = g2.ToByteArray();

            for (int i = 0; i < bStr.Length; i++)
            {
                for (int j = 0; j < bKey.Length; j++)
                {
                    bStr[i] = Convert.ToByte(bStr[i] ^ bKey[j]);
                }
            }
            return new Guid(bStr);
        }
        public static bool IsApp(String UserAgent)
        {

            if (String.IsNullOrEmpty(UserAgent) == false)
            {
                return UserAgent.IndexOf("UMC Client") > -1;
            }
            return false;
        }
        public static Guid? Guid(string str, bool ismd5)
        {
            if (String.IsNullOrEmpty(str))
            {
                return null;
            }
            else
            {
                var g = Guid(str);
                if (g.HasValue)
                {
                    return g;
                }
                else
                {
                    using (var md5 = new System.Security.Cryptography.MD5CryptoServiceProvider())
                    {
                        byte[] md = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(str));
                        return new Guid(md);
                    }
                }
            }
        }
        public static Guid? Guid(string str)
        {
            if (String.IsNullOrEmpty(str))
            {
                return null;
            }
            try
            {
                switch (str.Length)
                {
                    case 23:
                    case 22:
                        var sb = new StringBuilder();
                        foreach (var v in str)
                        {
                            switch (v)
                            {
                                case '-':
                                case '.':
                                    sb.Append('+');
                                    break;
                                case '_':
                                    sb.Append('/');
                                    break;
                                default:
                                    sb.Append(v);
                                    break;
                            }
                        }
                        switch (sb.Length % 3)
                        {
                            case 1:
                                sb.Append("==");
                                break;
                            case 2:
                                sb.Append('=');
                                break;
                        }
                        return new Guid(Convert.FromBase64String(sb.ToString()));
                    case 38:
                        switch (str[0])
                        {
                            case '(':
                            case '{':
                                return new Guid(str);

                        }
                        return null;
                    case 36:
                    case 32:
                        return new Guid(str);
                    default:
                        return null;
                }
            }
            catch
            {
                return null;
            }
        }
        /// <summary>
        /// 把十进制转化为36进制
        /// </summary>
        public static string Parse36Encode(int value)
        {

            return ParseEncode(value, 36);

        }
        public static string Parse36Encode(Guid value)
        {

            return ParseEncode(IntParse(value), 36);

        }



        /// <summary>
        /// 把十进制转化为62进制
        /// </summary>
        public static string Parse62Encode(int value)
        {
            return ParseEncode(value, 62);


        }
        /// <summary>
        /// 把十进制转化为2-62进制
        /// </summary>
        /// <param name="value">整形</param>
        /// <param name="p">进制</param>
        /// <returns></returns>
        public static string ParseEncode(int value, int p)
        {

            return ParseEncode(BitConverter.ToUInt32(BitConverter.GetBytes(value), 0), p);


        }

        public static string ParseEncode(ulong i, int p)
        {
            if (p > 1 && p < 63)
            {
                var sb = new StringBuilder();
                ulong j = 0, p2 = (ulong)p;

                while (i > ((ulong)(p - 1)))
                {
                    j = i % p2;
                    sb.Insert(0, STR_DE62[(int)j]);
                    i = i / p2;
                }
                sb.Insert(0, STR_DE62[(int)i]);

                return sb.ToString();
            }
            throw new ArgumentOutOfRangeException("p", "p > 1 && p < 63");
        }
        private static int _Conver(char c)
        {
            int d = 0;

            if (c >= 'a')
            {
                d = (c - 'a') + 10;
            }
            else if (c >= 'A')
            {
                d = (c - 'A') + 36;
            }
            else if (c >= '0')
            {
                d = (c - '0');
            }
            else
            {
                return -1;
            }
            return d;
        }
        const string STR_DE62 = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        /// <summary>
        /// 62进制转化十进制
        /// </summary>
        public static int Parse62Decode(string value)
        {
            return (int)ParseDecode(value, 62);
        }
        /// <summary>
        /// 把2-62之间的进制转化为十进制
        /// </summary>
        /// <param name="value"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static ulong ParseDecode(string value, int p)
        {
            uint v = 0;
            int l = value.Length, l2 = l;
            while (l > 1)
            {
                var d = _Conver(value[l2 - l]);
                if (d < 0)
                {
                    return 0;
                }
                var v2 = Math.Pow(p, l - 1);
                if (v2 > UInt32.MaxValue)
                {
                    return 0;
                }

                v += (UInt32)d * Convert.ToUInt32(v2);
                l--;
            }
            var c = _Conver(value[l2 - l]);
            if (c < 0)
            {
                return 0;
            }
            v += Convert.ToUInt32(c);
            return v;// BitConverter.ToInt32(BitConverter.GetBytes(v), 0);
        }
        public static int Parse36Decode(string value)
        {
            value = value.ToLower();
            return (int)ParseDecode(value, 36);
        }

        public static String SHA256(String s)
        {
            return Hex(SHA256Managed.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes(s)));
        }

        public static String SHA1(String s)
        {
            byte[] btInput = System.Text.Encoding.UTF8.GetBytes(s);
            var mdInst = System.Security.Cryptography.SHA1.Create();

            return Hex(mdInst.ComputeHash(btInput));

        }
        /// <summary>
        /// DES加密
        /// </summary>
        /// <param name="data">加密字符</param>
        /// <param name="sn">密钥</param>
        /// <returns></returns>
        public static byte[] DES(string data, Guid sn)
        {
            var btys = sn.ToByteArray();
            byte[] byKey = new byte[8];
            byte[] byIV = new byte[8];
            Array.Copy(btys, 0, byKey, 0, 8);
            Array.Copy(btys, 8, byIV, 0, 8);
            return DES(data, byKey, byIV);


        }
        public static byte[] AES(string data, byte[] key, byte[] iv)
        {
            var cTransform = System.Security.Cryptography.Aes.Create().CreateEncryptor(key, iv);
            var toEncryptArray = System.Text.Encoding.UTF8.GetBytes(data);
            return cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

        }
        public static byte[] AES(string data, string skey, int iterations)
        {
            var md5Byte = new List<byte>();
            md5Byte.AddRange(System.Text.Encoding.UTF8.GetBytes(skey));
            var salt = new byte[8];
            Array.Copy(System.Guid.NewGuid().ToByteArray(), 0, salt, 0, 8);
            md5Byte.AddRange(salt);
            var md5Hash = System.Security.Cryptography.MD5.Create();
            var md5 = md5Hash.ComputeHash(md5Byte.ToArray());
            for (var i = 1; i < iterations; i++)
            {
                md5 = md5Hash.ComputeHash(md5);
            }
            var key = new byte[8];
            var iv = new byte[8];

            Array.Copy(md5, 0, key, 0, 8);
            Array.Copy(md5, 8, iv, 0, 8);
            var da = AES(data, key, iv);
            var bs = new byte[da.Length + 16];
            Array.Copy(System.Text.Encoding.UTF8.GetBytes("Salted__"), 0, bs, 0, 8);
            Array.Copy(salt, 0, bs, 8, 8);
            Array.Copy(da, 0, bs, 16, da.Length);
            return bs;
        }
        public static byte[] DES(string data, string skey, int iterations)
        {
            var md5Byte = new List<byte>();
            md5Byte.AddRange(System.Text.Encoding.UTF8.GetBytes(skey));
            var salt = new byte[8];
            Array.Copy(System.Guid.NewGuid().ToByteArray(), 0, salt, 0, 8);
            //var salt = Guid.
            md5Byte.AddRange(salt);
            //var md5 = System.Security.Cryptography.MD5.Create().ComputeHash(md5Byte.ToArray());

            var md5Hash = System.Security.Cryptography.MD5.Create();
            var md5 = md5Hash.ComputeHash(md5Byte.ToArray());
            for (var i = 1; i < iterations; i++)
            {
                md5 = md5Hash.ComputeHash(md5);
            }

            var key = new byte[8];
            var iv = new byte[8];

            Array.Copy(md5, 0, key, 0, 8);
            Array.Copy(md5, 8, iv, 0, 8);
            var da = DES(data, key, iv);
            var bs = new byte[da.Length + 16];
            Array.Copy(System.Text.Encoding.UTF8.GetBytes("Salted__"), 0, bs, 0, 8);
            Array.Copy(salt, 0, bs, 8, 8);
            Array.Copy(da, 0, bs, 16, da.Length);
            return bs;
        }

        public static byte[] DES(string data, string key, string iv)
        {
            return DES(data, Hex(key), Hex(iv));
        }
        public static byte[] AES(string data, string key, string iv)
        {
            return AES(data, Hex(key), Hex(iv));
        }
        public static byte[] DES(string data, byte[] key, byte[] iv)
        {
            var cTransform = System.Security.Cryptography.DES.Create().CreateEncryptor(key, iv);

            var toEncryptArray = System.Text.Encoding.UTF8.GetBytes(data);
            return cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

        }







        public static byte[] HMAC(string text, string pwd)
        {
            return new HMACSHA1(Encoding.UTF8.GetBytes(pwd)).ComputeHash(Encoding.UTF8.GetBytes(text));
        }

        public static byte[] Hex(string hexString)
        {
            hexString = hexString.Replace(" ", "");
            //    int l = hexString.Length / 2;
            if ((hexString.Length % 2) != 0)
                hexString = "0" + hexString;
            byte[] returnBytes = new byte[hexString.Length / 2];
            for (int i = 0; i < returnBytes.Length; i++)
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            return returnBytes;
        }
        public static String Sign(System.Collections.Specialized.NameValueCollection query, string appKey)
        {
            var buff = new System.Text.StringBuilder();

            var result = new List<String>(query.AllKeys);

            result.Sort();
            for (int i = 0; i < result.Count; i++)
            {
                string pair = result[i];
                buff.AppendFormat("{0}={1}&", pair, query[pair]);
            }
            buff.AppendFormat("key={0}", appKey);
            return MD5(buff.ToString());
        }
        public static byte[] RSA(String publicPem, String text)
        {

            publicPem = publicPem.Replace("-----BEGIN PUBLIC KEY-----", "").Replace("-----END PUBLIC KEY-----", "").Replace("\n", "").Replace("\r", "");
            byte[] keyData = Convert.FromBase64String(publicPem);
            if (keyData.Length < 162)
            {
                throw new ArgumentException("pem file content is incorrect.");
            }
            byte[] pemModulus = new byte[128];
            byte[] pemPublicExponent = new byte[3];
            Array.Copy(keyData, 29, pemModulus, 0, 128);
            Array.Copy(keyData, 159, pemPublicExponent, 0, 3);
            RSAParameters para = new RSAParameters();
            para.Modulus = pemModulus;
            para.Exponent = pemPublicExponent;

            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.ImportParameters(para);

            return rsa.Encrypt(System.Text.Encoding.UTF8.GetBytes(text), false);

        }
        public static byte[] RSA(String n, String e, String text)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            RSAParameters RSAKeyInfo = new RSAParameters();
            RSAKeyInfo.Modulus = Hex(n);
            RSAKeyInfo.Exponent = Hex(e);
            rsa.ImportParameters(RSAKeyInfo);

            return rsa.Encrypt(System.Text.Encoding.UTF8.GetBytes(text), false);

        }
        public static string Hex(byte[] targetData)
        {
            var byte2String = new System.Text.StringBuilder();

            for (int i = 0; i < targetData.Length; i++)
            {
                byte2String.AppendFormat("{0:x2}", targetData[i]);
            }

            return byte2String.ToString();
        }
        public static string MD5(string myString)
        {
            if (String.IsNullOrEmpty(myString) == false)
            {
                var md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                return Hex(md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(myString)));
            }
            return null;
        }
        public static byte[] MD5(params object[] objs)
        {
            using (var md5 = new System.Security.Cryptography.MD5CryptoServiceProvider())
            {
                return md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(String.Join(",", objs)));
            }
        }
        /// <summary>
        /// DES解密
        /// </summary>
        /// <param name="byEnc">已经加密数据</param>
        /// <param name="sn">密钥</param>
        public static string DES(byte[] byEnc, Guid sn)
        {
            var btys = sn.ToByteArray();
            byte[] byKey = new byte[8];
            byte[] byIV = new byte[8];
            Array.Copy(btys, 0, byKey, 0, 8);
            Array.Copy(btys, 8, byIV, 0, 8);

            return System.Text.Encoding.UTF8.GetString(System.Security.Cryptography.DES.Create().CreateDecryptor(byKey, byIV).TransformFinalBlock(byEnc, 0, byEnc.Length));
        }
        /// <summary>
        /// 获取格式化Sql脚本中的字典参数名
        /// </summary>
        /// <param name="sqlTexts"></param>
        /// <returns></returns>
        public static string[] GetFaramKeys(params string[] sqlTexts)
        {
            System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex(@"\{([\w.]+)\}");

            var fList = new List<String>();

            foreach (var sqltext in sqlTexts)
            {
                if (!String.IsNullOrEmpty(sqltext))
                {
                    var ms = reg.Matches(sqltext);
                    for (var i = 0; i < ms.Count; i++)
                    {
                        string mv = ms[i].Groups[1].Value.ToLower();
                        if (!fList.Exists(str => str.ToLower() == mv))
                        {
                            fList.Add(ms[i].Groups[1].Value);
                        }

                    }
                }
            }
            var hash = new System.Collections.Hashtable();
            UMC.Data.Utility.AppendDictionary(hash);
            var em = hash.GetEnumerator();

            while (em.MoveNext())
            {

                fList.RemoveAll(str => String.Equals(str, em.Key as string, StringComparison.CurrentCultureIgnoreCase));

            }
            return fList.ToArray();
        }
        /// <summary>
        /// 把NameValueCollection转化为Dictionary 字典
        /// </summary>
        /// <param name="diction"></param>
        /// <param name="nvs"></param>
        /// <returns></returns>
        public static int AppendDictionary(System.Collections.IDictionary diction, params System.Collections.Specialized.NameValueCollection[] nvs)
        {
            return AppendDictionary(diction, true, nvs);
        }

        /// <summary>
        /// 根据XPathNavigator文档子结点来转化为Dictionary 字典
        /// </summary>
        /// <param name="obj">对象</param>
        /// <param name="xPath">XPathNavigator</param>
        /// <returns></returns>
        public static int AppendDictionary(System.Collections.IDictionary diction, System.Xml.XPath.XPathNavigator xPath)
        {
            System.Xml.XPath.XPathNodeIterator xtertor = xPath.SelectChildren(System.Xml.XPath.XPathNodeType.Element);
            int t = 0;
            while (xtertor.MoveNext())
            {

                if (!String.IsNullOrEmpty(xtertor.Current.Value))
                {
                    diction[xtertor.Current.Name] = xtertor.Current.Value;
                    t++;
                }

            }
            return t;
        }
        /// <summary>
        /// 把NameValueCollection转化为Dictionary 字典
        /// </summary>
        /// <param name="diction"></param>
        /// <param name="strFormat">是否字符串</param>
        /// <param name="nvs"></param>
        /// <returns></returns>
        public static int AppendDictionary(System.Collections.IDictionary diction, bool strFormat, params System.Collections.Specialized.NameValueCollection[] nvs)
        {
            int count = 0;
            for (var n = 0; n < nvs.Length; n++)
            {
                var values = nvs[n];

                for (int i = 0; i < values.Count; i++)
                {
                    string key = values.GetKey(i);

                    string str = values.Get(i);
                    if (!String.IsNullOrEmpty(key))
                    {
                        count++;
                        if (strFormat)
                        {
                            diction[key] = str;
                        }
                        else
                        {
                            diction[key] = UMC.Data.Reflection.Parse(str);
                        }

                    }
                }
            }
            return count;
        }

        static string Format(string format, List<String> keys, List<object> values, string empty)
        {
            if (string.IsNullOrEmpty(format))
            { return ""; }


            int start = 0, end = 0, l = format.Length, i = 0;
            var isStart = true;
            var sb = new StringBuilder();
            while (i < l)
            {
                var k = format[i];
                switch (k)
                {
                    case '{':
                        isStart = true;
                        start = end = i;
                        break;
                    case '}':
                        if (isStart && start < end)
                        {
                            var key = format.Substring(start + 1, end - start);
                            var index = keys.FindIndex(v => String.Equals(v, key, StringComparison.CurrentCultureIgnoreCase));
                            if (index > -1)
                            {
                                sb.Remove(sb.Length - 1 - key.Length, key.Length + 1);
                                sb.Append(values[index]);
                                start = end = i;
                                i++;
                                continue;
                            }
                            else if (empty != null)
                            {
                                sb.Remove(sb.Length - 1 - key.Length, key.Length + 1);
                                sb.Append(empty);
                                start = end = i;
                                i++;
                                continue;
                            }
                        }
                        start = end = i;

                        isStart = false;
                        break;
                    case ' ':
                    case '\t':
                    case '\b':
                    case '\n':
                    case '\r':
                        isStart = false;
                        start = end = i;
                        break;
                    default:
                        end = i;
                        break;
                }
                i++;
                sb.Append(k);
            }
            return sb.ToString();

        }
        /// <summary>
        /// 用字典格式化文本
        /// </summary>
        /// <param name="format">文本</param>
        /// <param name="diction">字典</param>
        public static string Format(string format, System.Collections.IDictionary diction)
        {
            return Format(format, diction, null);
        }
        /// <returns></returns>
        public static string Format(string format, System.Collections.IDictionary diction, string empty)
        {
            if (string.IsNullOrEmpty(format))
            { return ""; }

            var keys = new List<String>();
            var values = new List<object>();
            System.Collections.IDictionaryEnumerator em = diction.GetEnumerator();
            while (em.MoveNext())
            {
                if (em.Key is String)
                {
                    keys.Add(em.Key as string);
                    values.Add(em.Value);
                }
            }
            return Format(format, keys, values, empty);


        }

        public static string Format(string format, object obj)
        {
            return Format(format, obj, null);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="format"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string Format(string format, object obj, string empty)
        {
            if (obj is System.Collections.IDictionary)
            {
                return Format(format, obj as System.Collections.IDictionary, empty);
            }
            else
            {

                var keys = new List<String>();
                var values = new List<object>();
                PropertyInfo[] propertys = obj.GetType().GetProperties();
                for (int i = 0; i < propertys.Length; i++)
                {
                    if (propertys[i].GetIndexParameters().Length == 0)
                    {
                        Type ptype = propertys[i].PropertyType;
                        if (ptype.IsValueType || ptype.IsPrimitive || ptype.Equals(typeof(System.String)))
                        {
                            keys.Add(propertys[i].Name);
                            values.Add(propertys[i].GetValue(obj, null));
                        }
                    }
                }

                return Format(format, keys, values, empty);
            }
        }


        /// <summary>
        /// 数组批量处理
        /// </summary>
        /// <typeparam name="T">数组类型</typeparam>
        /// <param name="args">数组</param>
        /// <param name="action">处理方法</param>
        /// <returns></returns>
        public static void Each<T>(IEnumerable<T> args, System.Action<T> action)
        {
            foreach (var a in args)
            {
                action(a);
            }
        }
        /// <summary>
        /// 采用追加的方式写入文件
        /// </summary>
        /// <param name="file">文件名</param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static bool Writer(string file, string context, bool append)
        {
            if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(file)))
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(file));
            }

            using (System.IO.StreamWriter sWriter = new System.IO.StreamWriter(file, append))
            {
                sWriter.WriteLine(context);
                sWriter.Close();
            }
            return true;
        }
        public static Stream Writer(string file, bool append)
        {
            if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(file)))
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(file));
            }

            return File.Open(file, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.ReadWrite);

        }
        public static bool Append(string file, string context)
        {
            return Writer(file, context, true);
        }
        public static bool Writer(string file, string context)
        {
            return Writer(file, context, false);

        }
        /// <summary>
        /// 读字符串文件并用字符串返回
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string Reader(string fileName)
        {
            if (System.IO.File.Exists(fileName))
            {
                using (System.IO.StreamReader read = new System.IO.StreamReader(fileName))
                {
                    string str = read.ReadToEnd();
                    read.Close();
                    return str;
                }
            }
            return "";
        }
        public static int IntParse(Guid value)
        {
            return IntParse(value.ToByteArray());
        }

        public static int IntParse(byte[] b)
        {
            var _a = (((b[3] << 0x18) | (b[2] << 0x10)) | (b[1] << 8)) | b[0];
            var _b = (short)((b[5] << 8) | b[4]);
            var _c = (short)((b[7] << 8) | b[6]);
            var _f = b[10];
            var _k = b[15];
            return ((_a ^ ((_b << 0x10) | ((ushort)_c))) ^ ((_f << 0x18) | _k));

        }
        /// <summary>
        /// 把字符串转换成整型，如果字符串不是Number则返回defaultValue
        /// </summary>
        /// <param name="str"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static int IntParse(string str, int defaultValue)
        {
            if (String.IsNullOrEmpty(str))
            {
                return defaultValue;
            }
            int i;
            if (!int.TryParse(str, out i))
            {
                return defaultValue;
            }
            return i;
        }
        /// <summary>
        /// 把字符串转换成货币型，如果字符串不是Number则返回defaultValue 
        /// </summary>
        /// <param name="str"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static decimal DecimalParse(string str, decimal defaultValue)
        {
            if (String.IsNullOrEmpty(str))
            {
                return defaultValue;
            }
            decimal i;
            if (!decimal.TryParse(str, out i))
            {
                return defaultValue;
            }
            return i;
        }
        public static ulong? UInt64Parse(string str)
        {
            if (String.IsNullOrEmpty(str))
            {
                return null;
            }
            ulong i;
            if (!ulong.TryParse(str, out i))
            {
                return null;
            }
            return i;
        }
        public static ulong UInt64Parse(string str, ulong defaultValue)
        {
            if (String.IsNullOrEmpty(str))
            {
                return defaultValue;
            }
            ulong i;
            if (!ulong.TryParse(str, out i))
            {
                return defaultValue;
            }
            return i;
        }

        /// <summary>
        /// 转化路径
        /// </summary>
        /// <param name="path">路径</param>
        /// <returns></returns>
        public static string MapPath(string path)
        {
            return Data.Reflection.Instance().AppPath(path);
        }
        public static void Error(String name, params object[] logs)
        {
            writeLog(name, "Error", logs);
        }
        public static void Debug(String name, params object[] logs)
        {
            writeLog(name, "Debug", logs);
        }
        static void writeLog(String name, String type, params object[] logs)
        {
            Reflection.Instance().WriteLog(name, type, logs);

        }
        public static void Log(String name, params object[] logs)
        {
            writeLog(name, "Log", logs);
        }
        /// <summary>
        /// 把字符转化为对应的枚举
        /// </summary>
        /// <param name="str">字符串</param>
        /// <param name="defaultValue">默认值，注意：必须为对应的枚举类型</param>
        /// <returns></returns>
        public static Enum EnumParse(string str, Enum defaultValue)
        {
            Type type = defaultValue.GetType();
            if (!type.IsEnum)
            {
                throw new System.ArgumentException("obj is not Enum");
            }
            else
            {
                if (String.IsNullOrEmpty(str))
                {
                    return defaultValue;
                }
                try
                {
                    return (Enum)System.Enum.Parse(type, str);
                }
                catch
                {
                    return defaultValue;
                }
            }
        }
        public static T[] Enum<T>(T values) where T : struct
        {
            List<T> les = new List<T>();
            Array es = System.Enum.GetValues(typeof(T));
            int value = Convert.ToInt32(values);
            //int enumValue = 0;
            for (int i = es.Length - 1; i > -1; i--)
            {
                var em = (int)es.GetValue(i);
                if ((value & em) == em)
                {
                    les.Add((T)es.GetValue(i));
                }
            }
            return les.ToArray();
        }
        /// <summary>
        /// 从[1]取数据
        /// </summary>
        /// <param name="uri">客户端请求信息</param>
        /// <returns></returns>
        public static string GetPrefix(Uri uri)
        {
            return GetPrefix(uri, 1);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="uri">客户端请求信息</param>
        /// <param name="index">索引,如果是小于0的，则返回全部通配符Key，否则返回指定索引的通配符Key</param>
        /// <returns></returns>
        public static string GetPrefix(Uri uri, int index)
        {

            try
            {
                string path = uri.LocalPath;
                string prefix = uri.Segments[uri.Segments.Length - 1];
                //string prefix = path.Substring(path.LastIndexOf('/') + 1);
                //*.server.aspx
                if (index < 0)
                {
                    while (index < 0)
                    {
                        index++;
                        prefix = prefix.Substring(0, prefix.LastIndexOf('.'));
                    }
                    return prefix;
                }
                else
                {
                    string[] fixs = prefix.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

                    if (fixs.Length - 2 - index > 0)
                    {
                        return fixs[fixs.Length - 3 - index];
                    }
                }
            }
            catch
            {
                return string.Empty;
            }
            return string.Empty;
        }

        /// <summary>
        /// 类型转化
        /// </summary>
        /// <typeparam name="T">基元类型</typeparam>
        /// <param name="defaultValue">默认值</param>
        /// <param name="str"></param>
        /// <returns></returns>
        public static T Parse<T>(string str, T defaultValue) where T : struct
        {
            try
            {
                return (T)(UMC.Data.Reflection.Parse(str, typeof(T)) ?? defaultValue);
            }
            catch
            {
                return defaultValue;
            };
        }

        static int _ProcessId, _NewId;
        public static Guid NewGuid()
        {

            if (_ProcessId == 0)
            {
                _ProcessId = System.Diagnostics.Process.GetCurrentProcess().Id;
            }
            _NewId = System.Threading.Interlocked.Increment(ref _NewId) % UInt16.MaxValue;

            var b = System.Guid.NewGuid().ToByteArray();
            var rs = BitConverter.GetBytes(Utility.TimeSpan());

            Array.Reverse(rs);
            Array.Copy(rs, 0, b, 0, 4);
            Array.Copy(BitConverter.GetBytes(_ProcessId), 0, b, 4, 2);
            Array.Copy(BitConverter.GetBytes(_NewId), 0, b, 6, 2);

            return new System.Guid(b);
        }
        public static ulong NewId()
        {
            if (_ProcessId == 0)
            {
                _ProcessId = System.Diagnostics.Process.GetCurrentProcess().Id;
            }
            _NewId = System.Threading.Interlocked.Increment(ref _NewId) % UInt16.MaxValue;
            var b = new byte[8];
            Array.Copy(BitConverter.GetBytes(_NewId), 0, b, 2, 2);
            Array.Copy(BitConverter.GetBytes(_ProcessId), 0, b, 0, 2);
            Array.Copy(BitConverter.GetBytes(Utility.TimeSpan()), 0, b, 4, 4);

            return BitConverter.ToUInt64(b, 0);
        }
        /// <summary>
        /// 寻找数值位移或的所有值
        /// </summary>
        /// <param name="value">值</param>
        /// <returns></returns>
        public static int[] DispParse(int value)
        {
            List<int> list = new List<int>();
            if (value >= 1)
            {
                double exp = Math.Log(value, 2);
                int max = Convert.ToInt32(Math.Ceiling(exp));
                int va = 0; //Math.Pow(2, max);
                for (int m = max; m >= 0; m--)
                {
                    va = Convert.ToInt32(Math.Pow(2, m));
                    if ((value & va) > 0)
                    {
                        list.Add(va);
                    }
                }

            }
            return list.ToArray();
        }
    }

    class ChineseSpell
    {
        ///  summary>
        /// 汉字拼音首字母列表 本列表包含了20902个汉字,用于配合 GetChineseSpell 函数使用,本表收录的字符的Unicode编码范围为19968至40869
        ///  /summary>
        private static string strChineseFirstPY =
        "YDYQSXMWZSSXJBYMGCCZQPSSQBYCDSCDQLDYLYBSSJGYZZJJFKCCLZDHWDWZJLJPFYYNWJJTMYHZWZHFLZPPQHGSCYYYNJQYXXGJ"
        + "HHSDSJNKKTMOMLCRXYPSNQSECCQZGGLLYJLMYZZSECYKYYHQWJSSGGYXYZYJWWKDJHYCHMYXJTLXJYQBYXZLDWRDJRWYSRLDZJPC"
        + "BZJJBRCFTLECZSTZFXXZHTRQHYBDLYCZSSYMMRFMYQZPWWJJYFCRWFDFZQPYDDWYXKYJAWJFFXYPSFTZYHHYZYSWCJYXSCLCXXWZ"
        + "ZXNBGNNXBXLZSZSBSGPYSYZDHMDZBQBZCWDZZYYTZHBTSYYBZGNTNXQYWQSKBPHHLXGYBFMJEBJHHGQTJCYSXSTKZHLYCKGLYSMZ"
        + "XYALMELDCCXGZYRJXSDLTYZCQKCNNJWHJTZZCQLJSTSTBNXBTYXCEQXGKWJYFLZQLYHYXSPSFXLMPBYSXXXYDJCZYLLLSJXFHJXP"
        + "JBTFFYABYXBHZZBJYZLWLCZGGBTSSMDTJZXPTHYQTGLJSCQFZKJZJQNLZWLSLHDZBWJNCJZYZSQQYCQYRZCJJWYBRTWPYFTWEXCS"
        + "KDZCTBZHYZZYYJXZCFFZZMJYXXSDZZOTTBZLQWFCKSZSXFYRLNYJMBDTHJXSQQCCSBXYYTSYFBXDZTGBCNSLCYZZPSAZYZZSCJCS"
        + "HZQYDXLBPJLLMQXTYDZXSQJTZPXLCGLQTZWJBHCTSYJSFXYEJJTLBGXSXJMYJQQPFZASYJNTYDJXKJCDJSZCBARTDCLYJQMWNQNC"
        + "LLLKBYBZZSYHQQLTWLCCXTXLLZNTYLNEWYZYXCZXXGRKRMTCNDNJTSYYSSDQDGHSDBJGHRWRQLYBGLXHLGTGXBQJDZPYJSJYJCTM"
        + "RNYMGRZJCZGJMZMGXMPRYXKJNYMSGMZJYMKMFXMLDTGFBHCJHKYLPFMDXLQJJSMTQGZSJLQDLDGJYCALCMZCSDJLLNXDJFFFFJCZ"
        + "FMZFFPFKHKGDPSXKTACJDHHZDDCRRCFQYJKQCCWJDXHWJLYLLZGCFCQDSMLZPBJJPLSBCJGGDCKKDEZSQCCKJGCGKDJTJDLZYCXK"
        + "LQSCGJCLTFPCQCZGWPJDQYZJJBYJHSJDZWGFSJGZKQCCZLLPSPKJGQJHZZLJPLGJGJJTHJJYJZCZMLZLYQBGJWMLJKXZDZNJQSYZ"
        + "MLJLLJKYWXMKJLHSKJGBMCLYYMKXJQLBMLLKMDXXKWYXYSLMLPSJQQJQXYXFJTJDXMXXLLCXQBSYJBGWYMBGGBCYXPJYGPEPFGDJ"
        + "GBHBNSQJYZJKJKHXQFGQZKFHYGKHDKLLSDJQXPQYKYBNQSXQNSZSWHBSXWHXWBZZXDMNSJBSBKBBZKLYLXGWXDRWYQZMYWSJQLCJ"
        + "XXJXKJEQXSCYETLZHLYYYSDZPAQYZCMTLSHTZCFYZYXYLJSDCJQAGYSLCQLYYYSHMRQQKLDXZSCSSSYDYCJYSFSJBFRSSZQSBXXP"
        + "XJYSDRCKGJLGDKZJZBDKTCSYQPYHSTCLDJDHMXMCGXYZHJDDTMHLTXZXYLYMOHYJCLTYFBQQXPFBDFHHTKSQHZYYWCNXXCRWHOWG"
        + "YJLEGWDQCWGFJYCSNTMYTOLBYGWQWESJPWNMLRYDZSZTXYQPZGCWXHNGPYXSHMYQJXZTDPPBFYHZHTJYFDZWKGKZBLDNTSXHQEEG"
        + "ZZYLZMMZYJZGXZXKHKSTXNXXWYLYAPSTHXDWHZYMPXAGKYDXBHNHXKDPJNMYHYLPMGOCSLNZHKXXLPZZLBMLSFBHHGYGYYGGBHSC"
        + "YAQTYWLXTZQCEZYDQDQMMHTKLLSZHLSJZWFYHQSWSCWLQAZYNYTLSXTHAZNKZZSZZLAXXZWWCTGQQTDDYZTCCHYQZFLXPSLZYGPZ"
        + "SZNGLNDQTBDLXGTCTAJDKYWNSYZLJHHZZCWNYYZYWMHYCHHYXHJKZWSXHZYXLYSKQYSPSLYZWMYPPKBYGLKZHTYXAXQSYSHXASMC"
        + "HKDSCRSWJPWXSGZJLWWSCHSJHSQNHCSEGNDAQTBAALZZMSSTDQJCJKTSCJAXPLGGXHHGXXZCXPDMMHLDGTYBYSJMXHMRCPXXJZCK"
        + "ZXSHMLQXXTTHXWZFKHCCZDYTCJYXQHLXDHYPJQXYLSYYDZOZJNYXQEZYSQYAYXWYPDGXDDXSPPYZNDLTWRHXYDXZZJHTCXMCZLHP"
        + "YYYYMHZLLHNXMYLLLMDCPPXHMXDKYCYRDLTXJCHHZZXZLCCLYLNZSHZJZZLNNRLWHYQSNJHXYNTTTKYJPYCHHYEGKCTTWLGQRLGG"
        + "TGTYGYHPYHYLQYQGCWYQKPYYYTTTTLHYHLLTYTTSPLKYZXGZWGPYDSSZZDQXSKCQNMJJZZBXYQMJRTFFBTKHZKBXLJJKDXJTLBWF"
        + "ZPPTKQTZTGPDGNTPJYFALQMKGXBDCLZFHZCLLLLADPMXDJHLCCLGYHDZFGYDDGCYYFGYDXKSSEBDHYKDKDKHNAXXYBPBYYHXZQGA"
        + "FFQYJXDMLJCSQZLLPCHBSXGJYNDYBYQSPZWJLZKSDDTACTBXZDYZYPJZQSJNKKTKNJDJGYYPGTLFYQKASDNTCYHBLWDZHBBYDWJR"
        + "YGKZYHEYYFJMSDTYFZJJHGCXPLXHLDWXXJKYTCYKSSSMTWCTTQZLPBSZDZWZXGZAGYKTYWXLHLSPBCLLOQMMZSSLCMBJCSZZKYDC"
        + "ZJGQQDSMCYTZQQLWZQZXSSFPTTFQMDDZDSHDTDWFHTDYZJYQJQKYPBDJYYXTLJHDRQXXXHAYDHRJLKLYTWHLLRLLRCXYLBWSRSZZ"
        + "SYMKZZHHKYHXKSMDSYDYCJPBZBSQLFCXXXNXKXWYWSDZYQOGGQMMYHCDZTTFJYYBGSTTTYBYKJDHKYXBELHTYPJQNFXFDYKZHQKZ"
        + "BYJTZBXHFDXKDASWTAWAJLDYJSFHBLDNNTNQJTJNCHXFJSRFWHZFMDRYJYJWZPDJKZYJYMPCYZNYNXFBYTFYFWYGDBNZZZDNYTXZ"
        + "EMMQBSQEHXFZMBMFLZZSRXYMJGSXWZJSPRYDJSJGXHJJGLJJYNZZJXHGXKYMLPYYYCXYTWQZSWHWLYRJLPXSLSXMFSWWKLCTNXNY"
        + "NPSJSZHDZEPTXMYYWXYYSYWLXJQZQXZDCLEEELMCPJPCLWBXSQHFWWTFFJTNQJHJQDXHWLBYZNFJLALKYYJLDXHHYCSTYYWNRJYX"
        + "YWTRMDRQHWQCMFJDYZMHMYYXJWMYZQZXTLMRSPWWCHAQBXYGZYPXYYRRCLMPYMGKSJSZYSRMYJSNXTPLNBAPPYPYLXYYZKYNLDZY"
        + "JZCZNNLMZHHARQMPGWQTZMXXMLLHGDZXYHXKYXYCJMFFYYHJFSBSSQLXXNDYCANNMTCJCYPRRNYTYQNYYMBMSXNDLYLYSLJRLXYS"
        + "XQMLLYZLZJJJKYZZCSFBZXXMSTBJGNXYZHLXNMCWSCYZYFZLXBRNNNYLBNRTGZQYSATSWRYHYJZMZDHZGZDWYBSSCSKXSYHYTXXG"
        + "CQGXZZSHYXJSCRHMKKBXCZJYJYMKQHZJFNBHMQHYSNJNZYBKNQMCLGQHWLZNZSWXKHLJHYYBQLBFCDSXDLDSPFZPSKJYZWZXZDDX"
        + "JSMMEGJSCSSMGCLXXKYYYLNYPWWWGYDKZJGGGZGGSYCKNJWNJPCXBJJTQTJWDSSPJXZXNZXUMELPXFSXTLLXCLJXJJLJZXCTPSWX"
        + "LYDHLYQRWHSYCSQYYBYAYWJJJQFWQCQQCJQGXALDBZZYJGKGXPLTZYFXJLTPADKYQHPMATLCPDCKBMTXYBHKLENXDLEEGQDYMSAW"
        + "HZMLJTWYGXLYQZLJEEYYBQQFFNLYXRDSCTGJGXYYNKLLYQKCCTLHJLQMKKZGCYYGLLLJDZGYDHZWXPYSJBZKDZGYZZHYWYFQYTYZ"
        + "SZYEZZLYMHJJHTSMQWYZLKYYWZCSRKQYTLTDXWCTYJKLWSQZWBDCQYNCJSRSZJLKCDCDTLZZZACQQZZDDXYPLXZBQJYLZLLLQDDZ"
        + "QJYJYJZYXNYYYNYJXKXDAZWYRDLJYYYRJLXLLDYXJCYWYWNQCCLDDNYYYNYCKCZHXXCCLGZQJGKWPPCQQJYSBZZXYJSQPXJPZBSB"
        + "DSFNSFPZXHDWZTDWPPTFLZZBZDMYYPQJRSDZSQZSQXBDGCPZSWDWCSQZGMDHZXMWWFYBPDGPHTMJTHZSMMBGZMBZJCFZWFZBBZMQ"
        + "CFMBDMCJXLGPNJBBXGYHYYJGPTZGZMQBQTCGYXJXLWZKYDPDYMGCFTPFXYZTZXDZXTGKMTYBBCLBJASKYTSSQYYMSZXFJEWLXLLS"
        + "ZBQJJJAKLYLXLYCCTSXMCWFKKKBSXLLLLJYXTYLTJYYTDPJHNHNNKBYQNFQYYZBYYESSESSGDYHFHWTCJBSDZZTFDMXHCNJZYMQW"
        + "SRYJDZJQPDQBBSTJGGFBKJBXTGQHNGWJXJGDLLTHZHHYYYYYYSXWTYYYCCBDBPYPZYCCZYJPZYWCBDLFWZCWJDXXHYHLHWZZXJTC"
        + "ZLCDPXUJCZZZLYXJJTXPHFXWPYWXZPTDZZBDZCYHJHMLXBQXSBYLRDTGJRRCTTTHYTCZWMXFYTWWZCWJWXJYWCSKYBZSCCTZQNHX"
        + "NWXXKHKFHTSWOCCJYBCMPZZYKBNNZPBZHHZDLSYDDYTYFJPXYNGFXBYQXCBHXCPSXTYZDMKYSNXSXLHKMZXLYHDHKWHXXSSKQYHH"
        + "CJYXGLHZXCSNHEKDTGZXQYPKDHEXTYKCNYMYYYPKQYYYKXZLTHJQTBYQHXBMYHSQCKWWYLLHCYYLNNEQXQWMCFBDCCMLJGGXDQKT"
        + "LXKGNQCDGZJWYJJLYHHQTTTNWCHMXCXWHWSZJYDJCCDBQCDGDNYXZTHCQRXCBHZTQCBXWGQWYYBXHMBYMYQTYEXMQKYAQYRGYZSL"
        + "FYKKQHYSSQYSHJGJCNXKZYCXSBXYXHYYLSTYCXQTHYSMGSCPMMGCCCCCMTZTASMGQZJHKLOSQYLSWTMXSYQKDZLJQQYPLSYCZTCQ"
        + "QPBBQJZCLPKHQZYYXXDTDDTSJCXFFLLCHQXMJLWCJCXTSPYCXNDTJSHJWXDQQJSKXYAMYLSJHMLALYKXCYYDMNMDQMXMCZNNCYBZ"
        + "KKYFLMCHCMLHXRCJJHSYLNMTJZGZGYWJXSRXCWJGJQHQZDQJDCJJZKJKGDZQGJJYJYLXZXXCDQHHHEYTMHLFSBDJSYYSHFYSTCZQ"
        + "LPBDRFRZTZYKYWHSZYQKWDQZRKMSYNBCRXQBJYFAZPZZEDZCJYWBCJWHYJBQSZYWRYSZPTDKZPFPBNZTKLQYHBBZPNPPTYZZYBQN"
        + "YDCPJMMCYCQMCYFZZDCMNLFPBPLNGQJTBTTNJZPZBBZNJKLJQYLNBZQHKSJZNGGQSZZKYXSHPZSNBCGZKDDZQANZHJKDRTLZLSWJ"
        + "LJZLYWTJNDJZJHXYAYNCBGTZCSSQMNJPJYTYSWXZFKWJQTKHTZPLBHSNJZSYZBWZZZZLSYLSBJHDWWQPSLMMFBJDWAQYZTCJTBNN"
        + "WZXQXCDSLQGDSDPDZHJTQQPSWLYYJZLGYXYZLCTCBJTKTYCZJTQKBSJLGMGZDMCSGPYNJZYQYYKNXRPWSZXMTNCSZZYXYBYHYZAX"
        + "YWQCJTLLCKJJTJHGDXDXYQYZZBYWDLWQCGLZGJGQRQZCZSSBCRPCSKYDZNXJSQGXSSJMYDNSTZTPBDLTKZWXQWQTZEXNQCZGWEZK"
        + "SSBYBRTSSSLCCGBPSZQSZLCCGLLLZXHZQTHCZMQGYZQZNMCOCSZJMMZSQPJYGQLJYJPPLDXRGZYXCCSXHSHGTZNLZWZKJCXTCFCJ"
        + "XLBMQBCZZWPQDNHXLJCTHYZLGYLNLSZZPCXDSCQQHJQKSXZPBAJYEMSMJTZDXLCJYRYYNWJBNGZZTMJXLTBSLYRZPYLSSCNXPHLL"
        + "HYLLQQZQLXYMRSYCXZLMMCZLTZSDWTJJLLNZGGQXPFSKYGYGHBFZPDKMWGHCXMSGDXJMCJZDYCABXJDLNBCDQYGSKYDQTXDJJYXM"
        + "SZQAZDZFSLQXYJSJZYLBTXXWXQQZBJZUFBBLYLWDSLJHXJYZJWTDJCZFQZQZZDZSXZZQLZCDZFJHYSPYMPQZMLPPLFFXJJNZZYLS"
        + "JEYQZFPFZKSYWJJJHRDJZZXTXXGLGHYDXCSKYSWMMZCWYBAZBJKSHFHJCXMHFQHYXXYZFTSJYZFXYXPZLCHMZMBXHZZSXYFYMNCW"
        + "DABAZLXKTCSHHXKXJJZJSTHYGXSXYYHHHJWXKZXSSBZZWHHHCWTZZZPJXSNXQQJGZYZYWLLCWXZFXXYXYHXMKYYSWSQMNLNAYCYS"
        + "PMJKHWCQHYLAJJMZXHMMCNZHBHXCLXTJPLTXYJHDYYLTTXFSZHYXXSJBJYAYRSMXYPLCKDUYHLXRLNLLSTYZYYQYGYHHSCCSMZCT"
        + "ZQXKYQFPYYRPFFLKQUNTSZLLZMWWTCQQYZWTLLMLMPWMBZSSTZRBPDDTLQJJBXZCSRZQQYGWCSXFWZLXCCRSZDZMCYGGDZQSGTJS"
        + "WLJMYMMZYHFBJDGYXCCPSHXNZCSBSJYJGJMPPWAFFYFNXHYZXZYLREMZGZCYZSSZDLLJCSQFNXZKPTXZGXJJGFMYYYSNBTYLBNLH"
        + "PFZDCYFBMGQRRSSSZXYSGTZRNYDZZCDGPJAFJFZKNZBLCZSZPSGCYCJSZLMLRSZBZZLDLSLLYSXSQZQLYXZLSKKBRXBRBZCYCXZZ"
        + "ZEEYFGKLZLYYHGZSGZLFJHGTGWKRAAJYZKZQTSSHJJXDCYZUYJLZYRZDQQHGJZXSSZBYKJPBFRTJXLLFQWJHYLQTYMBLPZDXTZYG"
        + "BDHZZRBGXHWNJTJXLKSCFSMWLSDQYSJTXKZSCFWJLBXFTZLLJZLLQBLSQMQQCGCZFPBPHZCZJLPYYGGDTGWDCFCZQYYYQYSSCLXZ"
        + "SKLZZZGFFCQNWGLHQYZJJCZLQZZYJPJZZBPDCCMHJGXDQDGDLZQMFGPSYTSDYFWWDJZJYSXYYCZCYHZWPBYKXRYLYBHKJKSFXTZJ"
        + "MMCKHLLTNYYMSYXYZPYJQYCSYCWMTJJKQYRHLLQXPSGTLYYCLJSCPXJYZFNMLRGJJTYZBXYZMSJYJHHFZQMSYXRSZCWTLRTQZSST"
        + "KXGQKGSPTGCZNJSJCQCXHMXGGZTQYDJKZDLBZSXJLHYQGGGTHQSZPYHJHHGYYGKGGCWJZZYLCZLXQSFTGZSLLLMLJSKCTBLLZZSZ"
        + "MMNYTPZSXQHJCJYQXYZXZQZCPSHKZZYSXCDFGMWQRLLQXRFZTLYSTCTMJCXJJXHJNXTNRZTZFQYHQGLLGCXSZSJDJLJCYDSJTLNY"
        + "XHSZXCGJZYQPYLFHDJSBPCCZHJJJQZJQDYBSSLLCMYTTMQTBHJQNNYGKYRQYQMZGCJKPDCGMYZHQLLSLLCLMHOLZGDYYFZSLJCQZ"
        + "LYLZQJESHNYLLJXGJXLYSYYYXNBZLJSSZCQQCJYLLZLTJYLLZLLBNYLGQCHXYYXOXCXQKYJXXXYKLXSXXYQXCYKQXQCSGYXXYQXY"
        + "GYTQOHXHXPYXXXULCYEYCHZZCBWQBBWJQZSCSZSSLZYLKDESJZWMYMCYTSDSXXSCJPQQSQYLYYZYCMDJDZYWCBTJSYDJKCYDDJLB"
        + "DJJSODZYSYXQQYXDHHGQQYQHDYXWGMMMAJDYBBBPPBCMUUPLJZSMTXERXJMHQNUTPJDCBSSMSSSTKJTSSMMTRCPLZSZMLQDSDMJM"
        + "QPNQDXCFYNBFSDQXYXHYAYKQYDDLQYYYSSZBYDSLNTFQTZQPZMCHDHCZCWFDXTMYQSPHQYYXSRGJCWTJTZZQMGWJJTJHTQJBBHWZ"
        + "PXXHYQFXXQYWYYHYSCDYDHHQMNMTMWCPBSZPPZZGLMZFOLLCFWHMMSJZTTDHZZYFFYTZZGZYSKYJXQYJZQBHMBZZLYGHGFMSHPZF"
        + "ZSNCLPBQSNJXZSLXXFPMTYJYGBXLLDLXPZJYZJYHHZCYWHJYLSJEXFSZZYWXKZJLUYDTMLYMQJPWXYHXSKTQJEZRPXXZHHMHWQPW"
        + "QLYJJQJJZSZCPHJLCHHNXJLQWZJHBMZYXBDHHYPZLHLHLGFWLCHYYTLHJXCJMSCPXSTKPNHQXSRTYXXTESYJCTLSSLSTDLLLWWYH"
        + "DHRJZSFGXTSYCZYNYHTDHWJSLHTZDQDJZXXQHGYLTZPHCSQFCLNJTCLZPFSTPDYNYLGMJLLYCQHYSSHCHYLHQYQTMZYPBYWRFQYK"
        + "QSYSLZDQJMPXYYSSRHZJNYWTQDFZBWWTWWRXCWHGYHXMKMYYYQMSMZHNGCEPMLQQMTCWCTMMPXJPJJHFXYYZSXZHTYBMSTSYJTTQ"
        + "QQYYLHYNPYQZLCYZHZWSMYLKFJXLWGXYPJYTYSYXYMZCKTTWLKSMZSYLMPWLZWXWQZSSAQSYXYRHSSNTSRAPXCPWCMGDXHXZDZYF"
        + "JHGZTTSBJHGYZSZYSMYCLLLXBTYXHBBZJKSSDMALXHYCFYGMQYPJYCQXJLLLJGSLZGQLYCJCCZOTYXMTMTTLLWTGPXYMZMKLPSZZ"
        + "ZXHKQYSXCTYJZYHXSHYXZKXLZWPSQPYHJWPJPWXQQYLXSDHMRSLZZYZWTTCYXYSZZSHBSCCSTPLWSSCJCHNLCGCHSSPHYLHFHHXJ"
        + "SXYLLNYLSZDHZXYLSXLWZYKCLDYAXZCMDDYSPJTQJZLNWQPSSSWCTSTSZLBLNXSMNYYMJQBQHRZWTYYDCHQLXKPZWBGQYBKFCMZW"
        + "PZLLYYLSZYDWHXPSBCMLJBSCGBHXLQHYRLJXYSWXWXZSLDFHLSLYNJLZYFLYJYCDRJLFSYZFSLLCQYQFGJYHYXZLYLMSTDJCYHBZ"
        + "LLNWLXXYGYYHSMGDHXXHHLZZJZXCZZZCYQZFNGWPYLCPKPYYPMCLQKDGXZGGWQBDXZZKZFBXXLZXJTPJPTTBYTSZZDWSLCHZHSLT"
        + "YXHQLHYXXXYYZYSWTXZKHLXZXZPYHGCHKCFSYHUTJRLXFJXPTZTWHPLYXFCRHXSHXKYXXYHZQDXQWULHYHMJTBFLKHTXCWHJFWJC"
        + "FPQRYQXCYYYQYGRPYWSGSUNGWCHKZDXYFLXXHJJBYZWTSXXNCYJJYMSWZJQRMHXZWFQSYLZJZGBHYNSLBGTTCSYBYXXWXYHXYYXN"
        + "SQYXMQYWRGYQLXBBZLJSYLPSYTJZYHYZAWLRORJMKSCZJXXXYXCHDYXRYXXJDTSQFXLYLTSFFYXLMTYJMJUYYYXLTZCSXQZQHZXL"
        + "YYXZHDNBRXXXJCTYHLBRLMBRLLAXKYLLLJLYXXLYCRYLCJTGJCMTLZLLCYZZPZPCYAWHJJFYBDYYZSMPCKZDQYQPBPCJPDCYZMDP"
        + "BCYYDYCNNPLMTMLRMFMMGWYZBSJGYGSMZQQQZTXMKQWGXLLPJGZBQCDJJJFPKJKCXBLJMSWMDTQJXLDLPPBXCWRCQFBFQJCZAHZG"
        + "MYKPHYYHZYKNDKZMBPJYXPXYHLFPNYYGXJDBKXNXHJMZJXSTRSTLDXSKZYSYBZXJLXYSLBZYSLHXJPFXPQNBYLLJQKYGZMCYZZYM"
        + "CCSLCLHZFWFWYXZMWSXTYNXJHPYYMCYSPMHYSMYDYSHQYZCHMJJMZCAAGCFJBBHPLYZYLXXSDJGXDHKXXTXXNBHRMLYJSLTXMRHN"
        + "LXQJXYZLLYSWQGDLBJHDCGJYQYCMHWFMJYBMBYJYJWYMDPWHXQLDYGPDFXXBCGJSPCKRSSYZJMSLBZZJFLJJJLGXZGYXYXLSZQYX"
        + "BEXYXHGCXBPLDYHWETTWWCJMBTXCHXYQXLLXFLYXLLJLSSFWDPZSMYJCLMWYTCZPCHQEKCQBWLCQYDPLQPPQZQFJQDJHYMMCXTXD"
        + "RMJWRHXCJZYLQXDYYNHYYHRSLSRSYWWZJYMTLTLLGTQCJZYABTCKZCJYCCQLJZQXALMZYHYWLWDXZXQDLLQSHGPJFJLJHJABCQZD"
        + "JGTKHSSTCYJLPSWZLXZXRWGLDLZRLZXTGSLLLLZLYXXWGDZYGBDPHZPBRLWSXQBPFDWOFMWHLYPCBJCCLDMBZPBZZLCYQXLDOMZB"
        + "LZWPDWYYGDSTTHCSQSCCRSSSYSLFYBFNTYJSZDFNDPDHDZZMBBLSLCMYFFGTJJQWFTMTPJWFNLBZCMMJTGBDZLQLPYFHYYMJYLSD"
        + "CHDZJWJCCTLJCLDTLJJCPDDSQDSSZYBNDBJLGGJZXSXNLYCYBJXQYCBYLZCFZPPGKCXZDZFZTJJFJSJXZBNZYJQTTYJYHTYCZHYM"
        + "DJXTTMPXSPLZCDWSLSHXYPZGTFMLCJTYCBPMGDKWYCYZCDSZZYHFLYCTYGWHKJYYLSJCXGYWJCBLLCSNDDBTZBSCLYZCZZSSQDLL"
        + "MQYYHFSLQLLXFTYHABXGWNYWYYPLLSDLDLLBJCYXJZMLHLJDXYYQYTDLLLBUGBFDFBBQJZZMDPJHGCLGMJJPGAEHHBWCQXAXHHHZ"
        + "CHXYPHJAXHLPHJPGPZJQCQZGJJZZUZDMQYYBZZPHYHYBWHAZYJHYKFGDPFQSDLZMLJXKXGALXZDAGLMDGXMWZQYXXDXXPFDMMSSY"
        + "MPFMDMMKXKSYZYSHDZKXSYSMMZZZMSYDNZZCZXFPLSTMZDNMXCKJMZTYYMZMZZMSXHHDCZJEMXXKLJSTLWLSQLYJZLLZJSSDPPMH"
        + "NLZJCZYHMXXHGZCJMDHXTKGRMXFWMCGMWKDTKSXQMMMFZZYDKMSCLCMPCGMHSPXQPZDSSLCXKYXTWLWJYAHZJGZQMCSNXYYMMPML"
        + "KJXMHLMLQMXCTKZMJQYSZJSYSZHSYJZJCDAJZYBSDQJZGWZQQXFKDMSDJLFWEHKZQKJPEYPZYSZCDWYJFFMZZYLTTDZZEFMZLBNP"
        + "PLPLPEPSZALLTYLKCKQZKGENQLWAGYXYDPXLHSXQQWQCQXQCLHYXXMLYCCWLYMQYSKGCHLCJNSZKPYZKCQZQLJPDMDZHLASXLBYD"
        + "WQLWDNBQCRYDDZTJYBKBWSZDXDTNPJDTCTQDFXQQMGNXECLTTBKPWSLCTYQLPWYZZKLPYGZCQQPLLKCCYLPQMZCZQCLJSLQZDJXL"
        + "DDHPZQDLJJXZQDXYZQKZLJCYQDYJPPYPQYKJYRMPCBYMCXKLLZLLFQPYLLLMBSGLCYSSLRSYSQTMXYXZQZFDZUYSYZTFFMZZSMZQ"
        + "HZSSCCMLYXWTPZGXZJGZGSJSGKDDHTQGGZLLBJDZLCBCHYXYZHZFYWXYZYMSDBZZYJGTSMTFXQYXQSTDGSLNXDLRYZZLRYYLXQHT"
        + "XSRTZNGZXBNQQZFMYKMZJBZYMKBPNLYZPBLMCNQYZZZSJZHJCTZKHYZZJRDYZHNPXGLFZTLKGJTCTSSYLLGZRZBBQZZKLPKLCZYS"
        + "SUYXBJFPNJZZXCDWXZYJXZZDJJKGGRSRJKMSMZJLSJYWQSKYHQJSXPJZZZLSNSHRNYPZTWCHKLPSRZLZXYJQXQKYSJYCZTLQZYBB"
        + "YBWZPQDWWYZCYTJCJXCKCWDKKZXSGKDZXWWYYJQYYTCYTDLLXWKCZKKLCCLZCQQDZLQLCSFQCHQHSFSMQZZLNBJJZBSJHTSZDYSJ"
        + "QJPDLZCDCWJKJZZLPYCGMZWDJJBSJQZSYZYHHXJPBJYDSSXDZNCGLQMBTSFSBPDZDLZNFGFJGFSMPXJQLMBLGQCYYXBQKDJJQYRF"
        + "KZTJDHCZKLBSDZCFJTPLLJGXHYXZCSSZZXSTJYGKGCKGYOQXJPLZPBPGTGYJZGHZQZZLBJLSQFZGKQQJZGYCZBZQTLDXRJXBSXXP"
        + "ZXHYZYCLWDXJJHXMFDZPFZHQHQMQGKSLYHTYCGFRZGNQXCLPDLBZCSCZQLLJBLHBZCYPZZPPDYMZZSGYHCKCPZJGSLJLNSCDSLDL"
        + "XBMSTLDDFJMKDJDHZLZXLSZQPQPGJLLYBDSZGQLBZLSLKYYHZTTNTJYQTZZPSZQZTLLJTYYLLQLLQYZQLBDZLSLYYZYMDFSZSNHL"
        + "XZNCZQZPBWSKRFBSYZMTHBLGJPMCZZLSTLXSHTCSYZLZBLFEQHLXFLCJLYLJQCBZLZJHHSSTBRMHXZHJZCLXFNBGXGTQJCZTMSFZ"
        + "KJMSSNXLJKBHSJXNTNLZDNTLMSJXGZJYJCZXYJYJWRWWQNZTNFJSZPZSHZJFYRDJSFSZJZBJFZQZZHZLXFYSBZQLZSGYFTZDCSZX"
        + "ZJBQMSZKJRHYJZCKMJKHCHGTXKXQGLXPXFXTRTYLXJXHDTSJXHJZJXZWZLCQSBTXWXGXTXXHXFTSDKFJHZYJFJXRZSDLLLTQSQQZ"
        + "QWZXSYQTWGWBZCGZLLYZBCLMQQTZHZXZXLJFRMYZFLXYSQXXJKXRMQDZDMMYYBSQBHGZMWFWXGMXLZPYYTGZYCCDXYZXYWGSYJYZ"
        + "NBHPZJSQSYXSXRTFYZGRHZTXSZZTHCBFCLSYXZLZQMZLMPLMXZJXSFLBYZMYQHXJSXRXSQZZZSSLYFRCZJRCRXHHZXQYDYHXSJJH"
        + "ZCXZBTYNSYSXJBQLPXZQPYMLXZKYXLXCJLCYSXXZZLXDLLLJJYHZXGYJWKJRWYHCPSGNRZLFZWFZZNSXGXFLZSXZZZBFCSYJDBRJ"
        + "KRDHHGXJLJJTGXJXXSTJTJXLYXQFCSGSWMSBCTLQZZWLZZKXJMLTMJYHSDDBXGZHDLBMYJFRZFSGCLYJBPMLYSMSXLSZJQQHJZFX"
        + "GFQFQBPXZGYYQXGZTCQWYLTLGWSGWHRLFSFGZJMGMGBGTJFSYZZGZYZAFLSSPMLPFLCWBJZCLJJMZLPJJLYMQDMYYYFBGYGYZMLY"
        + "ZDXQYXRQQQHSYYYQXYLJTYXFSFSLLGNQCYHYCWFHCCCFXPYLYPLLZYXXXXXKQHHXSHJZCFZSCZJXCPZWHHHHHAPYLQALPQAFYHXD"
        + "YLUKMZQGGGDDESRNNZLTZGCHYPPYSQJJHCLLJTOLNJPZLJLHYMHEYDYDSQYCDDHGZUNDZCLZYZLLZNTNYZGSLHSLPJJBDGWXPCDU"
        + "TJCKLKCLWKLLCASSTKZZDNQNTTLYYZSSYSSZZRYLJQKCQDHHCRXRZYDGRGCWCGZQFFFPPJFZYNAKRGYWYQPQXXFKJTSZZXSWZDDF"
        + "BBXTBGTZKZNPZZPZXZPJSZBMQHKCYXYLDKLJNYPKYGHGDZJXXEAHPNZKZTZCMXCXMMJXNKSZQNMNLWBWWXJKYHCPSTMCSQTZJYXT"
        + "PCTPDTNNPGLLLZSJLSPBLPLQHDTNJNLYYRSZFFJFQWDPHZDWMRZCCLODAXNSSNYZRESTYJWJYJDBCFXNMWTTBYLWSTSZGYBLJPXG"
        + "LBOCLHPCBJLTMXZLJYLZXCLTPNCLCKXTPZJSWCYXSFYSZDKNTLBYJCYJLLSTGQCBXRYZXBXKLYLHZLQZLNZCXWJZLJZJNCJHXMNZ"
        + "ZGJZZXTZJXYCYYCXXJYYXJJXSSSJSTSSTTPPGQTCSXWZDCSYFPTFBFHFBBLZJCLZZDBXGCXLQPXKFZFLSYLTUWBMQJHSZBMDDBCY"
        + "SCCLDXYCDDQLYJJWMQLLCSGLJJSYFPYYCCYLTJANTJJPWYCMMGQYYSXDXQMZHSZXPFTWWZQSWQRFKJLZJQQYFBRXJHHFWJJZYQAZ"
        + "MYFRHCYYBYQWLPEXCCZSTYRLTTDMQLYKMBBGMYYJPRKZNPBSXYXBHYZDJDNGHPMFSGMWFZMFQMMBCMZZCJJLCNUXYQLMLRYGQZCY"
        + "XZLWJGCJCGGMCJNFYZZJHYCPRRCMTZQZXHFQGTJXCCJEAQCRJYHPLQLSZDJRBCQHQDYRHYLYXJSYMHZYDWLDFRYHBPYDTSSCNWBX"
        + "GLPZMLZZTQSSCPJMXXYCSJYTYCGHYCJWYRXXLFEMWJNMKLLSWTXHYYYNCMMCWJDQDJZGLLJWJRKHPZGGFLCCSCZMCBLTBHBQJXQD"
        + "SPDJZZGKGLFQYWBZYZJLTSTDHQHCTCBCHFLQMPWDSHYYTQWCNZZJTLBYMBPDYYYXSQKXWYYFLXXNCWCXYPMAELYKKJMZZZBRXYYQ"
        + "JFLJPFHHHYTZZXSGQQMHSPGDZQWBWPJHZJDYSCQWZKTXXSQLZYYMYSDZGRXCKKUJLWPYSYSCSYZLRMLQSYLJXBCXTLWDQZPCYCYK"
        + "PPPNSXFYZJJRCEMHSZMSXLXGLRWGCSTLRSXBZGBZGZTCPLUJLSLYLYMTXMTZPALZXPXJTJWTCYYZLBLXBZLQMYLXPGHDSLSSDMXM"
        + "BDZZSXWHAMLCZCPJMCNHJYSNSYGCHSKQMZZQDLLKABLWJXSFMOCDXJRRLYQZKJMYBYQLYHETFJZFRFKSRYXFJTWDSXXSYSQJYSLY"
        + "XWJHSNLXYYXHBHAWHHJZXWMYLJCSSLKYDZTXBZSYFDXGXZJKHSXXYBSSXDPYNZWRPTQZCZENYGCXQFJYKJBZMLJCMQQXUOXSLYXX"
        + "LYLLJDZBTYMHPFSTTQQWLHOKYBLZZALZXQLHZWRRQHLSTMYPYXJJXMQSJFNBXYXYJXXYQYLTHYLQYFMLKLJTMLLHSZWKZHLJMLHL"
        + "JKLJSTLQXYLMBHHLNLZXQJHXCFXXLHYHJJGBYZZKBXSCQDJQDSUJZYYHZHHMGSXCSYMXFEBCQWWRBPYYJQTYZCYQYQQZYHMWFFHG"
        + "ZFRJFCDPXNTQYZPDYKHJLFRZXPPXZDBBGZQSTLGDGYLCQMLCHHMFYWLZYXKJLYPQHSYWMQQGQZMLZJNSQXJQSYJYCBEHSXFSZPXZ"
        + "WFLLBCYYJDYTDTHWZSFJMQQYJLMQXXLLDTTKHHYBFPWTYYSQQWNQWLGWDEBZWCMYGCULKJXTMXMYJSXHYBRWFYMWFRXYQMXYSZTZ"
        + "ZTFYKMLDHQDXWYYNLCRYJBLPSXCXYWLSPRRJWXHQYPHTYDNXHHMMYWYTZCSQMTSSCCDALWZTCPQPYJLLQZYJSWXMZZMMYLMXCLMX"
        + "CZMXMZSQTZPPQQBLPGXQZHFLJJHYTJSRXWZXSCCDLXTYJDCQJXSLQYCLZXLZZXMXQRJMHRHZJBHMFLJLMLCLQNLDXZLLLPYPSYJY"
        + "SXCQQDCMQJZZXHNPNXZMEKMXHYKYQLXSXTXJYYHWDCWDZHQYYBGYBCYSCFGPSJNZDYZZJZXRZRQJJYMCANYRJTLDPPYZBSTJKXXZ"
        + "YPFDWFGZZRPYMTNGXZQBYXNBUFNQKRJQZMJEGRZGYCLKXZDSKKNSXKCLJSPJYYZLQQJYBZSSQLLLKJXTBKTYLCCDDBLSPPFYLGYD"
        + "TZJYQGGKQTTFZXBDKTYYHYBBFYTYYBCLPDYTGDHRYRNJSPTCSNYJQHKLLLZSLYDXXWBCJQSPXBPJZJCJDZFFXXBRMLAZHCSNDLBJ"
        + "DSZBLPRZTSWSBXBCLLXXLZDJZSJPYLYXXYFTFFFBHJJXGBYXJPMMMPSSJZJMTLYZJXSWXTYLEDQPJMYGQZJGDJLQJWJQLLSJGJGY"
        + "GMSCLJJXDTYGJQJQJCJZCJGDZZSXQGSJGGCXHQXSNQLZZBXHSGZXCXYLJXYXYYDFQQJHJFXDHCTXJYRXYSQTJXYEFYYSSYYJXNCY"
        + "ZXFXMSYSZXYYSCHSHXZZZGZZZGFJDLTYLNPZGYJYZYYQZPBXQBDZTZCZYXXYHHSQXSHDHGQHJHGYWSZTMZMLHYXGEBTYLZKQWYTJ"
        + "ZRCLEKYSTDBCYKQQSAYXCJXWWGSBHJYZYDHCSJKQCXSWXFLTYNYZPZCCZJQTZWJQDZZZQZLJJXLSBHPYXXPSXSHHEZTXFPTLQYZZ"
        + "XHYTXNCFZYYHXGNXMYWXTZSJPTHHGYMXMXQZXTSBCZYJYXXTYYZYPCQLMMSZMJZZLLZXGXZAAJZYXJMZXWDXZSXZDZXLEYJJZQBH"
        + "ZWZZZQTZPSXZTDSXJJJZNYAZPHXYYSRNQDTHZHYYKYJHDZXZLSWCLYBZYECWCYCRYLCXNHZYDZYDYJDFRJJHTRSQTXYXJRJHOJYN"
        + "XELXSFSFJZGHPZSXZSZDZCQZBYYKLSGSJHCZSHDGQGXYZGXCHXZJWYQWGYHKSSEQZZNDZFKWYSSTCLZSTSYMCDHJXXYWEYXCZAYD"
        + "MPXMDSXYBSQMJMZJMTZQLPJYQZCGQHXJHHLXXHLHDLDJQCLDWBSXFZZYYSCHTYTYYBHECXHYKGJPXHHYZJFXHWHBDZFYZBCAPNPG"
        + "NYDMSXHMMMMAMYNBYJTMPXYYMCTHJBZYFCGTYHWPHFTWZZEZSBZEGPFMTSKFTYCMHFLLHGPZJXZJGZJYXZSBBQSCZZLZCCSTPGXM"
        + "JSFTCCZJZDJXCYBZLFCJSYZFGSZLYBCWZZBYZDZYPSWYJZXZBDSYUXLZZBZFYGCZXBZHZFTPBGZGEJBSTGKDMFHYZZJHZLLZZGJQ"
        + "ZLSFDJSSCBZGPDLFZFZSZYZYZSYGCXSNXXCHCZXTZZLJFZGQSQYXZJQDCCZTQCDXZJYQJQCHXZTDLGSCXZSYQJQTZWLQDQZTQCHQ"
        + "QJZYEZZZPBWKDJFCJPZTYPQYQTTYNLMBDKTJZPQZQZZFPZSBNJLGYJDXJDZZKZGQKXDLPZJTCJDQBXDJQJSTCKNXBXZMSLYJCQMT"
        + "JQWWCJQNJNLLLHJCWQTBZQYDZCZPZZDZYDDCYZZZCCJTTJFZDPRRTZTJDCQTQZDTJNPLZBCLLCTZSXKJZQZPZLBZRBTJDCXFCZDB"
        + "CCJJLTQQPLDCGZDBBZJCQDCJWYNLLZYZCCDWLLXWZLXRXNTQQCZXKQLSGDFQTDDGLRLAJJTKUYMKQLLTZYTDYYCZGJWYXDXFRSKS"
        + "TQTENQMRKQZHHQKDLDAZFKYPBGGPZREBZZYKZZSPEGJXGYKQZZZSLYSYYYZWFQZYLZZLZHWCHKYPQGNPGBLPLRRJYXCCSYYHSFZF"
        + "YBZYYTGZXYLXCZWXXZJZBLFFLGSKHYJZEYJHLPLLLLCZGXDRZELRHGKLZZYHZLYQSZZJZQLJZFLNBHGWLCZCFJYSPYXZLZLXGCCP"
        + "ZBLLCYBBBBUBBCBPCRNNZCZYRBFSRLDCGQYYQXYGMQZWTZYTYJXYFWTEHZZJYWLCCNTZYJJZDEDPZDZTSYQJHDYMBJNYJZLXTSST"
        + "PHNDJXXBYXQTZQDDTJTDYYTGWSCSZQFLSHLGLBCZPHDLYZJYCKWTYTYLBNYTSDSYCCTYSZYYEBHEXHQDTWNYGYCLXTSZYSTQMYGZ"
        + "AZCCSZZDSLZCLZRQXYYELJSBYMXSXZTEMBBLLYYLLYTDQYSHYMRQWKFKBFXNXSBYCHXBWJYHTQBPBSBWDZYLKGZSKYHXQZJXHXJX"
        + "GNLJKZLYYCDXLFYFGHLJGJYBXQLYBXQPQGZTZPLNCYPXDJYQYDYMRBESJYYHKXXSTMXRCZZYWXYQYBMCLLYZHQYZWQXDBXBZWZMS"
        + "LPDMYSKFMZKLZCYQYCZLQXFZZYDQZPZYGYJYZMZXDZFYFYTTQTZHGSPCZMLCCYTZXJCYTJMKSLPZHYSNZLLYTPZCTZZCKTXDHXXT"
        + "QCYFKSMQCCYYAZHTJPCYLZLYJBJXTPNYLJYYNRXSYLMMNXJSMYBCSYSYLZYLXJJQYLDZLPQBFZZBLFNDXQKCZFYWHGQMRDSXYCYT"
        + "XNQQJZYYPFZXDYZFPRXEJDGYQBXRCNFYYQPGHYJDYZXGRHTKYLNWDZNTSMPKLBTHBPYSZBZTJZSZZJTYYXZPHSSZZBZCZPTQFZMY"
        + "FLYPYBBJQXZMXXDJMTSYSKKBJZXHJCKLPSMKYJZCXTMLJYXRZZQSLXXQPYZXMKYXXXJCLJPRMYYGADYSKQLSNDHYZKQXZYZTCGHZ"
        + "TLMLWZYBWSYCTBHJHJFCWZTXWYTKZLXQSHLYJZJXTMPLPYCGLTBZZTLZJCYJGDTCLKLPLLQPJMZPAPXYZLKKTKDZCZZBNZDYDYQZ"
        + "JYJGMCTXLTGXSZLMLHBGLKFWNWZHDXUHLFMKYSLGXDTWWFRJEJZTZHYDXYKSHWFZCQSHKTMQQHTZHYMJDJSKHXZJZBZZXYMPAGQM"
        + "STPXLSKLZYNWRTSQLSZBPSPSGZWYHTLKSSSWHZZLYYTNXJGMJSZSUFWNLSOZTXGXLSAMMLBWLDSZYLAKQCQCTMYCFJBSLXCLZZCL"
        + "XXKSBZQCLHJPSQPLSXXCKSLNHPSFQQYTXYJZLQLDXZQJZDYYDJNZPTUZDSKJFSLJHYLZSQZLBTXYDGTQFDBYAZXDZHZJNHHQBYKN"
        + "XJJQCZMLLJZKSPLDYCLBBLXKLELXJLBQYCXJXGCNLCQPLZLZYJTZLJGYZDZPLTQCSXFDMNYCXGBTJDCZNBGBQYQJWGKFHTNPYQZQ"
        + "GBKPBBYZMTJDYTBLSQMPSXTBNPDXKLEMYYCJYNZCTLDYKZZXDDXHQSHDGMZSJYCCTAYRZLPYLTLKXSLZCGGEXCLFXLKJRTLQJAQZ"
        + "NCMBYDKKCXGLCZJZXJHPTDJJMZQYKQSECQZDSHHADMLZFMMZBGNTJNNLGBYJBRBTMLBYJDZXLCJLPLDLPCQDHLXZLYCBLCXZZJAD"
        + "JLNZMMSSSMYBHBSQKBHRSXXJMXSDZNZPXLGBRHWGGFCXGMSKLLTSJYYCQLTSKYWYYHYWXBXQYWPYWYKQLSQPTNTKHQCWDQKTWPXX"
        + "HCPTHTWUMSSYHBWCRWXHJMKMZNGWTMLKFGHKJYLSYYCXWHYECLQHKQHTTQKHFZLDXQWYZYYDESBPKYRZPJFYYZJCEQDZZDLATZBB"
        + "FJLLCXDLMJSSXEGYGSJQXCWBXSSZPDYZCXDNYXPPZYDLYJCZPLTXLSXYZYRXCYYYDYLWWNZSAHJSYQYHGYWWAXTJZDAXYSRLTDPS"
        + "SYYFNEJDXYZHLXLLLZQZSJNYQYQQXYJGHZGZCYJCHZLYCDSHWSHJZYJXCLLNXZJJYYXNFXMWFPYLCYLLABWDDHWDXJMCXZTZPMLQ"
        + "ZHSFHZYNZTLLDYWLSLXHYMMYLMBWWKYXYADTXYLLDJPYBPWUXJMWMLLSAFDLLYFLBHHHBQQLTZJCQJLDJTFFKMMMBYTHYGDCQRDD"
        + "WRQJXNBYSNWZDBYYTBJHPYBYTTJXAAHGQDQTMYSTQXKBTZPKJLZRBEQQSSMJJBDJOTGTBXPGBKTLHQXJJJCTHXQDWJLWRFWQGWSH"
        + "CKRYSWGFTGYGBXSDWDWRFHWYTJJXXXJYZYSLPYYYPAYXHYDQKXSHXYXGSKQHYWFDDDPPLCJLQQEEWXKSYYKDYPLTJTHKJLTCYYHH"
        + "JTTPLTZZCDLTHQKZXQYSTEEYWYYZYXXYYSTTJKLLPZMCYHQGXYHSRMBXPLLNQYDQHXSXXWGDQBSHYLLPJJJTHYJKYPPTHYYKTYEZ"
        + "YENMDSHLCRPQFDGFXZPSFTLJXXJBSWYYSKSFLXLPPLBBBLBSFXFYZBSJSSYLPBBFFFFSSCJDSTZSXZRYYSYFFSYZYZBJTBCTSBSD"
        + "HRTJJBYTCXYJEYLXCBNEBJDSYXYKGSJZBXBYTFZWGENYHHTHZHHXFWGCSTBGXKLSXYWMTMBYXJSTZSCDYQRCYTWXZFHMYMCXLZNS"
        + "DJTTTXRYCFYJSBSDYERXJLJXBBDEYNJGHXGCKGSCYMBLXJMSZNSKGXFBNBPTHFJAAFXYXFPXMYPQDTZCXZZPXRSYWZDLYBBKTYQP"
        + "QJPZYPZJZNJPZJLZZFYSBTTSLMPTZRTDXQSJEHBZYLZDHLJSQMLHTXTJECXSLZZSPKTLZKQQYFSYGYWPCPQFHQHYTQXZKRSGTTSQ"
        + "CZLPTXCDYYZXSQZSLXLZMYCPCQBZYXHBSXLZDLTCDXTYLZJYYZPZYZLTXJSJXHLPMYTXCQRBLZSSFJZZTNJYTXMYJHLHPPLCYXQJ"
        + "QQKZZSCPZKSWALQSBLCCZJSXGWWWYGYKTJBBZTDKHXHKGTGPBKQYSLPXPJCKBMLLXDZSTBKLGGQKQLSBKKTFXRMDKBFTPZFRTBBR"
        + "FERQGXYJPZSSTLBZTPSZQZSJDHLJQLZBPMSMMSXLQQNHKNBLRDDNXXDHDDJCYYGYLXGZLXSYGMQQGKHBPMXYXLYTQWLWGCPBMQXC"
        + "YZYDRJBHTDJYHQSHTMJSBYPLWHLZFFNYPMHXXHPLTBQPFBJWQDBYGPNZTPFZJGSDDTQSHZEAWZZYLLTYYBWJKXXGHLFKXDJTMSZS"
        + "QYNZGGSWQSPHTLSSKMCLZXYSZQZXNCJDQGZDLFNYKLJCJLLZLMZZNHYDSSHTHZZLZZBBHQZWWYCRZHLYQQJBEYFXXXWHSRXWQHWP"
        + "SLMSSKZTTYGYQQWRSLALHMJTQJSMXQBJJZJXZYZKXBYQXBJXSHZTSFJLXMXZXFGHKZSZGGYLCLSARJYHSLLLMZXELGLXYDJYTLFB"
        + "HBPNLYZFBBHPTGJKWETZHKJJXZXXGLLJLSTGSHJJYQLQZFKCGNNDJSSZFDBCTWWSEQFHQJBSAQTGYPQLBXBMMYWXGSLZHGLZGQYF"
        + "LZBYFZJFRYSFMBYZHQGFWZSYFYJJPHZBYYZFFWODGRLMFTWLBZGYCQXCDJYGZYYYYTYTYDWEGAZYHXJLZYYHLRMGRXXZCLHNELJJ"
        + "TJTPWJYBJJBXJJTJTEEKHWSLJPLPSFYZPQQBDLQJJTYYQLYZKDKSQJYYQZLDQTGJQYZJSUCMRYQTHTEJMFCTYHYPKMHYZWJDQFHY"
        + "YXWSHCTXRLJHQXHCCYYYJLTKTTYTMXGTCJTZAYYOCZLYLBSZYWJYTSJYHBYSHFJLYGJXXTMZYYLTXXYPZLXYJZYZYYPNHMYMDYYL"
        + "BLHLSYYQQLLNJJYMSOYQBZGDLYXYLCQYXTSZEGXHZGLHWBLJHEYXTWQMAKBPQCGYSHHEGQCMWYYWLJYJHYYZLLJJYLHZYHMGSLJL"
        + "JXCJJYCLYCJPCPZJZJMMYLCQLNQLJQJSXYJMLSZLJQLYCMMHCFMMFPQQMFYLQMCFFQMMMMHMZNFHHJGTTHHKHSLNCHHYQDXTMMQD"
        + "CYZYXYQMYQYLTDCYYYZAZZCYMZYDLZFFFMMYCQZWZZMABTBYZTDMNZZGGDFTYPCGQYTTSSFFWFDTZQSSYSTWXJHXYTSXXYLBYQHW"
        + "WKXHZXWZNNZZJZJJQJCCCHYYXBZXZCYZTLLCQXYNJYCYYCYNZZQYYYEWYCZDCJYCCHYJLBTZYYCQWMPWPYMLGKDLDLGKQQBGYCHJ"
        + "XY";

        ///  
        /// 获得一个字符串的汉语拼音码
        ///  
        /// name="strText">字符串
        /// 汉语拼音码,该字符串只包含大写的英文字母
        public static string GetChineseSpell(string strText)
        {
            if (strText == null || strText.Length == 0)
                return strText;
            System.Text.StringBuilder myStr = new System.Text.StringBuilder();
            foreach (char vChar in strText)
            {
                // 若是字母则直接输出
                if ((vChar >= 'a' && vChar <= 'z') || (vChar >= 'A' && vChar <= 'Z'))
                    myStr.Append(char.ToUpper(vChar));
                else if ((int)vChar >= 19968 && (int)vChar <= 40869)
                {
                    // 对可以查找的汉字计算它的首拼音字母的位置，然后输出
                    myStr.Append(strChineseFirstPY[(int)vChar - 19968]);
                }
            }
            return myStr.ToString();
        }// GetChineseSpell 

        public static string GetFirstPinyin(string strText)
        {
            if (strText == null || strText.Length == 0)
                return strText;
            string myStr = string.Empty;
            char vChar = (strText.ToCharArray())[0];

            // 若是字母则直接返回
            if ((vChar >= 'a' && vChar <= 'z') || (vChar >= 'A' && vChar <= 'Z'))
                myStr = vChar.ToString();
            else if ((int)vChar >= 19968 && (int)vChar <= 40869)
            {
                // 对可以查找的汉字计算它的首拼音字母的位置，然后输出
                myStr = strChineseFirstPY[(int)vChar - 19968].ToString();
            }

            return myStr;
        }// 获取首字的形状拼音字母

        public static string AddFirstPinyin(string str)
        {
            if (string.IsNullOrEmpty(str)) return string.Empty;
            char vChar = (str.ToCharArray())[0];
            // 若是字母则直接返回
            if ((vChar >= 'a' && vChar <= 'z') || (vChar >= 'A' && vChar <= 'Z'))
            {
                return str;
            }
            else if ((int)vChar >= 19968 && (int)vChar <= 40869)
            {
                // 对可以查找的汉字计算它的首拼音字母的位置，然后输出
                string strNew = strChineseFirstPY[(int)vChar - 19968].ToString();
                return strNew + str;
            }
            else
            {
                return str;
            }
        }
    }
}
