using System.Collections.Generic;

namespace PackageUpdater.JsonWrapper
{
    /// <summary>Extended List which brings a inner get with type casting</summary>
    internal class JsonList : List<object>
    {
        /// <summary>Get the index of the list casted to the desired type</summary>
        public T Get<T>(int index)
        {
            return (T)base[index];
        }
    }
}