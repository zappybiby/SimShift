SimShift
========

Driver utilities for various open roaming simulators, like ETS2.

## SCS Extractor: http://download.eurotrucksimulator2.com/scs_extractor.zip
- Guide here: http://forum.scssoft.com/viewtopic.php?f=172&t=190685

### Basically:
1. Place SCS Extractor in ETS2 Main Folder
2. Make a shortcut for it
3. Right click on shortcut -> Properties
4. Target: `"..\scs_extractor.exe" (FILE).scs "C:\Users\<your user name>\Documents\Extracted ETS2\FILE"`

#### Or Use [SCS EXTRACTOR GUI](https://github.com/Bluscream/SCS-Extractor-GUI)

## Files That Need to Be Extracted
1. Raw map information. This is located in base.scs at base/map/europe/ (or within a mod). Use the SCS extractor to extract base.scs and extract the map data. 
- Put all *.base files in `steam\steamapps\common\Euro Truck Simulator 2\base`

2. Prefab information. These are also located in the base.scs. Extract this file as well with the SCS extractor, and locate the base/prefab/ folder. Put all *.ppd files in SCS/prefab. There are some duplicates; just ignore these because this has not been supported yet.

3. 2 SII def files; these are located in def.scs. Extract this file and locate def/world/road_look.sii and def/world/prefab.sii. Put them in the SII prefab location, and add the "LUT1.19-" to them.
