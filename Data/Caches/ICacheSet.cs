using System;
namespace UMC.Data.Caches
{
    public interface ICacheSet: IDataSubscribe
    {
        void Load();
        void Save();
        int Count
        {
            get;
        }
        void Flush();
        DateTime ShrinkTime
        {
            get;
        }
        Object Cache(System.Collections.Hashtable value);
        String Name
        {
            get;
        }
        int NameCode
        {
            get;
        }
        void Export(Action<byte[][]> export);
    }

}