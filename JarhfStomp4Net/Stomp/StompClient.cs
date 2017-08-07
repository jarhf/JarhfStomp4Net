using JarhfStomp4Net.SockJs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JarhfStomp4Net.Stomp
{
    /// <summary>
    /// Stomp协议官网文档：http://stomp.github.io/
    /// <para></para>Stomp参考文档：https://segmentfault.com/a/,http://jmesnil.net/stomp-websocket/doc/
    /// <para></para>Stomp.js是javascript实现的Stomp客户端：https://raw.githubusercontent.com/jmesnil/stomp-websocket/master/lib/stomp.js
    /// <para></para>Apache.NMS.Stomp是.net版的stomp client，不过不支持Stomp over WebSocket，一般是tcp和ActiveMQ。API风格类似JMS：http://activemq.apache.org/nms/apachenmsstomp.html   install-package Apache.NMS.Stomp
    /// <para></para>只好手工仿照stomp.js翻译成了本类
    /// </summary>
    /// @author JHF
    /// @since 4.6
    public class StompClient
    {
        int Counter;
        int heartbeat_outgoing = 10000;
        int heartbeat_incoming = 10000;
        int maxWebSocketFrameSize = 16 * 1024;
        /// <summary>
        /// 回调集合
        /// </summary>
        StompCallbacks Callbacks = new StompCallbacks();

        /// <summary>
        /// 当前使用的SockJsClient
        /// </summary>
        public SocketJsClient SockJs { get; set; }

        public StompStatus Status { get; set; }

        /// <summary>
        /// 所有Stomp消息订阅
        /// <para></para>请求订阅是id字段："SUBSCRIBE\nid:sub-1\ndestination:/topic/news\n\n\u0000" 
        /// <para></para>收到订阅的消息是subscription字段：MESSAGE\ndestination:/role/qbmadmin/topic/news\ncontent-type:text/plain;charset=UTF-8\nsubscription:sub-0\nmessage-id:1ckrvcko-11\ncontent-length:160\n\n{\"content\":\"京东1方\",\"createDate\":\"2017-03-27T11:16:27.252\",\"id\":347,\"roles\":[\"mdm\",\"qbmadmin\"],\"rolesStr\":\"mdm,qbmadmin\",\"title\":\"3\",\"type\":3,\"unread\":true}\u0000"
        /// </summary>
        public List<Subscription> Subscriptions = new List<Subscription>();

        string partialData = "";
        string VERSIONS_V1_0 = "1.0";
        string VERSIONS_V1_1 = "1.1";
        string VERSIONS_V1_2 = "1.2";
        string SUPPORTED_VERSIONS = "1.2";//"1.1,1.0";
        Timer pinger;   
        Timer ponger;
        DateTime serverActivity = DateTime.Now;


        public StompClient(string uri)
        {
            this.SockJs = new SocketJsClient(uri);
            this.SockJs.MessageReceived += Ws_MessageReceived;
            this.SockJs.OnClosed += Ws_Closed;
            this.SockJs.OnOpened += Ws_Opened;
            this.SockJs.OnError += Ws_Error;
            
        }

        private void Ws_Error(object sender, ErrorEventArgs e)
        {
            this.Debug("WebSocket Error:" + e.Exception);
        }

        private void Ws_Opened(object sender, EventArgs e)
        {
            this.Debug("WebSocket Opened");
            //websocket打开后马上发送stomp连接请求
            SendStompConnectCmd(null);
        }

        private void Ws_Closed(object sender, ClosedEventArgs e)
        {
            string code = "";
            string reason = "";
            if (e != null)
            {
                System.Net.WebSockets.WebSocketCloseStatus closeStatus = (System.Net.WebSockets.WebSocketCloseStatus)e.Code;
                code = e.Code + "(" + closeStatus.ToString() + ")";
                reason = e.Reason;
            }
            this.Status = StompStatus.Disconnected;
            Callbacks.Disconnected?.Invoke();
            this.Debug($"Whoops! WebSocket Closed: {code} {reason}");
            this.CleanUp();
        }

        private void CleanUp()
        {
            this.Subscriptions.Clear();
            this.pinger?.Dispose();
            this.ponger?.Dispose();
        }
        /*
         Client.prototype._cleanUp = function () {
            this.connected = false;
            if (this.pinger) {
                Stomp.clearInterval(this.pinger);
            }
            if (this.ponger) {
                return Stomp.clearInterval(this.ponger);
            }
        }; 
         */

        private Action<Frame> GetSubscribeCallback(string id)
        {
            return Subscriptions.FirstOrDefault(i => i.Id == id)?.Callback;
        }

        private void Ws_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            string data = e.Message;

            this.serverActivity = DateTime.Now;
            if (data == Frame.LF.ToString())
            {
                this.Debug("<<< PONG");
                return;
            }
            this.Debug("<<< " + data);

            var unmarshalledData = Frame.Unmarshall(this.partialData + data);
            this.partialData = unmarshalledData.Partial;
            var frames = unmarshalledData.Frames;

            for (int _i = 0, _len = frames.Count; _i < _len; _i++)
            {
                var frame = frames[_i];
                switch (frame.Command)
                {
                    case CommandEnum.CONNECTED:
                        this.Status = StompStatus.Connected;
                        this.Debug("connected to server " + frame.GetHeader("server"));
                        this.SetupHeartbeat(frame.Headers);
                        Callbacks.ConnectSuccess?.Invoke(frame);
                        break;

                    case CommandEnum.MESSAGE:
                        var subscription = frame.GetHeader("subscription");
                        var onreceive = this.GetSubscribeCallback(subscription);
                        if (onreceive != null)
                        {
                            var messageID = frame.GetHeader("message-id");
                            onreceive.Invoke(frame);
                        }
                        else
                        {
                            this.Debug("Unhandled received MESSAGE: " + frame);
                        }
                        break;
                    //case CommandEnum.RECEIPT:
                    //    this.onreceipt?.Invoke(frame)
                    //    break;
                    case CommandEnum.ERROR:
                        Callbacks.Error?.Invoke(frame);
                        break;
                    default:
                        this.Debug("Unhandled frame: " + frame);
                        break;
                }
            }
        }

        private void Transmit(CommandEnum command, StompHeader headers, string body = null)
        {
            string msg = Frame.Marshall(command, headers, body);
            this.Debug(msg);
            while (true)
            {
                if (msg.Length > this.maxWebSocketFrameSize)
                {
                    this.SockJs.Send(msg.Substring(0, this.maxWebSocketFrameSize));
                    msg = msg.Substring(this.maxWebSocketFrameSize);
                    this.Debug("remaining = " + msg.Length);
                }
                else
                {
                    this.SockJs.Send(msg);
                    return;
                }
            }
        }
        /*
        Client.prototype._transmit = function (command, headers, body) {
            var out;
            out = Frame.marshall(command, headers, body);
            if (typeof this.debug === "function") {
                this.debug(">>> " + out);
            }
            while (true) {
                if (out.length > this.maxWebSocketFrameSize) {
                    this.ws.send(out.substring(0, this.maxWebSocketFrameSize));
                    out = out.substring(this.maxWebSocketFrameSize);
                    if (typeof this.debug === "function") {
                        this.debug("remaining = " + out.length);
                    }
                } else {
                    return this.ws.send(out);
                }
            }
        };
         */


        private void SetupHeartbeat(Dictionary<string, string> headers)
        {
            if (headers == null || !headers.ContainsKey("version"))
                return;
            string version = headers["version"];
            if (version != VERSIONS_V1_1 && version != VERSIONS_V1_2)
                return;

            var arr = headers["heart-beat"].Split(',');
            List<int> heartBeat = new List<int>();
            for (int i = 0, len = arr.Length; i < len; i++)
            {
                string v = arr[i];
                heartBeat.Add(int.Parse(v));
            }

            int serverOutgoing = heartBeat[0];
            int serverIncoming = heartBeat[1];

            if (!(this.heartbeat_outgoing == 0 || serverIncoming == 0))
            {
                int ttl = Math.Max(this.heartbeat_outgoing, serverIncoming);

                this.Debug("send PING every " + ttl + "ms");

                Action<object> pingAction = (object o) =>
                {
                    this.SockJs.Send(Frame.LF.ToString());
                    this.Debug(">>> PING");
                };

                this.pinger = new System.Threading.Timer(new TimerCallback(pingAction), null, 0, ttl);
            }

            if (!(this.heartbeat_incoming == 0 || serverOutgoing == 0))
            {
                int ttl = Math.Max(this.heartbeat_incoming, serverOutgoing);

                this.Debug("check PONG every " + ttl + "ms");
                Action<object> pongAction = (object o) =>
                {
                    int delta = (int)(DateTime.Now - this.serverActivity).TotalMilliseconds;
                    if (delta > ttl * 2)
                    {
                        this.Debug("did not receive server activity for the last " + delta + "ms");
                        this.SockJs.Close();
                    }
                };
                this.ponger = new Timer(new TimerCallback(pongAction), null, 0, ttl);
            }
        }
        /*
         Client.prototype._setupHeartbeat = function (headers) {
            var serverIncoming, serverOutgoing, ttl, v, _ref, _ref1;
            if ((_ref = headers.version) !== Stomp.VERSIONS.V1_1 && _ref !== Stomp.VERSIONS.V1_2) {
                return;
            }
            _ref1 = (function () {
                var _i, _len, _ref1, _results;
                _ref1 = headers['heart-beat'].split(",");
                _results = [];
                for (_i = 0, _len = _ref1.length; _i < _len; _i++) {
                    v = _ref1[_i];
                    _results.push(parseInt(v));
                }
                return _results;
            })(), serverOutgoing = _ref1[0], serverIncoming = _ref1[1];
            if (!(this.heartbeat.outgoing === 0 || serverIncoming === 0)) {
                ttl = Math.max(this.heartbeat.outgoing, serverIncoming);
                if (typeof this.debug === "function") {
                    this.debug("send PING every " + ttl + "ms");
                }
                this.pinger = Stomp.setInterval(ttl, (function (_this) {
                    return function () {
                        _this.ws.send(Byte.LF);
                        return typeof _this.debug === "function" ? _this.debug(">>> PING") : void 0;
                    };
                })(this));
            }
            if (!(this.heartbeat.incoming === 0 || serverOutgoing === 0)) {
                ttl = Math.max(this.heartbeat.incoming, serverOutgoing);
                if (typeof this.debug === "function") {
                    this.debug("check PONG every " + ttl + "ms");
                }
                return this.ponger = Stomp.setInterval(ttl, (function (_this) {
                    return function () {
                        var delta;
                        delta = now() - _this.serverActivity;
                        if (delta > ttl * 2) {
                            if (typeof _this.debug === "function") {
                                _this.debug("did not receive server activity for the last " + delta + "ms");
                            }
                            return _this.ws.close();
                        }
                    };
                })(this));
            }
        };
         */
        public void Connect(StompHeader headers, Action<Frame> connectCallback, Action<Frame> failedCallback = null)
        {
            Callbacks.ConnectSuccess = connectCallback;
            Callbacks.Error = failedCallback;
            this.SockJs.OnError += (sender, args) => { Console.WriteLine(args.Exception.Message); };
            
            if (this.SockJs.State == WebSocketState.Open)
            {
                connectCallback(new Frame());
            }
            else
            {//先要将WebSocket打开，才能发送Stomp连接命令
                this.Status = StompStatus.Connecting;
                this.Debug("Opening Web Socket for stomp connect...");
                this.SockJs.Open();
            }
        }
        /* javascript源代码。注意：js中WebSocket是new的时候就open的（不过也是异步的）
         Client.prototype.connect = function () {
            var args, errorCallback, headers, out;
            args = 1 <= arguments.length ? __slice.call(arguments, 0) : [];
            out = this._parseConnect.apply(this, args);
            headers = out[0], this.connectCallback = out[1], errorCallback = out[2];
            if (typeof this.debug === "function") {
                this.debug("Opening Web Socket...");
            }
           
            this.ws.onmessage = (function (_this) {
                return function (evt) {
                    var arr, c, client, data, frame, messageID, onreceive, subscription, unmarshalledData, _i, _len, _ref, _results;
                    data = typeof ArrayBuffer !== 'undefined' && evt.data instanceof ArrayBuffer ? (arr = new Uint8Array(evt.data), typeof _this.debug === "function" ? _this.debug("--- got data length: " + arr.length) : void 0, ((function () {
                        var _i, _len, _results;
                        _results = [];
                        for (_i = 0, _len = arr.length; _i < _len; _i++) {
                            c = arr[_i];
                            _results.push(String.fromCharCode(c));
                        }
                        return _results;
                    })()).join('')) : evt.data;
                    _this.serverActivity = now();
                    if (data === Byte.LF) {
                        if (typeof _this.debug === "function") {
                            _this.debug("<<< PONG");
                        }
                        return;
                    }
                    if (typeof _this.debug === "function") {
                        _this.debug("<<< " + data);
                    }
                    unmarshalledData = Frame.unmarshall(_this.partialData + data);
                    _this.partialData = unmarshalledData.partial;
                    _ref = unmarshalledData.frames;
                    _results = [];
                    for (_i = 0, _len = _ref.length; _i < _len; _i++) {
                        frame = _ref[_i];
                        switch (frame.command) {
                            case "CONNECTED":
                                if (typeof _this.debug === "function") {
                                    _this.debug("connected to server " + frame.headers.server);
                                }
                                _this.connected = true;
                                _this._setupHeartbeat(frame.headers);
                                _results.push(typeof _this.connectCallback === "function" ? _this.connectCallback(frame) : void 0);
                                break;
                            case "MESSAGE":
                                subscription = frame.headers.subscription;
                                onreceive = _this.subscriptions[subscription] || _this.onreceive;
                                if (onreceive) {
                                    client = _this;
                                    messageID = frame.headers["message-id"];
                                    frame.ack = function (headers) {
                                        if (headers == null) {
                                            headers = {};
                                        }
                                        return client.ack(messageID, subscription, headers);
                                    };
                                    frame.nack = function (headers) {
                                        if (headers == null) {
                                            headers = {};
                                        }
                                        return client.nack(messageID, subscription, headers);
                                    };
                                    _results.push(onreceive(frame));
                                } else {
                                    _results.push(typeof _this.debug === "function" ? _this.debug("Unhandled received MESSAGE: " + frame) : void 0);
                                }
                                break;
                            case "RECEIPT":
                                _results.push(typeof _this.onreceipt === "function" ? _this.onreceipt(frame) : void 0);
                                break;
                            case "ERROR":
                                _results.push(typeof errorCallback === "function" ? errorCallback(frame) : void 0);
                                break;
                            default:
                                _results.push(typeof _this.debug === "function" ? _this.debug("Unhandled frame: " + frame) : void 0);
                        }
                    }
                    return _results;
                };
            })(this);
            this.ws.onclose = (function (_this) {
                return function () {
                    var msg;
                    msg = "Whoops! Lost connection to " + _this.ws.url;
                    if (typeof _this.debug === "function") {
                        _this.debug(msg);
                    }
                    _this._cleanUp();
                    return typeof errorCallback === "function" ? errorCallback(msg) : void 0;
                };
            })(this);
            return this.ws.onopen = (function (_this) {
                return function () {
                    if (typeof _this.debug === "function") {
                        _this.debug('Web Socket Opened...');
                    }
                    headers["accept-version"] = Stomp.VERSIONS.supportedVersions();
                    headers["heart-beat"] = [_this.heartbeat.outgoing, _this.heartbeat.incoming].join(',');
                    return _this._transmit("CONNECT", headers);
                };
            })(this);
        };
         */

        private void SendStompConnectCmd(StompHeader headers)
        {
            if (headers == null)
                headers = new Stomp.StompHeader();
            if (!headers.ContainsKey("accept-version"))
            {
                headers["accept-version"] = SUPPORTED_VERSIONS;
            }
            if (!headers.ContainsKey("heart-beat"))
            {
                headers["heart-beat"] = heartbeat_outgoing + "," + heartbeat_incoming;
            }
            this.Transmit(CommandEnum.CONNECT, headers);
        }

        public void Disconnect(Action disconnectCallback, StompHeader headers)
        {
            Callbacks.Disconnected = disconnectCallback;
            this.Transmit(CommandEnum.DISCONNECT, headers);
            this.SockJs.Close();
        }
        /*
          Client.prototype.disconnect = function (disconnectCallback, headers) {
            if (headers == null) {
                headers = {};
            }
            this._transmit("DISCONNECT", headers);
            this.ws.onclose = null;
            this.ws.close();
            this._cleanUp();
            return typeof disconnectCallback === "function" ? disconnectCallback() : void 0;
        };
         */

        public void Send(string destination, StompHeader headers, string body)
        {
            if (body == null)
            {
                body = "";
            }
            headers.Destination = destination;
            this.Transmit(CommandEnum.SEND, headers, body);
        }
        /*
         Client.prototype.send = function (destination, headers, body) {
            if (headers == null) {
                headers = {};
            }
            if (body == null) {
                body = '';
            }
            headers.destination = destination;
            return this._transmit("SEND", headers, body);
        };
         */


        public void Subscribe(string destination, Action<Frame> callback, StompHeader headers = null)
        {
            if (headers == null)
            {
                headers = new StompHeader();
            }
            if (!headers.ContainsKey("id"))
            {
                headers.Set("id", "sub-" + this.Counter++);
            }
            headers.Destination = destination;
            this.Subscriptions.Add(new Stomp.Subscription()
            {
                Id = headers.Get("id"),
                Callback = callback,
                Destination = destination
            });
            this.Transmit(CommandEnum.SUBSCRIBE, headers);
        }
        /*
         Client.prototype.subscribe = function (destination, callback, headers) {
            var client;
            if (headers == null) {
                headers = {};
            }
            if (!headers.id) {
                headers.id = "sub-" + this.counter++;
            }
            headers.destination = destination;
            this.subscriptions[headers.id] = callback;
            this._transmit("SUBSCRIBE", headers);
            client = this;
            return {
                id: headers.id,
                unsubscribe: function () {
                    return client.unsubscribe(headers.id);
                }
            };
        };
         */

        public void Unsubscribe(string id)
        {
            this.Subscriptions.RemoveAll(i => i.Id == id);

            this.Transmit(CommandEnum.UNSUBSCRIBE, new StompHeader() { ["id"] = id });
        }
        /*
         Client.prototype.unsubscribe = function (id) {
            delete this.subscriptions[id];
            return this._transmit("UNSUBSCRIBE", {
                id: id
            });
        };
         */

        public void UnsubscribeAll()
        {
            foreach (var item in this.Subscriptions)
            {
                this.Transmit(CommandEnum.UNSUBSCRIBE, new StompHeader() { ["id"] = item.Id });
            }
            this.Subscriptions.Clear();
        }

        #region 
        /*
         Client.prototype.begin = function (transaction) {
            var client, txid;
            txid = transaction || "tx-" + this.counter++;
            this._transmit("BEGIN", {
                transaction: txid
            });
            client = this;
            return {
                id: txid,
                commit: function () {
                    return client.commit(txid);
                },
                abort: function () {
                    return client.abort(txid);
                }
            };
        };

        Client.prototype.commit = function (transaction) {
            return this._transmit("COMMIT", {
                transaction: transaction
            });
        };

        Client.prototype.abort = function (transaction) {
            return this._transmit("ABORT", {
                transaction: transaction
            });
        };

        Client.prototype.ack = function (messageID, subscription, headers) {
            if (headers == null) {
                headers = {};
            }
            headers["message-id"] = messageID;
            headers.subscription = subscription;
            return this._transmit("ACK", headers);
        };

        Client.prototype.nack = function (messageID, subscription, headers) {
            if (headers == null) {
                headers = {};
            }
            headers["message-id"] = messageID;
            headers.subscription = subscription;
            return this._transmit("NACK", headers);
        };
         */
        public void Begin(string transaction)
        {
            string txid = transaction ?? "tx-" + this.Counter++;
            this.Transmit(CommandEnum.BEGIN, new StompHeader() { ["transaction"] = txid });
        }

        public void Commit(string transaction)
        {
            this.Transmit(CommandEnum.COMMIT, new StompHeader() { ["transaction"] = transaction });
        }

        public void Abort(string transaction)
        {
            this.Transmit(CommandEnum.ABORT, new StompHeader() { ["transaction"] = transaction });
        }

        public void Ack(string messageID, string subscription, StompHeader headers = null)
        {
            if (headers == null)
            {
                headers = new Stomp.StompHeader();
            }
            headers.Set("message-id", messageID);
            headers.Subscription = subscription;
            this.Transmit(CommandEnum.ACK, headers);
        }

        public void Nack(string messageId, string subscription, StompHeader headers = null)
        {
            if (headers == null)
            {
                headers = new Stomp.StompHeader();
            }
            headers.Set("message-id", messageId);
            headers.Subscription = subscription;
            this.Transmit(CommandEnum.NACK, headers);
        }
        #endregion

        private void Debug(string msg)
        {
            System.Diagnostics.Debug.WriteLine(msg);
        }

    }


    class StompCallbacks
    {
        /// <summary>
        /// Stomp连接成功的回调
        /// </summary>
        public Action<Frame> ConnectSuccess;
        /// <summary>
        /// 发生错误的回调
        /// </summary>
        public Action<Frame> Error;
        /// <summary>
        /// 关闭Stomp连接的回调
        /// </summary>
        public Action Disconnected;

        /// <summary>
        /// 所有订阅消息的回调
        /// </summary>
        public Dictionary<string, Action<Frame>> SubscriptionCallbacks { get; set; } = new Dictionary<string, Action<Frame>>();

        public Action<Frame> Get(string key)
        {
            return SubscriptionCallbacks.ContainsKey(key) ? SubscriptionCallbacks[key] : null;
        }

        public void Set(string key, Action<Frame> callback)
        {
            if (SubscriptionCallbacks.ContainsKey(key))
            {
                SubscriptionCallbacks[key] = callback;
            }
            else
            {
                SubscriptionCallbacks.Add(key, callback);
            }
        }

        public void Remove(string key)
        {
            if (SubscriptionCallbacks.ContainsKey(key))
                SubscriptionCallbacks.Remove(key);
        }
    }

}
