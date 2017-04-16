using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JarhfStomp4Net.Stomp
{
    /// <summary>
    /// 
    /// </summary>
    /// @author JHF
    /// @since 4.6
    public class MyDictionary<TKey, TValue> : Dictionary<TKey, TValue>
    {
        public void Set(TKey key, TValue value)
        {
            if (this.ContainsKey(key))
                base[key] = value;
            else
                this.Add(key, value);
        }
    }

    public class MultiMap<TKey, TValue> : Dictionary<TKey, List<TValue>>
    {
        public void Put(TKey key, TValue value)
        {
            List<TValue> list;
            if (this.TryGetValue(key, out list))
            {
                list.Add(value);
            }
            else
            {
                list = new List<TValue>();
                list.Add(value);
                base.Add(key, list);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Remove(TKey key, TValue value)
        {
            List<TValue> list;
            if (this.TryGetValue(key, out list))
            {
                list.Remove(value);
            }
        }

        public new List<TValue> this[TKey key]
        {
            get
            {
                List<TValue> list;
                if (!this.TryGetValue(key, out list))
                {
                    list = new List<TValue>();
                }
                return list;
            }
        }
    }
}
