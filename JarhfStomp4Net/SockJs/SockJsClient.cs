using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
#if WEBSOCKET4NET
using WebSocket4Net;
using SuperSocket.ClientEngine;
#endif
#if WEBSOCKETSHARP
using WebSocketSharp;
#endif
namespace JarhfStomp4Net.SockJs
{
    /// <summary>
    /// 
    /// </summary>
    /// @author JHF
    /// @since 4.6
    /// 
    /// Modified by fkahhaleh
    ///     Refactored code to allow for different WebSocket implementations via Interface Design Pattern
    ///     
    public class SocketJsClient
    {
        private static Random random = new Random();

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;
        public event EventHandler<ClosedEventArgs> OnClosed;
        public event EventHandler<EventArgs> OnOpened;
        public event EventHandler<ErrorEventArgs> OnError;

        IWebSocket websocket;

        public SocketJsClient(string url)
        {
            websocket = WebSocketFactory.GetWebSocket(GenerateTransportUrl(url));
            websocket.OnMessageReceived += SockJsClient_MessageReceived;
            websocket.OnOpened += Websocket_OnOpened;
            websocket.OnErrors += Websocket_OnError;
            websocket.OnClosed += Websocket_OnClosed;
        }

        private void Websocket_OnClosed(object sender, ClosedEventArgs e)
        {
            this.OnClosed?.Invoke(sender, e);
        }
        private void Websocket_OnError(object sender, ErrorEventArgs e)
        {
            this.OnError?.Invoke(sender, e);
        }
        private void Websocket_OnOpened(object sender, EventArgs e)
        {
            this.OnOpened?.Invoke(sender, e);
        }

        public WebSocketState State
        {
            get { return websocket.getSocketState(); }
        }
        private void SockJsClient_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            //After receiving the message, you will need to follow the SockJs format
            TransportMessage(e.Message);
        }

        public void Open()
        {
            if (websocket != null)
                websocket.Open();
        }
        /// <summary>
        /// 覆写了基类的Send方法，对消息数据进行了SockJs格式的包装
        /// </summary>
        /// <param name="data"></param>
        public void Send(string data)
        {
            websocket.Send(WebSockerHelperUtils.SockJsEncode(data));
        }

        public void Close()
        {

        }

        public string getSocketImplementationInfo()
        {
            string wsImplementationinfo = "Unknown";
            wsImplementationinfo = websocket?.GetType().BaseType?.FullName;

            return wsImplementationinfo;
        }

        private void OnMessageReceived(string msg)
        {
            this.MessageReceived?.Invoke(this, new MessageReceivedEventArgs(msg));
        }

        private void Debug(string msg)
        {
            System.Diagnostics.Debug.WriteLine(msg);
        }

        /// <summary>
        /// 处理SockJs消息
        /// </summary>
        /// <param name="msg"></param>
        private void TransportMessage(string msg)
        {
            if (msg == null || msg.Length < 1)
                return;

            string type = msg.Substring(0, 1);
            string content = msg.Substring(1);

            // first check for messages that don't need a payload
            switch (type)
            {
                case "o"://连接已打开
                    this.SockJsOpen();
                    return;
                case "h"://心跳
                    //这里待实现
                    //javascript src:
                    //this.dispatchEvent(new Event('heartbeat'));
                    //debug('heartbeat', this.transport);
                    return;
            }

            if (string.IsNullOrEmpty(content))
                return;

            JToken payload = null;
            try
            {
                payload = JToken.Parse(content);
            }
            catch (Exception ex)
            {
                Debug("sockjs payload bad json:" + ex.Message);
                return;
            }

            switch (type)
            {
                case "a"://array message
                    if (payload is JArray)
                    {
                        foreach (var item in payload)
                        {
                            Debug("message:" + item);
                            this.OnMessageReceived(item.ToString());
                        };
                    }
                    break;
                case "m"://message
                    Debug("message" + payload);
                    this.OnMessageReceived(payload.ToString());
                    break;
                case "c"://close                    
                    if ((payload is JArray) && payload.Count() == 2)
                    {
                        websocket.Close(ushort.Parse(payload[0].ToString()), payload[1].ToString());
                    }
                    break;
            }
        }
        /*
        SockJS.prototype._transportMessage = function(msg)
        {
            debug('_transportMessage', msg);
            var self = this
              , type = msg.slice(0, 1)
              , content = msg.slice(1)
              , payload
              ;

            // first check for messages that don't need a payload
            switch (type)
            {
                case 'o':
                    this._open();
                    return;
                case 'h':
                    this.dispatchEvent(new Event('heartbeat'));
                    debug('heartbeat', this.transport);
                    return;
            }

            if (content)
            {
                try
                {
                    payload = JSON3.parse(content);
                }
                catch (e)
                {
                    debug('bad json', content);
                }
            }

            if (typeof payload === 'undefined')
            {
                debug('empty payload', content);
                return;
            }

            switch (type)
            {
                case 'a':
                    if (Array.isArray(payload))
                    {
                        payload.forEach(function(p) {
                            debug('message', self.transport, p);
                            self.dispatchEvent(new TransportMessageEvent(p));
                        });
                    }
                    break;
                case 'm':
                    debug('message', this.transport, payload);
                    this.dispatchEvent(new TransportMessageEvent(payload));
                    break;
                case 'c':
                    if (Array.isArray(payload) && payload.length === 2)
                    {
                        this._close(payload[0], payload[1], true);
                    }
                    break;
            }
        };
        */

