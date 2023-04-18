using System.Xml;
using System.Xml.Serialization;
using System.Reflection;
using System.Collections;
using System;
using System.Data;
using System.Collections.Generic;
using System.Text;
using System.IO;
namespace UMC.Data.Sql
{
    static class CBO
    {
        public static void SetValue(object obvalue, System.Reflection.PropertyInfo objPropertyInfo, object drObj)
        {
            if (drObj != null)
            {

                if (drObj == DBNull.Value)
                {
                    return;
                }

                Type proType = objPropertyInfo.PropertyType;
                if (proType.IsEnum)
                {
                    objPropertyInfo.SetValue(obvalue, Enum.ToObject(proType, drObj), null);
                }
                else if (proType.IsGenericType)
                {
                    if (CBO.Nullable == proType.GetGenericTypeDefinition())
                    {
                        var ndType = proType.GetGenericArguments()[0];
                        if (ndType.IsEnum)
                        {
                            if (drObj is string)
                            {
                                objPropertyInfo.SetValue(obvalue, Enum.Parse(ndType, drObj as string), null);
                            }
                            else
                            {
                                objPropertyInfo.SetValue(obvalue, Enum.ToObject(ndType, drObj), null);
                            }
                        }
                        else if (drObj is string)
                        {
                            objPropertyInfo.SetValue(obvalue, UMC.Data.Reflection.Parse(drObj as string, ndType), null);
                        }
                        else if (ndType == typeof(DateTime))
                        {
                            if (drObj is DateTime)
                            {
                                objPropertyInfo.SetValue(obvalue, drObj, null);
                            }
                            else
                            {
                                objPropertyInfo.SetValue(obvalue, Reflection.TimeSpan(Convert.ToInt32(drObj)), null);
                            }
                        }
                        else
                        {
                            objPropertyInfo.SetValue(obvalue, Convert.ChangeType(drObj, ndType), null);
                        }
                    }

                }
                else
                {
                    if (drObj is string)
                    {
                        if (objPropertyInfo.GetCustomAttributes(typeof(JSONAttribute), true).Length > 0
                            ||
                            objPropertyInfo.PropertyType.GetCustomAttributes(typeof(JSONAttribute), true).Length > 0)
                        {
                            var str = (string)drObj;
                            if (String.IsNullOrEmpty(str) == false)
                            {
                                objPropertyInfo.SetValue(obvalue, JSON.Deserialize(str, proType), null);
                            }
                        }
                        else if (proType == typeof(String))
                        {
                            objPropertyInfo.SetValue(obvalue, drObj);
                        }
                        else
                        {
                            objPropertyInfo.SetValue(obvalue, UMC.Data.Reflection.Parse(drObj as string, proType));
                        }
                    }
                    else if (proType == typeof(String))
                    {
                        objPropertyInfo.SetValue(obvalue, drObj.ToString(), null);
                    }
                    else
                    {
                        objPropertyInfo.SetValue(obvalue, Convert.ChangeType(drObj, proType), null);
                    }
                }

            }
        }
        internal static T CreateObject<T>(T tObj, IDataReader dr) where T : Record
        {
            for (var i = 0; i < dr.FieldCount; i++)
            {
                tObj.SetValue(dr.GetName(i), dr[i]);
            }
            return tObj;
        }
        internal static readonly Type Nullable = typeof(Nullable<>);
        
    }



}