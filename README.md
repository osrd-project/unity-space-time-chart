This is a basic POC for a 3D space time chart, plugged to editoast's endpoints.

Work in progress.

## Getting started

Requirements:

1. [Unity](https://unity.com/download)
2. .net runtime, handled by unity hub on Windows (through a visual studio install), can be installed through e.g. pacman on linux

Import the project in unity and run.

## Map background

Tiles are fetches as png from [mapbox](https://www.mapbox.com/) and then displayed as textures.

For the tiles to be loaded, a mapbox token needs to be set in mapbox.key.
Otherwise the tiles would just be blank, they still keep track of which areas have been loaded.

## Linter

I use a CSharpier plugin in my IDE. There's no CI check for now, but running the linter at each commit would be appreciated.
