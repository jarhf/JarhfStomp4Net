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
    public class Subscription
    {
        public string Id { get; set; }

        public Action<Frame> Callback { get; set; }

        public string Destination { get; set; }

        public override string ToString()
        {
            return $"Id:{Id}, Destination:{Destination}, Callback:{Callback?.Method.Name}";
        }
    }
}
