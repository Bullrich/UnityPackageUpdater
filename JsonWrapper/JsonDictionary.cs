using System.Collections.Generic;

namespace PackageUpdater.JsonWrapper
{
    // https://github.com/Bullrich/Unity-Json-Wrapper
    
    /// <summary>Extension of Dictionary which allows modification of inner classes</summary>
    internal class JsonDictionary : Dictionary<string, object>
    {
        /// <summary>Get the value casted to the desired type</summary>
        public T Get<T>(string key)
        {
            return (T)base[key];
        }
    }
}