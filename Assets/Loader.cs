// loads the raw binary data into a texture saved as a Unity asset 
// (so can be de-activated after a given data cube has been converted)
// adapted from a XNA project by Kyle Hayward 
// http://graphicsrunner.blogspot.ca/2009/01/volume-rendering-101.html
// Gilles Ferrand, University of Manitoba / RIKEN, 2016–2017

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.IO; // to get BinaryReader
using System.Linq; // to get array's Min/Max

public class Loader : MonoBehaviour {

	public string path = @"Assets/";
	public string filename = "skull";
	public string extension = ".raw";
	public int[] size = new int[3] {256, 256, 256};
	public bool mipmap;

	void Start() {
		// load the raw data
		Color[] colors = LoadRAWFile();
		// create the texture
		Texture3D texture = new Texture3D (size[0], size[1], size[2], TextureFormat.Alpha8, mipmap);
		texture.SetPixels (colors);
		texture.Apply ();
		// assign it to the material of the parent object
		GetComponent<Renderer>().material.SetTexture ("_Data", texture);
		// save it as an asset for re-use
		#if UNITY_EDITOR
		AssetDatabase.CreateAsset(texture, path+filename+".asset");
		#endif
	}

	private Color[] LoadRAWFile()
	{
		Color[] colors;

		Debug.Log ("Opening file "+path+filename+extension);
		FileStream file = new FileStream(path+filename+extension, FileMode.Open);
		Debug.Log ("File length = "+file.Length+" bytes, Data size = "+size[0]*size[1]*size[2]+" points -> "+file.Length/(size[0]*size[1]*size[2])+" byte(s) per point");

		BinaryReader reader = new BinaryReader(file);
		byte[] buffer = new byte[size[0] * size[1] * size[2]]; // assumes 8-bit data
		reader.Read(buffer, 0, sizeof(byte) * buffer.Length);
		reader.Close();

		colors = new Color[buffer.Length];
		Color color = Color.black;
		for (int i = 0; i < buffer.Length; i++)
		{
			color.a = (float)buffer[i] / byte.MaxValue; //scale the scalar values to [0, 1]
			colors [i] = color;
		}

		return colors;
	}

}
