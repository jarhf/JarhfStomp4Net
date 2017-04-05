# JarhfStomp4Net
A C# stomp client, using for communication with spring websocket server.  
Spring websocket server must register STOMP over WebSocket endpoints, configures sockjs for processing HTTP requests from SockJS clients

  
# Usage
```
string uri = "http://localhost:8083/mywebsocket"; // uri also can be "ws://localhost:8083/mywebsocket"
StompClient client = new StompClient(uri);
string destincation = "/notification";
client.Connect(null, (Frame frame) =>
{
	Debug("stomp connected:" + frame.Body);
	client.Subscribe(destincation, (Frame frame) =>
	 {
		 Console.WriteLine(destincation +" receive: " + frame.Body);
	 });
});
```
