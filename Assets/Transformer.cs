// transforms an object: 
// - hold and drag primary mouse button to rotate the cube around its centre
// - hold and drag primary mouse button to rotate the cube around the camera (useful when inside the cube)
// - use arrow keys to translate the cube left/right and forward/backward
// - scroll wheel to scale the cube up/down
// adapted from various online sources
// Gilles Ferrand, University of Manitoba 2016

using UnityEngine;

public class Transformer : MonoBehaviour {
	
	// rotate
	public float  rotSpeed = 4.0f;
	bool isRotating;
	Vector3 rotationAxisX;
	Vector3 rotationAxisY;
	Vector3 mouseOrigin;
	Vector3 angleDelta;
	GameObject rotationCentre;

	// translate
	public float panSpeed = 2.0f;
	bool isMovingLeft, isMovingRight;
	bool isMovingUp, isMovingDown;
	bool isMovingForward, isMovingBackward;
	Vector3 translationAxis;

	// scale
	public float zoomSpeed = 1.0f;
	private float scaleMin = 0.01f;
	private float scaleMax = 100f;
	bool isScaling;
	float scale; // same for the 3 axes

	void Awake () {
	}

	void Start () {
		scale = Mathf.Clamp (this.transform.localScale[0], scaleMin, scaleMax);
	}

	void Update () {

		// rotate 

		if (Input.GetMouseButtonDown (0) || Input.GetMouseButtonDown (1)) { 
			isRotating = true;
			if (Input.GetMouseButtonDown (0)) rotationCentre = this.gameObject;
			if (Input.GetMouseButtonDown (1)) rotationCentre = Camera.main.gameObject;
			mouseOrigin = Input.mousePosition;
		}

		if (isRotating) {
			rotationAxisX = Camera.main.transform.up;
			rotationAxisY = Camera.main.transform.right;
			angleDelta = (Input.mousePosition - mouseOrigin)/Screen.width;
			angleDelta *= rotSpeed;
			angleDelta.x *= -1;
			this.transform.RotateAround (rotationCentre.transform.position, rotationAxisX, angleDelta.x);
			this.transform.RotateAround (rotationCentre.transform.position, rotationAxisY, angleDelta.y);
			if (!Input.GetMouseButton(0) && !Input.GetMouseButton(1)) isRotating = false;
		}

		// translate

		isMovingRight    = Input.GetKey (KeyCode.RightArrow);
		isMovingLeft     = Input.GetKey (KeyCode.LeftArrow);
		isMovingForward  = Input.GetKey (KeyCode.UpArrow);
		isMovingBackward = Input.GetKey (KeyCode.DownArrow);

		if (isMovingRight || isMovingLeft) {
			translationAxis = Camera.main.transform.right;
			float distance = panSpeed * Time.deltaTime;
			if (isMovingRight) this.transform.position += translationAxis * distance;
			if (isMovingLeft)  this.transform.position -= translationAxis * distance;
		}

		if (isMovingForward || isMovingBackward) {
			translationAxis = Camera.main.transform.forward;
			float distance = panSpeed * Time.deltaTime;
			if (isMovingForward)  this.transform.position += translationAxis * distance;
			if (isMovingBackward) this.transform.position -= translationAxis * distance;
		}

		// scale

		float scroll = Input.GetAxis ("Mouse ScrollWheel");
		isScaling = scroll != 0;

		if(isScaling) {
			scale *= 1 + scroll*zoomSpeed;
			scale = Mathf.Clamp(scale, scaleMin, scaleMax);
			this.transform.localScale = new Vector3(scale, scale, scale);
			if (scroll == 0) isScaling = false;
		}

	}
	
}
