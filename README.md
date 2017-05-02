SimShift
========

Driver utilities for various open roaming simulators, like ETS2.

Add Binaries folder to Reference Paths

ETS2SDK: Obtain from https://github.com/nlhans/ets2-map/tree/master/Binaries
Place in Binaries Folder


SCS Extractor: http://download.eurotrucksimulator2.com/scs_extractor.zip

Raw map information. This is located in base.scs at base/map/europe/ (or within a mod). Use the SCS extractor to extract base.scs and extract the map data. 
- Put all *.base files in SimShift\bin\Debug\europe

Prefab information. These are also located in the base.scs. Extract this file as well with the SCS extractor, and locate the base/prefab/ folder. Put all *.ppd files in SCS/prefab. There are some duplicates; just ignore these because this has not been supported yet.
 - TODO: prefabFolder in Ets2Mapper.cs looks in "E\Mods\ETS2\data 1.19\base\prefab" for prefab files. Need to change this to support all computers.

2 SII def files; these are located in def.scs. Extract this file and locate def/world/road_look.sii and def/world/prefab.sii. Put them in the SII prefab location, and add the "LUT1.19-" to them.
