using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class LightingTest : MonoBehaviour {
	public float speed = 5;
	public bool pause = true;
	public bool reset = false;
	void Awake()
	{
		runInEditMode = true;
	}

	// Update is called once per frame
	void Update () {
		if (!pause)
		{
			transform.Rotate(Vector3.up, speed * Time.deltaTime, Space.World);

		}
		if (reset)
		{
			pause = true;
			transform.rotation = Quaternion.Euler(35, 0, 0);
			reset = false;
		}
	}
}
