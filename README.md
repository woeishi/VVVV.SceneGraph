VVVV.SceneGraph
========
simplified handling of 3d scenes and their transformation hierarchies in [vvvv beta](https://vvvv.org/)

## Short
* 3d files are loaded into a xpath searchable scene definition tree (currently only an assimp adapter via [dx11-vvvv by mrvux](https://github.com/mrvux/AssimpNet)  s implemented)
* geometries and textures are loaded onto the gpu on demand, uploads an asset only once even if in use multiple times
* applying transformations correctly resolves with it's parent transformations and propagates down to children. Each transformation modification branches out of the original transformation tree in order to only affect vvvv downstream nodes. (usage should feel like immutable clones of the graph without the performance penalty)
* Drawing either DX11 by mr vux or DX9 vvvv native

## Long
[-> contribution page](https://vvvv.org/contribution/scenegraph)

## Requirements
* [vvvv beta](https://vvvv.org/downloads) >= 35.8
* [dx11-vvvv](https://vvvv.org/contribution/directx11-nodes) >= 1.3

## Build
* clone this git repo
* edit csproj: change reference path and dx11 dependencies to your local installation
* open in VS and build

## License
Free for non-commercial use. Pay-as-you-can licensing model for commercial usage.

## Supporters
* [decode](https://www.decode.io/) sponsored a great part of v1.0
* [meso](https://meso.design/en) v2.0: transformation caching, color modification
* [wirmachenbunt](https://wirmachenbunt.de/) v2.0: animation