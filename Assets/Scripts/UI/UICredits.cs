﻿using UnityEngine;
using System.Collections;

public class UICredits : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	    if (Input.GetMouseButtonDown(0))
        {
            UIManager.GetInstance().HideLastOpen();
        }
	}
}
