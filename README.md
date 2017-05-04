SimShift
========

Driver utilities for various open roaming simulators, like ETS2.

## Windows Installation

## Reverting to version 1.19
- Right now we are trying to update to latest version, but in the mean time:
- https://forum.truckersmp.com/index.php?/topic/17-how-to-downgrade-ets2ats-to-supported-version/
- Revert to version 1.19x **FIRST** before doing any of the next instructions

## Extract base.scs and def.scs 
**Highly reccomended:** Use [SCS EXTRACTOR GUI](https://github.com/Bluscream/SCS-Extractor-GUI/releases)
1. Open SCS Extractor GUI
2. Navigate to ETS2 folder `steam\steamapps\common\Euro Truck Simulator 2\`
3. Extract base.scs (GUI will place in `\Euro Truck Simulator 2\base`)
    - THIS WILL TAKE A WHILE, THE COMMAND PROMPT WILL CLOSE BY ITSELF, DO NOT CLOSE EARLY
4. Extract def.scs (GUI will place in `\Euro Truck Simulator 2\def`)

## Set paths
1. https://github.com/zappybiby/SimShift/blob/1bd8ecd49588422da87b4c5d8fa27a3c722c0c30/SimShift/SimShift/FrmMain.cs#L37
2. https://github.com/zappybiby/SimShift/blob/1bd8ecd49588422da87b4c5d8fa27a3c722c0c30/SimShift/SimShift/FrmMain.cs#L42
3. https://github.com/zappybiby/SimShift/blob/1bd8ecd49588422da87b4c5d8fa27a3c722c0c30/SimShift/SimShift/FrmMain.cs#L45

## Install SDK Plugin for ETS2
1. Get [SDK Plugin](https://github.com/nlhans/ets2-sdk-plugin/releases)
2. Place the acquired DLL inside bin/win_x86/plugins/ of your ETS2 installation. 


