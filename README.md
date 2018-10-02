# Terrain Splatmap Profile
This package allows you set splatmaps (aka masks, alphamaps, weightmaps) using independently generated 
alphamaps. That is, instead having to generate a set of RGBA splatmaps, where each color channel
corresponds to the blending values for one of your terrain layers, you instead provide 1 alphamap
for each terrain layer and then this package combines them into the splatmap layers needed
by Unity's terrain system.

The original use case is to import alphamaps generated in programs like like World Machine or Houdini. While
these programs can generate RGBA splatmaps directly, you have to keep track of each channel each of your
terrain layer's alphamaps is in. With this tool, you can associate an alphamap with a terrain layer, and
let the tool worry about keeping the alphamap with the correct terrain layer.

## Requirements

* Unity 2018.3.0b3 or later.
* High Definition Render Pipeline 4.0.1-preview or later.

## Package Manager Installation
Open the Package Manager and press the + button on the bottom of the window to *Add package from disk...*.

## Use
In Project view, right click and select *Create > Terrain Splatmap Profile*.
