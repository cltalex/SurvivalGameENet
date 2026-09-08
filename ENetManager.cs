using UnityEngine;
using ENet;

public class ConnectionSettings {
    public readonly string HostName = "127.0.0.1";
    public readonly ushort Port = 7777;
}

public class ENetManager : MonoBehaviour {
    private Host client;
    private Peer serverPeer;
    private ConnectionSettings connectionSettings = new ConnectionSettings();
    private bool _enetInitialized;

    private void Awake() {
        if (!Library.Initialize()) {
            Debug.LogError("ENet: Failed to initialize!");
            return;
        }
        _enetInitialized = true;
        Debug.Log("ENet: Initialized!");
        ConnectToEnet();
    }

    private void ConnectToEnet() {
        Debug.Log("ENet: Connecting to server...");
        client = new Host();
        client.Create(1, 2);

        Address address = new();
        if (!address.SetHost(connectionSettings.HostName)) {
            Debug.LogError("ENet: Failed to set Host");
            return;
        }
        address.Port = connectionSettings.Port;
        serverPeer = client.Connect(address, 2);
    }

    private void Update() {
        if (client == null)
            return;

        ENet.Event netEvent;

        while (client.CheckEvents(out netEvent) > 0) {
            HandleEvent(netEvent);
        }
        while (client.Service(0, out netEvent) > 0) {
            HandleEvent(netEvent);
        }
    }

    void HandleEvent(ENet.Event netEvent) {
        switch (netEvent.Type) {
            case ENet.EventType.Connect:
                Debug.Log("ENet: Connected to server");
                break;
            case ENet.EventType.Receive:
                byte[] packetData = new byte[netEvent.Packet.Length];
                netEvent.Packet.CopyTo(packetData);
                string msg = StripTrailingPacketData(packetData);
                Debug.Log($"ENet: Packet received. -> Channel: {netEvent.ChannelID} Data: {msg}");
                netEvent.Packet.Dispose();
                break;
            case ENet.EventType.Disconnect:
                Debug.Log("ENet: Disconnected");
                break;
            case ENet.EventType.Timeout:
                Debug.Log("ENet: Timed out");
                break;
        }
    }

    private void OnDestroy() {
        client?.Flush();
        client?.Dispose();
        if (_enetInitialized) {
            Library.Deinitialize();
            _enetInitialized = false;
        }
    }

    private string StripTrailingPacketData(byte[] packetData) => System.Text.Encoding.UTF8.GetString(packetData).TrimEnd('\0');

    private byte[] StringToBytes(string msg) => System.Text.Encoding.UTF8.GetBytes(msg);
}