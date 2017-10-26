# generates mock 3D data cubes for loading into Unity
# Gilles Ferrand, RIKEN, 2017

import numpy as np
import scipy.interpolate

def axes(N_bins=256, formats=[8]):
	""" generates coordinate axes at the centre of the cube """
	cube = np.zeros((N_bins,N_bins,N_bins))
	half = int(N_bins/2)
	radius = 2
	cube[half:int(half*(1+3/3.))+1,half-radius:half+radius+1,half-radius:half+radius+1] = 1 # long   segment along 1st dimension
	cube[half-radius:half+radius+1,half:int(half*(1+2/3.))+1,half-radius:half+radius+1] = 1 # medium segment along 2nd dimension
	cube[half-radius:half+radius+1,half-radius:half+radius+1,half:int(half*(1+1/3.))+1] = 1 # short  segment along 3rd dimension
	for format in formats: tofile(cube, name='cube_axes', format=format, T=(3,2,1))

def random(N_bins=256, N_dots=None, rand_power=0, rand_min=0, rand_max=1, gauss_centre=0.5, gauss_width=0, types=["sparse","dense"], formats=[8]):
	""" generate dots at random locations inside the cube """
	if N_dots==None: N_dots = N_bins**2
	# set random coordinates
	x = np.random.rand(N_dots) * N_bins
	y = np.random.rand(N_dots) * N_bins
	z = np.random.rand(N_dots) * N_bins
	# set data
	d = np.random.rand(N_dots)**rand_power
	d = rand_min + d * (rand_max-rand_min)
	if gauss_width>0: # Gaussian distribution in space
		gauss_centre *= N_bins
		gauss_width *= N_bins
		d *= np.exp(-((x-gauss_centre)/gauss_width)**2-((y-gauss_centre)/gauss_width)**2-((z-gauss_centre)/gauss_width)**2)
	# put dots inside a regular cube
	if "sparse" in types:
		cube   = np.zeros((N_bins,N_bins,N_bins))
		weight = np.zeros((N_bins,N_bins,N_bins))
		for i in range(N_dots):
			cube  [int(x[i]),int(y[i]),int(z[i])] += d[i]
			weight[int(x[i]),int(y[i]),int(z[i])] += 1.
		i_dots = np.where(weight>=1)
		cube[i_dots] /= weight[i_dots]
		cube /= cube.max()
		print "%i dots put in the cube at %i locations"%(N_dots,len(i_dots[0]))
		for format in formats: tofile(cube, name='cube_random_sparse', format=format, T=(3,2,1))
	# interpolate the scattered data
	if "dense" in types:
		print "interpolating values on a regular grid"
		t = np.linspace(0, N_bins, N_bins)
		X, Y, Z = np.meshgrid(t,t,t)
		D = scipy.interpolate.griddata((x,y,z), d, (X,Y,Z), method='linear', fill_value=0)
		if np.isnan(D).any(): print "! interpolation failed"
		for format in formats: tofile(D, name='cube_random_dense', format=format, T=(3,2,1))
	
def tofile(cube_norm, name='cube', format=8, T=(3,2,1)):
	""" writes a data cube in an 8-bit binary file """
	# the data are expected to be normalized in [0,1]
	if format==8: 
		cube_fmt = (cube_norm*255).astype('uint8')
	elif format==32: 
		cube_fmt = cube_norm.astype('float32')
	else:
		print "unsupported format"
		return
	filename = '%s_T%i%i%i_%ix%ix%i.bin%i'%(name,T[0],T[1],T[2],cube_norm.shape[0],cube_norm.shape[1],cube_norm.shape[2],format)
	print "writing "+filename
	cube_fmt.transpose(T[0]-1,T[1]-1,T[2]-1).tofile(filename)
	# note: data is always written in C order by tofile()
	# transpose(3,2,1) is needed so that cube dimensions 1,2,3 map to Unity axes x,y,z (left -handed coordinate system)
	# transpose(2,3,1) is needed so that cube dimensions 1,2,3 map to Unity axes x,z,y (right-handed coordinate system)

def examples(N_bins=256, formats=[8]):
	# coordinate system
	axes(N_bins=N_bins)
	# dots with Gaussian spatial distribution
	random(N_bins=N_bins,rand_power=0,rand_max=1.0,gauss_width=0.0,types=["sparse"],formats=formats)
	# cube filled with spongy texture
	random(N_bins=N_bins,rand_power=2,rand_max=0.5,gauss_width=0.0,types=["dense" ],formats=formats)
	
if __name__ == "__main__":
	examples()
