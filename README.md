# Mimic
A free **open source** Socket-based networking library built around .NET Standard 2.1, providing a seamless integration into a wide range of C# projects. 
With Mimic, you can simplify and streamline network communication in your applications, effortlessly creating client-server architectures and exchanging data between connected devices.  

## Features
- **Socket Communication:** Mimic provides a simple socket API for establishing connections and transmitting data between networked devices.
- **Message-Based System:** Communicate with **`INetworkMessage`** objects to ensure a smooth and flawless translation of data between client and server.
- **Cross-Platform Compatibility:** Mimic is built on .NET Standard 2.1, ensuring compatibility across various platforms and frameworks, including .Net Core, .NET Framework, Xamarin, **Unity**, **Godot**, and more.  

## Architecture
The Server and Client have a very similar architecture with only very minor quality differences. 

Mimic already comes with a complete NetworkServer and NetworkClient class for a quick and easy start.

### Quick NetworkServer Example  
```cs
public class MyServerApplication
{
  NetworkServer server;
  int port = 4117;
  
  public void CreateServer()
  {
    // Create the server
    server = new NetworkServer(port);
    
    // Register the SendMessage NetworkMessage
    server.RegisterHandler<SendMessage>(OnMessageReceived);
  }
  
  // Trigger when we received a SendMessage NetworkMessage from the client
  void OnMessageReceived(SendMessage msg, IPEndPoint endPoint)
  {
    Console.WriteLine("Message Received from client: + msg.message);
    
    // Create a new SendMessage NetworkMessage which we will send to the client
    SendMessage sendMessage = new SendMessage("Hello Client! :D");
    server.Send(sendMessage, endPoint);
  }
}
```

### Quick NetworkClient Example  
```cs
public class MyClientApplication
{
  NetworkClient client;
  string serverAddress = "127.0.0.1";
  int port = 4117;
  
  public void CreateServer()
  {
    // Create the server
    client = new NetworkClient(serverAddress, port);
    
    // Register the SendMessage NetworkMessage
    client.RegisterHandler<SendMessage>(OnMessageReceived);
    
    // Create a new SendMessage NetworkMessage which we will send to the server
    SendMessage sendMessage = new SendMessage("Hello Server! ^^");
    client.Send(sendMessage);
  }
  
  // Trigger when we received a SendMessage NetworkMessage from the server
  void OnMessageReceived(SendMessage msg)
  {
    Console.WriteLine("Message Received from server: + msg.message);
  }
}
```
### Quick NetworkMessage Example  
```cs
public strict SendMessage : INetworkMessage
{
  public string message;
  
  public SendMessage(string message)
  {
    this.message = message;
  }
  
  public void Serialize(NetworkWriter writer)
  {
    writer.WriteString(message);
  }
  
  public void Deserialize(NetworkReader reader)
  {
    message = reader.ReadString();
  }
}
```

## Contributing
Contributions to Mimic are welcome! If you encounter any bugs, issues, or have feature requests, please open an issue on the **[Github Repository.](https://github.com/realQuartzi/Mimic/issues)**

## Credits & Thanks
This project was completely inspired by the #1 free open source game networking library for Unity **[Mirror](https://github.com/MirrorNetworking/Mirror)**.  
Without Mirror this little project would not have been possible!  
And there is a chance that you can communicate between Mimic and Mirror (untested)
