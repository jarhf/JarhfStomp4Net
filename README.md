# JarhfStomp4Net
C#实现的stomp client和sockjs client，用来和spring websocket server通信.  

> 工作中用到websocket通信，服务器端用的是spring websocket。客户端有多种，其中包含C#版的桌面程序。  
但是网上C#版的客户端很少，找了一些比如 [StompClient](https://github.com/Code-Sharp/StompSharp),[StompNet](https://github.com/krlito/StompNet), [Aapche.NMS](http://activemq.apache.org/nms/)等都不符合要求无法使用。    
最接近可用的是 [DistSpringWebSocketClient-CSharp](https://github.com/DistChen/DistSpringWebsocketClient-CSharp)，不过他没有基于sockjs规范websocket client，导致消息格式不对，和spring websocket通信总是1007错误。    
好了，没办法，只好自己动手。  
参考了：https://github.com/jmesnil/stomp-websocket和https://github.com/sockjs/sockjs-client


服务器端spring websocket配置如下 :
```
<websocket:message-broker application-destination-prefix="/app">
        <websocket:stomp-endpoint path="/mywebsocket" allowed-origins="*">
            <websocket:sockjs/>
        </websocket:stomp-endpoint>
        <websocket:simple-broker prefix="/topic,/topic2"/>
	<!-- spring默认的是Jackson序列化，这里改为自己用fastjson序列化实现的MessageConverter -->
        <websocket:message-converters>
            <bean class="com.xkw.qbm.common.converter.FastJsonMessageConverter">
                <property name="fastJsonConfig">
                    <bean id="fastJsonConfig"
                          class="com.alibaba.fastjson.support.config.FastJsonConfig">
                        <property name="serializerFeatures">
                            <list>
                                <value>DisableCircularReferenceDetect</value>
                            </list>
                        </property>
                    </bean>
                </property>
            </bean>
        </websocket:message-converters>        
</websocket:message-broker>
```

# StompClient Usage
使用方法和stomp.js保持高度一致

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

# License
MIT
