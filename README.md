# Path of Building Unity Edition

This project will attempt to serve as a new hosting environment for [Path of Building Community Fork](https://github.com/PathOfBuildingCommunity/PathOfBuilding) lua scripts, replacing the files which currently reside in the Path of Building installation folder, including:
 * `Path of Building.exe`
 * `Update.exe`
 * `SimpleGraphic.dll`
 * `lua51.dll`
 * `lcurl.dll`
 * `libcurl.dll`
 * `lzip.dll`
 * `lua51.dll`
 * `SimpleGraphic\*`
 * `lua\*`

## Advantages and Motivation

The advantage of using Unity is for built-in cross-platform support. With Path of Exile coming to OSX, support for running Path of Building on OSX has become more frequently requested. Additionally, support for HTML5 and mobile platforms are worth pursuing.

Additionally, the source code for `Path of Building.exe` and `Update.exe` are unavailable, and `SimpleGraphic.dll` has many issues which the community team don't have the expertise or willingness to improve or fix. The Unity implementation replaces these files with open-source files that can be built by anyone with a free Unity license.

## Unity Version

Currently, the developers are using Unity version `2020.1.6f1`.

## Lua script changes

At this time, it is undesireable to require any changes to the Path of Building Community Fork lua scripts. Ideally, they will work as-is on all Unity platforms so that the old `Path of Building.exe` can still be supported in parallel.

In the future, the Unity edition could expose special lua values which would allow the lua scripts to detect which platform they were running on. This could be especially useful for mobile, where touch input and window layout would need modification.

## Progress

This project is still in very early stages and needs a lot of work for full lua support. At this time, it is best served as a solo project, but if you have particular interest in helping then please open up an issue on this repository and we'll go from there.

## Pull Requests

I will look at any pull requests, but I'd prefer to know ahead of time that you are working on one so that we can avoid conflicts or duplicated effort.

## Building

See [BUILDING.md](BUILDING.md).
