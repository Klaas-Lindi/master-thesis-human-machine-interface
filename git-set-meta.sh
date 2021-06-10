#!/bin/bash

echo "Starting git set meta procedure..."
echo "Copy .meta files from .LRTUnity into LRTUnity for setting meta data"

cd ./Teleporter-SAINT-Joystick/Assets/.LRTUnity/
find . -name '*.meta' -exec cp -v --parents \{\} ../LRTUnity \;

echo "Git set meta process is done!"
