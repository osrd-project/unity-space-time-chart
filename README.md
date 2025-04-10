This is a basic POC for a 3D space time chart, plugged to editoast's endpoints.

Work in progress.

## Getting started

Requirements:

1. [Unity](https://unity.com/download)
2. .net runtime, handled by unity hub on Windows (through a visual studio install), can be installed through e.g. pacman on linux
3. A running OSRD stack (excluding front-end) with an infra in DB and a timetable with some trains

Import the project in unity and run.

## Config

For now the configuration is done in the camera settings in the scene.

Most important parameters are infra ID, timetable ID, and lat/lon start location.

## Map background

Tiles are fetches as png from [mapbox](https://www.mapbox.com/) and then displayed as textures.

For the tiles to be loaded, a mapbox token needs to be set in mapbox.key.
Otherwise the tiles would just be blank, they still keep track of which areas have been loaded.

## Controls

Camera angles are controlled with the mouse.

(I'll describe shortcuts on an AZERTY keyboard but they're correctly mapped to any layout)

Camera can be moved with ZQSD. Pressing shift makes it move faster.

Train timeline is moved with A and E. Pressing shift and/or ctrl makes it move faster.

Map can be zoomed in/out with O/P.

Train times can be zoomed in/out with L/M. 

## Linter

I use a CSharpier plugin in my IDE. There's no CI check for now, but running the linter at each commit would be appreciated.