        private void SockJsOpen()
        {
            //待实现
            //javascript src:
            //SockJS.prototype._open = function()
            //{
            //    debug('_open', this._transport.transportName, this.readyState);
            //    if (this.readyState === SockJS.CONNECTING)
            //    {
            //        if (this._transportTimeoutId)
            //        {
            //            clearTimeout(this._transportTimeoutId);
            //            this._transportTimeoutId = null;
            //        }
            //        this.readyState = SockJS.OPEN;
            //        this.transport = this._transport.transportName;
            //        this.dispatchEvent(new Event('open'));
            //        debug('connected', this.transport);
            //    }
            //    else
            //    {
            //        // The server might have been restarted, and lost track of our
            //        // connection.
            //        this._close(1006, 'Server lost session');
            //    }
            //};
        }

        //transport,_transUrl,transportUrl的关系： _transUrl是传入的原始url
        //var transportUrl = urlUtils.addPath(this._transUrl, '/' + this._server + '/' + this._generateSessionId());
        //var options = this._transportOptions[Transport.transportName];
        //debug('transport url', transportUrl);
        //var transportObj = new Transport(transportUrl, this._transUrl, options);
        private static string GenerateServerId()
        {
            return random.Next(1000).ToString().PadLeft(3, '0');
        }

        private static string GenerateSessionId()
        {
            string sessionId = "";
            string randomStringChars = "abcdefghijklmnopqrstuvwxyz012345";
            int max = randomStringChars.Length;
            for (var i = 0; i < 8; i++)
            {
                sessionId += randomStringChars[random.Next(max)].ToString();
            }
            return sessionId;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        private static string GenerateTransportUrl(string uri)
        {
            var transportUrl = uri.TrimEnd('/') + $"/{GenerateServerId()}/{GenerateSessionId()}/websocket";
            if (transportUrl.StartsWith("https"))
            {
                transportUrl = "wss" + transportUrl.Substring(5);
            }
            else if (transportUrl.StartsWith("http"))
            {
                transportUrl = "ws" + transportUrl.Substring(4);
            }
            return transportUrl;
        }


        //WebSocketTransport.prototype.send = function(data)
        //{
        //    var msg = '[' + data + ']';
        //    debug('send', msg);
        //    this.ws.send(msg);
        //};

    }

#if WEBSOCKET4NET
    public class WS4NetSocket : WebSocket4Net.WebSocket, IWebSocket
    {
        public event EventHandler<MessageReceivedEventArgs> OnMessageReceived;
        public event EventHandler<ClosedEventArgs> OnClosed;
        public event EventHandler<ErrorEventArgs> OnErrors;
        public event EventHandler<EventArgs> OnOpened;

        public void Close(ushort closeCode, string closeReason)
        {
            base.Close((int)closeCode, closeReason);
        }

