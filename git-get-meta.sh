#!/bin/bash

echo "Starting git get meta procedure..."
echo "Copy .meta files LRTUnity into .LRTUnity for storing in this porject"

cd ./Teleporter-SAINT-Joystick/Assets/LRTUnity/
find . -name '*.meta' -exec cp -v --parents \{\} ../.LRTUnity \;

echo "Git get meta process is done!"

# get git status
git status
