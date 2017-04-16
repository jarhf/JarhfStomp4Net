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
    public class StompHeader : MyDictionary<string, string>
    {
        //以下是Stomp消息头demo，\n\n之前的是头
        //请求连接时发送的消息："CONNECT\naccept-version:1.1,1.0\nheart-beat:10000,10000\n\n\u0000"
        //接收到连接成功的消息："CONNECTED\nversion:1.1\nheart-beat:0,0\nuser-name:admin@mdm\n\n\u0000"
        //请求订阅发送的消息："SUBSCRIBE\nid:sub-0\ndestination:/role/qbmadmin/topic/news\n\n\u0000"
        //接受到订阅的消息："MESSAGE\ndestination:/role/qbmadmin/topic/news\ncontent-type:text/plain;charset=UTF-8\nsubscription:sub-0\nmessage-id:1ckrvcko-11\ncontent-length:160\n\n{\"content\":\"京东1方\",\"createDate\":\"2017-03-27T11:16:27.252\",\"id\":347,\"roles\":[\"mdm\",\"qbmadmin\"],\"rolesStr\":\"mdm,qbmadmin\",\"title\":\"3\",\"type\":3,\"unread\":true}\u0000"

        public static string SubscriptionHeader = "subscription";
        public static string DestinationHeader = "destination";
        public static string ContentLengthHeader = "content-length";

        public string Subscription
        {
            get
            {
                return Get(SubscriptionHeader);
            }
            set
            {
                this.Set(SubscriptionHeader, value);
            }
        }
        public string Destination
        {
            get
            {
                return Get(DestinationHeader);
            }
            set
            {
                this.Set(DestinationHeader, value);
            }
        }
        public string ContentLength
        {
            get
            {
                return Get(ContentLengthHeader);
            }
            set
            {
                this.Set(ContentLengthHeader, value);
            }
        }


        public string Get(string key)
        {
            string value;
            if (this.TryGetValue(key, out value))
            {
                return value;
            }
            return null;
        }
    }

    public class StompSubscription : MyDictionary<string, Action<Frame>>
    {
        public Action<Frame> Get(string key)
        {
            Action<Frame> value;
            if (this.TryGetValue(key, out value))
            {
                return value;
            }
            return null;
        }
    }
}
