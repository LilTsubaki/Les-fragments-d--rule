﻿using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System;
using System.Threading;

public class Server : MonoBehaviour{

    public enum State {turningAroundMap, firstPlayerPicking, secondPlayerPicking, playing, gameOver }

    public float timeout=300;
    public float currrentTimeout;

    public int broadcastPort;
    public int playPort;

    TcpListener _listener;
    List<ServerListener> _clients;

    ServerListener _client1;
    ServerListener _client2;

    UdpClient _udpClient;

    bool _isRunning = false;

    bool _searchingClient = false;

    private State _currentState;

    public State CurrentState
    {
        get
        {
            return _currentState;
        }

        set
        {
            _currentState = value;
        }
    }

    public ServerListener Client1
    {
        get
        {
            return _client1;
        }

        set
        {
            _client1 = value;
        }
    }

    public ServerListener Client2
    {
        get
        {
            return _client2;
        }

        set
        {
            _client2 = value;
        }
    }

    public bool SearchingClient
    {
        get
        {
            return _searchingClient;
        }

        set
        {
            _searchingClient = value;
        }
    }

    public void Awake()
    {
        //Logger.logLvl = Logger.Type.TRACE;
        currrentTimeout = timeout;
        ServerManager.GetInstance().Init(this);
        _clients = new List<ServerListener>();
        
        _udpClient = new UdpClient(broadcastPort);
		_udpClient.EnableBroadcast = true;

        _searchingClient = true;
		_isRunning = true;
        
		StartListening(playPort);
        Thread newThread =new Thread(WaitingClient);
        newThread.Start();


    }

    public void OnDestroy()
    {
        _isRunning = false;

        _searchingClient = false;

        _udpClient.Close();


        /*foreach(var cli in _clients)
        {
            cli.Stop();
        }*/

        _listener.Stop();
    }


    void FixedUpdate()
	{
        switch(_currentState)
        {
            case State.playing:
                currrentTimeout -= Time.fixedDeltaTime;
                if (currrentTimeout <= 0)
                {
                    ServerManager.GetInstance()._server.EndTurn();
                }
                break;
            default:
                break;
        }
	}

	public void WaitingClient(){
		while (_isRunning && _searchingClient) {
			if(_udpClient.Available>=4){
				
				IPEndPoint ip=new IPEndPoint(IPAddress.Any, broadcastPort);
				byte[] data =_udpClient.Receive(ref ip);

				if (BitConverter.IsLittleEndian)
					Array.Reverse(data);

				int id = BitConverter.ToInt32(data, 0);

				if (id == 0){
					Logger.Warning("id: "+id);

				


					byte[] data2;
					data2 = BitConverter.GetBytes(1);
					if (BitConverter.IsLittleEndian)
						Array.Reverse(data2);

					_udpClient.Send(data2, data.Length, ip);
				}
			}
		}
        _udpClient.Close();
      }

    public bool Register(ServerListener serverListener)
    {
        if (_client1 == null)
        {
            _client1 = serverListener;
            return true;
        }

        if (_client2 == null)
        {
            _client2 = serverListener;
            _searchingClient = false;
            CleanClients();
            return true;
        }

        return false;
    }

    public void SendCharacter()
    {
        _client1._character = PlayBoardManager.GetInstance().Character1;
        _client1._character.Name = _client1._name;
        _client1.SendCharacter();
        _client2._character = PlayBoardManager.GetInstance().Character2;
        _client2._character.Name = _client2._name;
        _client2.SendCharacter();
    }

    public bool SetCharacters(ServerListener serverListener)
    {
        if (_client1 == serverListener)
        {
            _client1._character = PlayBoardManager.GetInstance().Character1;
            return true;
        }

        if (_client2 == serverListener)
        {
            _client2._character = PlayBoardManager.GetInstance().Character2;
            return true;
        }

        return false;
    }

    public void CleanClients()
    {
        foreach (var client in _clients)
        {
            if (client != _client1 && client != _client2)
            {
                client.Stop();
                _clients.Remove(client);
            }
        }
    }

    private void StartListening(int port)
    {
        try
        {
			_listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            //StartCoroutine("Listen");
            Thread newThread = new Thread(Listen);
            newThread.Start();
        }
        catch
        { }
    }

    public TcpClient ListenConnections()
    {
        try
        {
            if (_listener.Pending())
            {
                TcpClient client = _listener.AcceptTcpClient();
                return client;
            }
        }
        catch
        { }
        return null;
    }

    public void Stop()
    {
        _listener.Stop();
        _isRunning = false;
    }

    void Listen()
    {
		while (_isRunning && _searchingClient)
        {
            TcpClient client=ListenConnections();
            if (client != null)
            {
                Logger.Trace("Client found");
                //StartCoroutine("ListenClient", client);
                ServerListener listener = new ServerListener(this, client);
                _clients.Add(listener);
                Thread newThread = new Thread(listener.ListenClient);
                newThread.Start();

                
            }
                
        }
    }

    public void RemoveClient(ServerListener client)
    {
        _clients.Remove(client);
    }

    public void EndTurn()
    {
        PlayBoardManager.GetInstance().EndTurn();
        currrentTimeout = timeout;

        if (_client1!=null)
            _client1.EndTurn();
        if (_client2 != null)
            _client2.EndTurn();
    }

    public void UpdateCharacter(Character character)
    {
        if(_client1 != null && _client1._character == character)
        {
            _client1.UpdateCharacter();
            return;
        }

        if (_client2 !=null && _client2._character == character )
        {
            _client2.UpdateCharacter();
            return;
        }
    }

    public void ApplyEffects(Character character, bool success, bool crit, int runes)
    {
        if (_client1 != null)
        {
            _client1.UpdateCharacter();
            if (_client1._character == character)
            {
                _client1.ResetBoard(success, crit, runes);
            }
        }

        if (_client2 != null)
        {
            _client2.UpdateCharacter();
            if (_client2._character == character)
            {
                _client2.ResetBoard(success, crit, runes);
            }
        }
    }

    public void RestartGame()
    {
        if (_client1 != null)
        {
            _client1.RestartGame();
        }
        if (_client2 != null)
        {
            _client2.RestartGame();
        }
        AudioManager.GetInstance().Play("choixMap");
        ManagerManager.GetInstance().ResetAll();
        System.GC.Collect();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void EndGame(Character winner)
    {
        if (_client1 != null)
        {
            _client1.EndGame(winner);
        }
        if (_client2 != null)
        {
            _client2.EndGame(winner);
        }
    }

    public void EndTurnOnNextUpdate()
    {
        PlayBoardManager.GetInstance().CanEndTurn = true;
    }
}
