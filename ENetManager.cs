using System;
using System.Runtime.InteropServices;
using System.Text;
using enet;
using UnityEngine;
using static enet.ENet;

public class ConnectionSettings {
    public readonly string HostName = "127.0.0.1";
    public readonly ushort Port = 7777;
}

public unsafe class ENetManager : MonoBehaviour {
    private ENetHost* clientHost;
    private ENetPeer* serverPeer;
    private readonly ConnectionSettings connectionSettings = new ConnectionSettings();
    private bool _enetInitialized;

    private void Awake() {
        if (enet_initialize() != 0) {
            Debug.LogError("ENet: Failed to initialize!");
            return;
        }

        _enetInitialized = true;
        Debug.Log("ENet: Initialized ENet library!");
        ConnectToEnet();
    }

    public void ConnectToEnet() {
        Debug.Log("ENet: Connecting to server...");

        ENetAddress localAddress = new ENetAddress();
        enet_address_set_host_ip(&localAddress, "0.0.0.0");
        localAddress.port = 0;

        clientHost = enet_host_create(&localAddress, 1, 2, 0, 0, ENetHostOption.ENET_HOSTOPT_IPV4);
        if (clientHost == null) {
            Debug.LogError("ENet: Failed to create client host");
            return;
        }

        ENetAddress address = new ENetAddress();
        if (enet_address_set_host_ip(&address, connectionSettings.HostName) != 0) {
            Debug.LogError("ENet: Failed to set host IP");
            return;
        }

        address.port = connectionSettings.Port;
        serverPeer = enet_host_connect(clientHost, &address, 2, 0);
        if (serverPeer == null) {
            Debug.LogError("ENet: Failed to create connection request");
            return;
        }

        Debug.Log($"ENet: Connection request sent to {connectionSettings.HostName}:{connectionSettings.Port}");
    }

    private void Update() {
        if (clientHost == null)
            return;

        ENetEvent netEvent = new ENetEvent();
        while (enet_host_service(clientHost, &netEvent, 0) > 0) {
            HandleEvent(ref netEvent);
        }
    }

    private void HandleEvent(ref ENetEvent netEvent) {
        switch (netEvent.type) {
            case ENetEventType.ENET_EVENT_TYPE_CONNECT:
                Debug.Log("ENet: Connected to server");
                break;

            case ENetEventType.ENET_EVENT_TYPE_RECEIVE:
                if (netEvent.packet == null)
                    break;

                int length = (int)netEvent.packet->dataLength;
                if (length <= 0) {
                    enet_packet_destroy(netEvent.packet);
                    break;
                }

                byte[] packetData = new byte[length];
                Marshal.Copy((IntPtr)netEvent.packet->data, packetData, 0, length);

                string msg = StripTrailingPacketData(packetData);
                Debug.Log($"ENet: Packet received. -> Channel: {netEvent.channelID} Data: {msg}");
                enet_packet_destroy(netEvent.packet);
                break;

            case ENetEventType.ENET_EVENT_TYPE_DISCONNECT:
                Debug.Log("ENet: Disconnected");
                break;

            case ENetEventType.ENET_EVENT_TYPE_NONE:
                break;
        }
    }

    private void OnDestroy() {
        if (clientHost != null) {
            enet_host_destroy(clientHost);
            clientHost = null;
        }

        if (_enetInitialized) {
            enet_deinitialize();
            _enetInitialized = false;
        }
    }

    private string StripTrailingPacketData(byte[] packetData) => Encoding.UTF8.GetString(packetData).TrimEnd('\0');

    private byte[] StringToBytes(string msg) => Encoding.UTF8.GetBytes(msg);
}
