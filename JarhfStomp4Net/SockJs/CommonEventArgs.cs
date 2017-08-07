using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JarhfStomp4Net.SockJs
{
    public class MessageReceivedEventArgs : EventArgs
    {
        public MessageReceivedEventArgs(string msgBody)
        {
            this.Message = msgBody;
        }
        public string Message { get; internal set; }
    }
    public class ClosedEventArgs : EventArgs
    {
        internal ClosedEventArgs(ushort code, string reason, bool cleanlyClosed)
        {
            this.Code = code;
            this.Reason = reason;
            this.WasClean = cleanlyClosed;
        }
        public ushort Code { get; }
        public string Reason { get; }
        public bool WasClean { get; }
    }
    public class ErrorEventArgs : EventArgs
    {
        internal ErrorEventArgs(Exception ex, string errDetails)
        {
            this.Exception = ex;
            this.Message = errDetails;
        }
        public Exception Exception { get; }
        public string Message { get; }
    }
}
