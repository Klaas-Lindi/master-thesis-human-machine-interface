# LRTUnity

This repository includes the core lrt unity asset package for all unity projects in TOR Teleoepration

## Installation

Clone this project within the /Assets/ folder of the unity projects

## Structure

* **LRT_Animation**: Contains animation for UI.
* **LRT_ExtMap**: Contains configuration for the Map from Mapbox. Currently only used by the CopKa HMI.
* **LRT_ExtPointCloud**: Contains test data for point clouds.
* **LRT_Materials**: Contains the defined materials which are used for LRT objects.
* **LRT_Models**: LRT own models for unity environment:
  * Gimbal
  * UAV
  * Panda
* **LRT_Prefab**: Contains common used prefabs
* **LRT_Skripts**: Contains all script used over multiple unity projects
* **LRT_Sprite**: Contains UI sprites.

## To Do's
* The whole structure has to be restructured and aligned
* This assets should be packaged for unity to import it as a package

## Important Notes

* **Currently all \*.meta files are ignored. This can cause to missing references! Use git-get-meta and git-set-meta to avoid wrong references** 



 