using System;
#if WEBSOCKET4NET
using WebSocket4Net;
using SuperSocket.ClientEngine;
#endif
#if WEBSOCKETSHARP
using WebSocketSharp;
#endif
namespace JarhfStomp4Net.SockJs
{
    public interface IWebSocket
    {
        event EventHandler<MessageReceivedEventArgs> OnMessageReceived;
        event EventHandler<ErrorEventArgs> OnErrors;
        event EventHandler<EventArgs> OnOpened;
        event EventHandler<ClosedEventArgs> OnClosed;

        WebSocketState getSocketState();

        void Open();
        void Send(string data);
        void Close(ushort closeCode, string closeReason);
    }


    public enum WebSocketState : int
    {
        None = -1,
        Connecting = 0,
        Open = 1,
        Closing = 2,
        Closed = 3
    }
    
    public static class WebSocketFactory
    {
        public static IWebSocket GetWebSocket(string url)
        {
#if WEBSOCKET4NET
            return new WS4NetSocket(url);
#endif
#if WEBSOCKETSHARP
            return new WSSharpSocket(url);
#else
            return null;
#endif
        }
    }

}