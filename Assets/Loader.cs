// loads the raw binary data into a texture saved as a Unity asset 
// (so can be de-activated after a given data cube has been converted)
// adapted from a XNA project by Kyle Hayward 
// http://graphicsrunner.blogspot.ca/2009/01/volume-rendering-101.html
// data can be UInt8 or Float32, expected to be normalized in [0, 1]
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
	public enum Format {Uint8, Float32};
	public Format format;

	void Start() {
		Color[] colors = new Color[0];
		TextureFormat textureformat = new TextureFormat();
		Vector4 channels = new Vector4();
		string suffix = "";
		// load the raw data
		switch (format) {
		case(Format.Uint8):
			colors = LoadRAWUint8File ();
			textureformat = TextureFormat.Alpha8;
			channels = new Vector4 (0, 0, 0, 1);
			suffix = "A8";
			break;
		case(Format.Float32):
			colors = LoadRAWFloat32File ();
			textureformat = TextureFormat.RFloat;
			channels = new Vector4 (1, 0, 0, 0);
			suffix = "R32";
			break;
		}
		// create the texture
		Texture3D texture = new Texture3D (size[0], size[1], size[2], textureformat, mipmap);
		texture.SetPixels (colors);
		texture.Apply ();
		// assign it to the material of the parent object
		try {
			Material material = GetComponent<Renderer> ().material;
			material.SetTexture ("_Data", texture);
			material.SetVector ("_DataChannel", channels);
		}
		catch  { 
			Debug.Log ("Cannot attach the texture to the parent object");
		}
		// save it as an asset for re-use
		#if UNITY_EDITOR
		AssetDatabase.CreateAsset(texture, path+filename+"-"+suffix+".asset");
		#endif
	}

	private Color[] LoadRAWUint8File()
	{
		Color[] colors; // NB: data value goes into A channel only

		Debug.Log ("Opening file " + path + filename + extension);
		FileStream file = new FileStream (path + filename + extension, FileMode.Open);
		Debug.Log ("File length = " + file.Length + " bytes, Data size = " + size [0] * size [1] * size [2] + " points -> " + file.Length / (size [0] * size [1] * size [2]) + " byte(s) per point");

		BinaryReader reader = new BinaryReader (file);
		byte[] buffer = new byte[size [0] * size [1] * size [2]];
		reader.Read (buffer, 0, sizeof(byte) * buffer.Length);
		reader.Close ();

		colors = new Color[buffer.Length];
		Color color = Color.black;
		for (int i = 0; i < buffer.Length; i++) {
			color = Color.black;
			color.a = (float)buffer [i] / byte.MaxValue;
			colors [i] = color;
		}

		return colors;
	}

	private Color[] LoadRAWFloat32File()
	{
		Color[] colors; // NB: data value goes into R channel only

		Debug.Log ("Opening file "+path+filename+extension);
		FileStream file = new FileStream(path+filename+extension, FileMode.Open);
		Debug.Log ("File length = "+file.Length+" bytes, Data size = "+size[0]*size[1]*size[2]+" points -> "+file.Length/(size[0]*size[1]*size[2])+" byte(s) per point");

		BinaryReader reader = new BinaryReader(file);
		float[] buffer = new float[size[0] * size[1] * size[2]];
		for (int i = 0; i < buffer.Length; i++) {
			buffer [i] = reader.ReadSingle ();
		}
		reader.Close();

		colors = new Color[buffer.Length];
		Color color = Color.black;
		for (int i = 0; i < buffer.Length; i++) {
			color = Color.black;
			color.r = (float)buffer[i];
			colors [i] = color;
		}

		return colors;
	}

}
