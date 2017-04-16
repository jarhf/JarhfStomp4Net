# JarhfStomp4Net
A C# stomp client, using for communication with spring websocket server.  

服务器端用的是spring websocket, 配置的stomp消息通信协助，websocket配置sockjs实现（貌似别无他选）。配置如下 :
```
<websocket:message-broker application-destination-prefix="/app">
        <websocket:stomp-endpoint path="/mywebsocket" allowed-origins="*">
            <websocket:sockjs/>
        </websocket:stomp-endpoint>
        <websocket:simple-broker prefix="/notification"/>       
</websocket:message-broker>
```
但是呢，我们客户端有多种，其中包含C#版的桌面程序。网上找了一些C#版的StompClient，要么用不了，要么就是没有符合sockjs协议要求的WebSocket实现。
于是自己写了这个库，用于和server端进行websocket通信。

# StompClient Usage
```
string uri = "http://localhost:8083/mywebsocket"; // uri also can be "ws://localhost:8083/mywebsocket"
StompClient client = new StompClient(uri);
string destincation = "/notification";
client.Connect(null, (Frame frame) =>
{
	Console.WriteLine("stomp connected:" + frame.Body);
	client.Subscribe(destincation, (Frame frame) =>
	 {
		 Console.WriteLine(destincation +" receive: " + frame.Body);
	 });
});
```
