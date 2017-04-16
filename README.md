# JarhfStomp4Net
A C# stomp client, using for communication with spring websocket server.  
spring websocket config below:
```
<websocket:message-broker application-destination-prefix="/app">
        <websocket:stomp-endpoint path="/mywebsocket" allowed-origins="*">
            <websocket:sockjs/>
        </websocket:stomp-endpoint>
        <websocket:simple-broker prefix="/notification"/>       
</websocket:message-broker>
```
  
# StompClient Usage
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
