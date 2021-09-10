# Hand Tracking for Mobile Virtual Reality

Mobile Virtual Reality inspired by Google Cardboard (where a smartphone is placed in a cheap headset with no electronics) has lost its popularity after standalone VR headsets appeared on the market. The devices like Oculus Quest support full tracking of the real world position and rotation of the headset and a pair of controllers (or even just the user’s hands), all without any external sensors or computers, while Cardboard-like applications only track the rotation of the user's head. We tried to create a framework that could provide a similar experience to the standalone headsets - a mobile VR application capable of tracking itself and the user's hands in 6 degrees of freedom, all while only requiring a smartphone with a few cheap additions. We used Unity AR Foundation to achieve headset tracking and a variety of OpenCV algorithms to solve hand tracking - ArUco markers, Color Thresholding, Camshift, and deep learning approaches like OpenPose and YOLOv3. Our main focus was on testing the hand tracking algorithms, and we discovered that while they are far from perfect, the concept is feasible, and, with some improvements, the framework could become a real competitor in the space of standalone VR headsets.

## Running the code

This project can be opened in Unity. It is recommended to use the version that the application was developed in - 2020.3.2f1.

The project needs the following packages from the Unity Asset Store and the Unity Package Registry to function:

-   AR Foundation (4.0.12)
-   ARCore XR Plugin (4.0.12)
-   ARKit XR Plugin (4.0.12)
-   TextMeshPro (3.0.4)
-   [OpenCV for Unity](https://assetstore.unity.com/packages/tools/integration/opencv-for-unity-21088) (2.4.3)

The following assets are not essential but required to run the _Main_ scene.

-   [Outdoor Ground Textures](https://assetstore.unity.com/packages/2d/textures-materials/floors/outdoor-ground-textures-12555) (1.2.1)
-   [Nature Starter Kit 2](https://assetstore.unity.com/packages/3d/environments/nature-starter-kit-2-52977) (1.0)
-   [Free HDR Sky](https://assetstore.unity.com/packages/2d/textures-materials/sky/free-hdr-sky-61217) (1.0)
-   [Plank Textures PBR](https://assetstore.unity.com/packages/2d/textures-materials/wood/plank-textures-pbr-72318) (1.0)

Once the project is set up and all the assets are installed, the application can be run in the Unity Editor. It is worth mentioning that when loading up the project for the first time, Unity can show a warning related to missing packages. It can be ignored as the packages above are indeed missing and need to be added manually.

## Deep dive

The extended description of the process I went through while designing this as well as all the testing and final results can be found in the full text of my Bachelor thesis - [Hand Tracking For Mobile Virtual Reality](./HandTrackingForMobileVR.pdf).
