#!/bin/bash

echo. "Starting git set meta procedure..."
echo. "Copy .meta files from .LRTUnity into LRTUnity for setting meta data"
xcopy ".\Teleporter-SAINT-Joystick\Assets\.LRTUnity\*.meta" ".\Teleporter-SAINT-Joystick\Assets\LRTUnity\" /e /y

echo. "Git set meta process is done!"