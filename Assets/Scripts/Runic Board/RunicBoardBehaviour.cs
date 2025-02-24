﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RunicBoardBehaviour : MonoBehaviour {
    private RunicBoard _board;

    public GameObject _handGO;
    public GameObject _boardGO;

    public GameObject _airRuneAsset;
    public GameObject _earthRuneAsset;
    public GameObject _fireRuneAsset;
    public GameObject _metalRuneAsset;
    public GameObject _woodRuneAsset;
    public GameObject _waterRuneAsset;

    private GameObject _heldRune;

    private List<GameObject> _runesGO;

    private SendBoardResponse _latestSendBoardResponse;

    public RunicBoard Board
    {
        get
        {
            return _board;
        }

        set
        {
            _board = value;
        }
    }

    public SendBoardResponse LatestSendBoardResponse
    {
        get
        {
            return _latestSendBoardResponse;
        }

        set
        {
            _latestSendBoardResponse = value;
        }
    }




    /// <summary>
    /// Iinstatiante runes game object and display them in the hand
    /// </summary>
    private void InstantiateRunesInHand()
    {
        foreach(KeyValuePair<int, Rune> kvp in Board.RunesInHand)
        {
            Transform parent = _handGO.transform.GetChild((int)kvp.Key);
            GameObject rune = new GameObject();

            switch (kvp.Value.Element._name)
            {
                case "Fire":
                    rune = GameObject.Instantiate(_fireRuneAsset);
                    break;
                case "Water":
                    rune = GameObject.Instantiate(_waterRuneAsset);
                    break;
                case "Air":
                    rune = GameObject.Instantiate(_airRuneAsset);
                    break;
                case "Earth":
                    rune = GameObject.Instantiate(_earthRuneAsset);
                    break;
                case "Wood":
                    rune = GameObject.Instantiate(_woodRuneAsset);
                    break;
                case "Metal":
                    rune = GameObject.Instantiate(_metalRuneAsset);
                    break;
                default:
                    break;
            }

            _runesGO.Add(rune);

            // Set transformation
            rune.GetComponent<RuneBehaviour>()._initialParent = parent;
            rune.transform.SetParent(parent);
            rune.transform.localPosition = new Vector3(0, 0.3f, 0);

            // Associate Rune object and gameObject
            rune.GetComponent<RuneBehaviour>()._rune = kvp.Value;
        }
    }

    /// <summary>
    /// Remove all runes from the board, and put them back in the hand of the player
    /// </summary>
    /// 
    public void ResetRunes()
    {
        _board.RemoveAllRunes();
        for (int i = 0; i < _runesGO.Count; i++)
        {
            RuneBehaviour rb = _runesGO[i].GetComponent<RuneBehaviour>();
            _runesGO[i].transform.SetParent(rb._initialParent);
            rb._state = RuneBehaviour.State.BeingReleased;
        }
    }
    public void ResetRunes(int runeKept)
    {
        switch (runeKept)
        {
            case 0:
                ResetRunes();
                break;
            case 1:
                //RunicBoardManager.GetInstance().GetBoardPlayer1().RemoveAllRunesExceptHistory(true);
                ResetRunesExceptHistory(true);// 
                break;
            case 2:
                //RunicBoardManager.GetInstance().GetBoardPlayer1().RemoveAllRunesExceptHistory(false);
                ResetRunesExceptHistory(false); //
                break;
        }
    }

    public void ResetRunesExceptHistory(bool ignoreSecond)
    {
        _board.RemoveAllRunesExceptHistory(ignoreSecond);
        for(int i = 0; i < _runesGO.Count; ++i)
        {
            RuneBehaviour rb = _runesGO[i].GetComponent<RuneBehaviour>();
            if(rb._rune.PositionOnBoard == -1)
            {
                _runesGO[i].transform.SetParent(rb._initialParent);
                rb._state = RuneBehaviour.State.BeingReleased;
            }
        }
    }

    void InputUpdate()
    {
        // If mouse is pressed, check if a rune is underneath. If a rune is found, put his gameObject in _heldRune.
        if (Input.GetMouseButtonDown(0))
        {
            Ray camRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hitInfo;
            if(Physics.Raycast(camRay, out hitInfo, Mathf.Infinity, LayerMask.GetMask("Runes")))
            {
                _heldRune = hitInfo.collider.gameObject;
                RuneBehaviour runeBehaviour = hitInfo.collider.gameObject.GetComponent<RuneBehaviour>();
                if (runeBehaviour != null)
                {
                    runeBehaviour._state = RuneBehaviour.State.BeingTaken;
                }
            }
        }

        if (Input.GetMouseButtonUp(0) && _heldRune != null)
        {
            Ray camRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hitInfo;

            RuneBehaviour runeBehaviour = _heldRune.GetComponent<RuneBehaviour>();
            
            if (Physics.Raycast(camRay, out hitInfo, Mathf.Infinity, LayerMask.GetMask("Runes Slot")) && !ClientManager.GetInstance()._client.LockedMode)
            {
                    RuneSlotBehaviour runeSlotBehaviour = hitInfo.collider.gameObject.GetComponent<RuneSlotBehaviour>();
                if (runeSlotBehaviour != null)
                {
                    Rune rune = _heldRune.GetComponent<RuneBehaviour>()._rune;
                    int slotPosition = runeSlotBehaviour._position;
                    // Rune is currently on board
                    if (rune.IsOnBoard())
                    {
                        // Rune is released on board
                        if(runeSlotBehaviour.isBoard)
                        {
                            Logger.Debug("BOARD");
                            if (Board.ChangeRunePosition(rune.PositionOnBoard, slotPosition))
                            {
                                _heldRune.transform.SetParent(hitInfo.collider.transform);
                            }
                        }
                        // Rune is released in hand
                        else
                        {                            
                            Logger.Debug("HAND");
                            if (_board.RemoveRuneFromBoard(rune.PositionOnBoard))
                            {
                                Transform hand = hitInfo.collider.transform.parent;
                                Transform slot = hand.GetChild(rune.PositionInHand);
                                _heldRune.transform.SetParent(slot);
                                LatestSendBoardResponse = ClientManager.GetInstance()._client.SendBoard(false);
                            }
                        }
                    }
                    // Rune is currently in hand
                    else if (runeSlotBehaviour.isBoard)
                    {
                        int newPositionOnBoard = Board.PlaceRuneOnBoard(rune.PositionInHand, slotPosition);
                        if (newPositionOnBoard >= 0)
                        {
                             LatestSendBoardResponse =  ClientManager.GetInstance()._client.SendBoard();
                             if(LatestSendBoardResponse._exist)
                             {
                                 Transform parent;
                                 if (newPositionOnBoard == 12)
                                 {
                                     parent = _boardGO.transform.GetChild(9).transform;
                                 }
                                 else
                                 {
                                     parent = hitInfo.collider.transform;
                                 }
                                 _heldRune.transform.SetParent(parent);
                             }
                             else
                             {
                                 Board.RemoveRuneFromBoard(slotPosition, false);
                             }
                        }
                    }
                }
            }
            
            runeBehaviour._state = RuneBehaviour.State.BeingReleased;
            _heldRune = null;
        }
    }

    void Awake()
    {
        _runesGO = new List<GameObject>();
        Dictionary<int, Rune> hand = RunicBoardManager.GetInstance().TempHand;

        Logger.Debug("Awake Runic Board Behaviour");

        Board = new RunicBoard(hand);
        RunicBoardManager.GetInstance().RegisterBoard(Board);
        RunicBoardManager.GetInstance().RegisterBoardBehaviour(this);

        InstantiateRunesInHand();
    }

	// Use this for initialization
	void Start () {
	    
	}
	
	// Update is called once per frame
	void Update ()
    {
        if(!ClientManager.GetInstance()._client.GameOver)
        {
            InputUpdate();
        }
    }
}
