# Unity-RayTracing
Demo of volume rendering a data cube in Unity by performing ray tracing on the GPU

The Loader script reads a raw binary file as a 3D texture, assigns the texture to the VolumetricData material of the Data Cube object, and saves it as an asset for re-use.
The RayCaster shader performs volume rendering on the GPU, by casting rays inside the cube and sampling the 3D texture along each ray. It includes slicing and thresholding of the data cube.
The Transformer script provides basic runtime interaction using a mouse and keyboard: rotate, translate, and scale the cube. 
