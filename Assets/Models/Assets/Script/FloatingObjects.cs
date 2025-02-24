using UnityEngine;
using System.Collections;


public class FloatingObjects : MonoBehaviour {

	public GameObject Object;
			
	private float rot,tilt,lift = 0.0f;
	[Range(-4.0f,4.0f)]public float rotSpeed = 0.05f;
	[Range(-4.0f,4.0f)]public float tiltSpeed = 0.5f;
	[Range(-0.1f,0.1f)]public float liftSpeed = 0.012f;
	[Range(-10.0f,10.0f)]public float revolve = 1.1f;
			
	void FixedUpdate()
	{	
		rot += rotSpeed * revolve;
		tilt = Mathf.Sin (Time.time) * tiltSpeed;
		lift = Mathf.Sin (Time.time) * liftSpeed;
				
		Object.transform.eulerAngles = new Vector3 (0.0f,rot,tilt);	// replace the Y coordinates by rot
		Object.transform.Translate(0.0f,lift,0.0f, Space.World);
		Object.transform.RotateAround(Vector3.zero, Vector3.up, revolve);
		//transform.LookAt(Vector3.zero);
	}

}