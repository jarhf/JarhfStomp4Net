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
    public enum CommandEnum
    {
        CONNECT,
        CONNECTED,
        SEND,
        SUBSCRIBE,
        UNSUBSCRIBE,
        ACK,
        NACK,
        BEGIN,
        COMMIT,
        ABORT,
        DISCONNECT,
        MESSAGE,
        RECEIPT,
        ERROR
    }
}
