﻿using UnityEngine;
using System.Collections;

public class PlayBoardBehaviour : MonoBehaviour
{
    private Hexagon _previousHexagon;
    private HexagonBehaviour _previousHexagonBehaviour;
    private float _timerClic = 0.5f;
    private float _currentTime = 0;
    private bool _isInDoubleClicWindow = false;

    public Animator _musicAnimator;

    // Use this for initialization
    void Start () {
        RuntimeAnimatorController controller = Resources.Load<RuntimeAnimatorController>("sounds/MusicController");
        _musicAnimator = gameObject.AddComponent<Animator>();
        _musicAnimator.runtimeAnimatorController = controller;
	}

    void UpdateDoubleClic()
    {
        if(Input.GetMouseButtonDown(0))
        {
            _isInDoubleClicWindow = true;
        }

        if (_isInDoubleClicWindow)
        {
            _currentTime += Time.deltaTime;
            if (_currentTime > _timerClic)
            {
                _isInDoubleClicWindow = false;
                _currentTime = 0;
            }
        }

    }
	
	// Update is called once per frame
	void Update ()
    {
        if (ServerManager.GetInstance()._server != null && ServerManager.GetInstance()._server.CurrentState == Server.State.playing)
        {
            if (PlayBoardManager.GetInstance().CanEndTurn)
            {
                PlayBoardManager.GetInstance().CanEndTurn = false;
                ServerManager.GetInstance()._server.EndTurn();
            }
            
            HighLight();
            if (Input.GetMouseButtonDown(0)){
                if (PlayBoardManager.GetInstance().CurrentState == PlayBoardManager.State.SpellMode)
                {
                    if (_isInDoubleClicWindow)
                    {
                        MakeSpell();
                    }
                    else
                    {
                       // HighLight();
                    }
                }      
            }

            Character char1 = PlayBoardManager.GetInstance().Character1;
            Character char2 = PlayBoardManager.GetInstance().Character2;
            if ((char1._lifeCurrent <= 1250 && char1._lifeCurrent > 500) || (char2._lifeCurrent <= 1250 && char2._lifeCurrent > 500))
            {
                _musicAnimator.SetTrigger("BattleHalf");
            }
            if (char1._lifeCurrent <= 500 || char2._lifeCurrent <= 500)
            {
                _musicAnimator.SetTrigger("BattleEnd");
            }


            if (char1._lifeCurrent <= 0)
            {
                PlayBoardManager.GetInstance().Winner = PlayBoardManager.GetInstance().Character2;
                ServerManager.GetInstance()._server.CurrentState = Server.State.gameOver;
            }

            if (char2._lifeCurrent <= 0)
            {
                PlayBoardManager.GetInstance().Winner = PlayBoardManager.GetInstance().Character1;
                ServerManager.GetInstance()._server.CurrentState = Server.State.gameOver;
            }

            if (ServerManager.GetInstance()._server.CurrentState == Server.State.gameOver)
            {
                _musicAnimator.SetTrigger("Victory");
            }

            UpdateDoubleClic();
        }
        

    }

    public void HighLight()
    {
        Ray ray = CameraManager.GetInstance().Active.ScreenPointToRay(Input.mousePosition);
        RaycastHit rch;
        int layermask = LayerMask.GetMask("Hexagon");
        if (Physics.Raycast(ray, out rch, Mathf.Infinity, layermask))
        {
            HexagonBehaviour hexagonBehaviour = rch.collider.gameObject.GetComponent<HexagonBehaviour>();
            if (hexagonBehaviour != null)
            {
                Hexagon hexa = hexagonBehaviour._hexagon;
                if(hexa != null)
                {
                    if (_previousHexagon != null && _previousHexagonBehaviour != null)
                    {
                        if (_previousHexagon.CurrentState == Hexagon.State.OverAccessible)
                            _previousHexagon.CurrentState = _previousHexagon.PreviousState;

                        if ((_previousHexagon.CurrentState == Hexagon.State.OverSelfTargetable || _previousHexagon.CurrentState == Hexagon.State.OverEnnemiTargetable ||
                             _previousHexagon.CurrentState == Hexagon.State.Targetable) && _previousHexagonBehaviour.FinalArea != null)
                        {
                            for (int i = 0; i < _previousHexagonBehaviour.FinalArea.Count; i++)
                            {
                                if (_previousHexagonBehaviour.FinalArea[i].CurrentState == Hexagon.State.OverSelfTargetable || _previousHexagonBehaviour.FinalArea[i].CurrentState == Hexagon.State.OverEnnemiTargetable)
                                    _previousHexagonBehaviour.FinalArea[i].CurrentState = _previousHexagonBehaviour.FinalArea[i].PreviousState;
                            }
                        }
                    }

                    _previousHexagon = hexa;
                    _previousHexagonBehaviour = hexagonBehaviour;

                    //on mouse enter new hexa
                    if (hexa.CurrentState == Hexagon.State.Targetable)
                    {
                        hexagonBehaviour.MakeFinalArea();
                    }
                    if (hexa.CurrentState == Hexagon.State.Accessible)
                        hexa.CurrentState = Hexagon.State.OverAccessible;
                }
            }
        }
        else
        {
            if (_previousHexagon != null && _previousHexagon.CurrentState != Hexagon.State.Spawnable)
            {
                if ((_previousHexagon.CurrentState == Hexagon.State.OverSelfTargetable || _previousHexagon.CurrentState == Hexagon.State.OverEnnemiTargetable ||
                             _previousHexagon.CurrentState == Hexagon.State.Targetable) && _previousHexagonBehaviour.FinalArea != null)
                {
                    for (int i = 0; i < _previousHexagonBehaviour.FinalArea.Count; i++)
                    {
                        if (_previousHexagonBehaviour.FinalArea[i].CurrentState == Hexagon.State.OverSelfTargetable || _previousHexagonBehaviour.FinalArea[i].CurrentState == Hexagon.State.OverEnnemiTargetable)
                            _previousHexagonBehaviour.FinalArea[i].CurrentState = _previousHexagonBehaviour.FinalArea[i].PreviousState;
                    }
                }
                else
                    _previousHexagon.CurrentState = _previousHexagon.PreviousState;

                _previousHexagon = null;
            }
        }
    }

    public void MakeSpell()
    {
        Ray ray = CameraManager.GetInstance().Active.ScreenPointToRay(Input.mousePosition);
        //Debug.DrawLine(ray.origin, ray.direction * 20);
        RaycastHit rch;
        //int layermask = (1 << LayerMask.NameToLayer("Default"));
        int layermask = LayerMask.GetMask("Hexagon");
        if (Physics.Raycast(ray, out rch, Mathf.Infinity, layermask))
        {
            HexagonBehaviour hb = rch.collider.gameObject.GetComponent<HexagonBehaviour>();
            if (hb != null)
                MakeSpell(hb);
        }
    }

    public void MakeSpell(HexagonBehaviour hexagonBehaviour)
    {
        Hexagon hexa = hexagonBehaviour._hexagon;
        if (hexa != null && (hexa.CurrentState == Hexagon.State.OverEnnemiTargetable || hexa.CurrentState == Hexagon.State.OverSelfTargetable
            || hexa.CurrentState == Hexagon.State.Targetable))
        {
            if (hexagonBehaviour.FinalArea == null)
            {
                hexagonBehaviour.MakeFinalArea();
            }
            SpellManager.GetInstance().ApplyEffects(hexagonBehaviour.FinalArea, hexa);
            PlayBoardManager.GetInstance().Board.ResetBoard();
        }
    }
}
