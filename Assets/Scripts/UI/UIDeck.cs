﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class UIDeck : MonoBehaviour {

    public DeckSelectionBehaviour _deck;
    public InputField _inputField;

    void OnGUI()
    {
        if(_inputField.isFocused && Input.GetKeyDown(KeyCode.Return))
        {
            SaveHandAndPlay();
        }
    }

	public void SaveHandAndPlay()
    {
        if(_deck.DeckSelection.RunesInHand.Count == 7)
        {
            DeckSelection ds = _deck.DeckSelection;
            RunicBoardManager.GetInstance().TempHand = ds.RunesInHand;
            if (_inputField.text.Length == 0)
            {
                _inputField.text = "Anonyme";
            }
            ClientManager.GetInstance()._client.Name = _inputField.text;

            UIManager.GetInstance().HidePanelNoStack("PanelRunicBoard");
            //UIManager.GetInstance().ShowPanelNoStack("PanelServers");

            UIManager.GetInstance().ShowPanelNoStack("PanelWaitingMap");
            UIManager.GetInstance().HidePanelNoStack("menuButton");
            UIManager.GetInstance().ShowPanelNoStack("menuButton");
            _deck.gameObject.SetActive(false);
            AudioManager.GetInstance().Play("validate");
            ClientManager.GetInstance()._client._deckValidated = true;
            ClientManager.GetInstance()._client.SearchHost();
            
        }
        else
        {
            AudioManager.GetInstance().Play("error");
            Logger.Debug("Not enough runes : " + _deck.DeckSelection.RunesInHand.Count);
        }
    }
}