        public WebSocketState getSocketState()
        {
            WebSocketState retState = WebSocketState.None;
            switch (base.State)
            {
                case WebSocket4Net.WebSocketState.Connecting:
                    retState = WebSocketState.Connecting;
                    break;
                case WebSocket4Net.WebSocketState.Open:
                    retState = WebSocketState.Open;
                    break;
                case WebSocket4Net.WebSocketState.Closing:
                    retState = WebSocketState.Closing;
                    break;
                case WebSocket4Net.WebSocketState.Closed:
                    retState = WebSocketState.Closed;
                    break;
                default:
                    break;
            }
            return retState;
        }

        public WS4NetSocket(string url) : base(url)
        {
            base.MessageReceived += SockJsClient_MessageReceived;
        }

        private void SockJsClient_MessageReceived(object sender, WebSocket4Net.MessageReceivedEventArgs e)
        {
            //接受到消息后还需要根据SockJs的格式处理一下
            this.OnMessageReceived?.Invoke(sender, new MessageReceivedEventArgs(e.Message));
        }
    }
#endif

#if WEBSOCKETSHARP
    public class WSSharpSocket : WebSocketSharp.WebSocket, IWebSocket
    {
        public event EventHandler<MessageReceivedEventArgs> OnMessageReceived;
        public event EventHandler<ClosedEventArgs> OnClosed;
        public event EventHandler<EventArgs> OnOpened;
        public event EventHandler<ErrorEventArgs> OnErrors;

        //event EventHandler<ErrorEventArgs> IWebSocket.OnError
        //{
        //    add
        //    {
        //        throw new NotImplementedException();
        //    }

        //    remove
        //    {
        //        throw new NotImplementedException();
        //    }
        //}

        public void Open()
        {
            //if (base.ReadyState == WebSocketSharp.WebSocketState.Closed)
                base.Connect();
        }
        public new void Close(ushort closeCode, string closeReason)
        {
            if (base.ReadyState == WebSocketSharp.WebSocketState.Open)
            {
                if (Enum.IsDefined(typeof(CloseStatusCode), closeCode))
                {
                    CloseStatusCode code = (CloseStatusCode)closeCode;
                    base.Close(code, closeReason);
                }
                else
                    base.Close(CloseStatusCode.Undefined, "StatusCode was unknown");
            }
        }
        public WSSharpSocket(string url) : base(url)
        {
            base.OnOpen += WSSharpSocket_OnOpen;
            base.OnMessage += WSSharpSocket_OnMessage;
            base.OnClose += WSSharpSocket_OnClose;
            base.OnError += WSSharpSocket_OnError;
        }

        private void WSSharpSocket_OnError(object sender, WebSocketSharp.ErrorEventArgs e)
        {
            this.OnErrors?.Invoke(sender, new SockJs.ErrorEventArgs(e.Exception, e.Message));
        }

        public WebSocketState getSocketState()
        {
            WebSocketState retState = WebSocketState.None;
            switch (base.ReadyState)
            {
                case WebSocketSharp.WebSocketState.Connecting:
                    retState = WebSocketState.Connecting;
                    break;
                case WebSocketSharp.WebSocketState.Open:
                    retState = WebSocketState.Open;
                    break;
                case WebSocketSharp.WebSocketState.Closing:
                    retState = WebSocketState.Closing;
                    break;
                case WebSocketSharp.WebSocketState.Closed:
                    retState = WebSocketState.Closed;
                    break;
                default:
                    break;
            }
            return retState;
        }

        private void WSSharpSocket_OnOpen(object sender, EventArgs e)
        {
            this.OnOpened?.Invoke(sender, e);
        }
        private void WSSharpSocket_OnClose(object sender, CloseEventArgs e)
        {
            OnClosed?.Invoke(sender, new SockJs.ClosedEventArgs(e.Code, e.Reason, e.WasClean));
        }
        private void WSSharpSocket_OnMessage(object sender, MessageEventArgs e)
        {
            OnMessageReceived?.Invoke(sender, new SockJs.MessageReceivedEventArgs(e.Data));
        }

    }
#endif

    internal static class WebSockerHelperUtils
    {
#region 以下代码参考：https://github.com/sockjs/sockjs-client/blob/master/lib/utils/escape.js

