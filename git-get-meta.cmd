#!/bin/bash

echo. "Starting git get meta procedure..."
echo. "Copy .meta files LRTUnity into .LRTUnity for storing in this porject"
xcopy ".\Teleporter-SAINT-Joystick\Assets\LRTUnity\*.meta" ".\Teleporter-SAINT-Joystick\Assets\.LRTUnity\" /e /y

git status

echo. "Git get meta process is done..."