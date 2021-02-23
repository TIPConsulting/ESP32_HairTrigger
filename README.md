# Hair Trigger

Control your favorite FPS games with a gyroscopic glove.  This project uses C# on the ESP32 via [NanoFramework](https://www.nanoframework.net/). Client/Server integration is achieved via the [ArdNet](https://dev.azure.com/tipconsulting/ArdNet) IoT framework.

![HairTrigger Diagram](https://raw.githubusercontent.com/TIPConsulting/ESP32_HairTrigger/main/Diagrams/HairTriggerDiagram.jpg)

<img src="https://raw.githubusercontent.com/TIPConsulting/ESP32_HairTrigger/master/Diagrams/Beetle32_Pinout_Front.JPG" alt="Beetle ESP32 Front" width="400px"> <img src="https://raw.githubusercontent.com/TIPConsulting/ESP32_HairTrigger/master/Diagrams/Beetle32_Pinout_Back.JPG" alt="Beetle ESP32 Back" width="400px">

## Hardware

- 1x Glove
- 1x [Beetle ESP32](https://www.dfrobot.com/product-1798.html)
- 1x 500mAh Lipo battery
- JST-PH female plug
- Copper tape
- Assorted lengths of wire

## Inspiration

I've been wanting to make an IoT wearable for a while to test out the new [ArdNet Nano](https://dev.azure.com/tipconsulting/ArdNet/_wiki/wikis/ArdNet.wiki/301/ArdNet.Nano) implementation for ESP32.  This proved to be a good opportunity.  At first I was essentially planning a remote integrated with a glove, but I decided that wouldn't be enough to truly demonstrate the platform.  Sitting at the drawing board once more (read: playing games), I came up with this: What if I could actually aim with my hand while I played?  Nobody's *ever* done that before!  Thus was born Hair Trigger.

<img src="https://raw.githubusercontent.com/TIPConsulting/ESP32_HairTrigger/master/Diagrams/TestRig2.JPG" alt="Test Rig" width="400px">

## Design

The glove build isn't too complicated, the vast majority of the system complexity lives in the software instead of the hardware.  The glove has the controller and MPU sewn on to the back of the hand where they will be unobtrusive.  The hardest part here is making sure you get the MPU oriented properly to match the software axis expectations.  There is then a strip of conductive tape across the palm to act as a "safety" - user gestures will only be sent when the capacitive touch strip is triggered.  Hopefully this will help solve drift problems from unreliable sensors; wearers will be able to release the safety and reposition their hand to get comfortable.  Finally, we need a way to shoot, so there will also a capactive touch trigger accessible near the thumb

<img src="https://raw.githubusercontent.com/TIPConsulting/ESP32_HairTrigger/master/Diagrams/CapTouchPlacement.png" alt="Cap Touch Placement" width="400px">

## Applications

TODO

## Build Process

<img src="https://raw.githubusercontent.com/TIPConsulting/ESP32_HairTrigger/master/Diagrams/TestRig1.JPG" alt="Test Rig" width="400px">

<img src="https://raw.githubusercontent.com/TIPConsulting/ESP32_HairTrigger/master/Diagrams/TestRigPower.JPG" alt="Test Rig Power" width="400px">