        /// <summary>
        /// Some extra characters that Chrome gets wrong, and substitutes with something else on the wire.
        /// eslint-disable-next-line no-control-regex 
        /// </summary>
        static Regex extraEscapable = new Regex("[\x00-\x1f\ud800-\udfff\ufffe\uffff\u0300-\u0333\u033d-\u0346\u034a-\u034c\u0350-\u0352\u0357-\u0358\u035c-\u0362\u0374\u037e\u0387\u0591-\u05af\u05c4\u0610-\u0617\u0653-\u0654\u0657-\u065b\u065d-\u065e\u06df-\u06e2\u06eb-\u06ec\u0730\u0732-\u0733\u0735-\u0736\u073a\u073d\u073f-\u0741\u0743\u0745\u0747\u07eb-\u07f1\u0951\u0958-\u095f\u09dc-\u09dd\u09df\u0a33\u0a36\u0a59-\u0a5b\u0a5e\u0b5c-\u0b5d\u0e38-\u0e39\u0f43\u0f4d\u0f52\u0f57\u0f5c\u0f69\u0f72-\u0f76\u0f78\u0f80-\u0f83\u0f93\u0f9d\u0fa2\u0fa7\u0fac\u0fb9\u1939-\u193a\u1a17\u1b6b\u1cda-\u1cdb\u1dc0-\u1dcf\u1dfc\u1dfe\u1f71\u1f73\u1f75\u1f77\u1f79\u1f7b\u1f7d\u1fbb\u1fbe\u1fc9\u1fcb\u1fd3\u1fdb\u1fe3\u1feb\u1fee-\u1fef\u1ff9\u1ffb\u1ffd\u2000-\u2001\u20d0-\u20d1\u20d4-\u20d7\u20e7-\u20e9\u2126\u212a-\u212b\u2329-\u232a\u2adc\u302b-\u302c\uaab2-\uaab3\uf900-\ufa0d\ufa10\ufa12\ufa15-\ufa1e\ufa20\ufa22\ufa25-\ufa26\ufa2a-\ufa2d\ufa30-\ufa6d\ufa70-\ufad9\ufb1d\ufb1f\ufb2a-\ufb36\ufb38-\ufb3c\ufb3e\ufb40-\ufb41\ufb43-\ufb44\ufb46-\ufb4e\ufff0-\uffff]");

        static Dictionary<string, string> _extraLookup;
        static Dictionary<string, string> extraLookup
        {
            get
            {
                if (_extraLookup == null)
                    _extraLookup = unrollLookup(extraEscapable);
                return _extraLookup;
            }
        }

        /// <summary>
        /// This may be quite slow, so let's delay until user actually uses bad characters.
        /// </summary>
        /// <param name="escapable"></param>
        /// <returns></returns>
        static Dictionary<string, string> unrollLookup(Regex escapable)
        {
            Dictionary<string, string> unrolled = new Dictionary<string, string>();

            StringBuilder c = new StringBuilder();
            for (int i = 0; i < 65536; i++)
            {
                c.Append(((char)i).ToString());
            }
            extraEscapable.Replace(c.ToString(), (Match a) =>
            {
                string hexStr = "0000" + Convert.ToString((int)a.Value[0], 16);
                hexStr = hexStr.Substring(hexStr.Length - 4);
                unrolled.Add(a.Value, "\\u" + hexStr);
                return "";
            });

            return unrolled;
        }
        /// <summary>
        /// Quote string, also taking care of unicode characters that browsers
        /// often break. Especially, take care of unicode surrogates:
        /// <para>http://en.wikipedia.org/wiki/Mapping_of_Unicode_characters#Surrogates</para>
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        internal static string Quote(string str)
        {
            var quoted = JsonConvert.ToString(str);

            // In most cases this should be very fast and good enough.           
            if (!extraEscapable.Match(quoted).Success)
            {
                return quoted;
            }

            return extraEscapable.Replace(quoted, (Match a) =>
            {
                return extraLookup[a.Value];
            });
        }
#endregion


        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        internal static string SockJsEncode(string data)
        {
            return BracketData(Quote(data));
        }

        /// <summary>
        /// sockjs会将数据用[]包起来
        /// <para>考参代码：https://github.com/sockjs/sockjs-client/blob/master/dist/sockjs.js 第2983行</para>
        /// </summary>
        /// <![CDATA[]]>
        /// <param name="data"></param>
        /// <returns></returns>
        internal static string BracketData(string data)
        {
            return "[" + data + "]";
        }
    }

}
