# KAIT
Kinect Ambient Intelligence Toolkit (KAIT)

KAIT was designed to simplify the development of context aware applications using the Kinect. The ultimate goal for KAIT is to support the creation of solutions that don't require direct user interaction but rather understand what they're doing and deliver services to them transperently.

A simple example of KAIT's capabilities is a digital sign in a lobby. Rather than having a static content loop KAIT makes it easy to have the sign display content based on "who" the user is and where they are in the phisical space and even what they're doing. But don't classify KAIT as a digial sign solution. She's far more capabile then that!

At her core KAIT is designed to extend the core Kinect capabilities to support the aqusition of player demographics (age and gender), face recoginiton, voice, and player location content triggers. KAIT goes beyond the Kinect and also provides the plumming and scripts nesscary to extend her services to Azure (Use of the Azure components are NOT required to use KAIT)

KAIT's Azure services allow you make the Kinect an IoT sensor. KAIT will produce 3 data streams: Skeletal, Demographics and Interaction. These streams are delivered via Azure Event Hub and processed by Azure Stream Analytics where you can implement advanced business rules that work example could notifiy people and services when a use has been dwelling in the same location for a period of time.

This all sounds very complext but "Don't Panic" KAIT contains all the scripts need to creat the cloud plumbing!

But we get ahead of ourselves!

First things first.

1. You will need the Kinect SDK v2 + the speech SDK Components (64bit Please)
2. Download KAIT
2. Open the KAIT.BiometricReference.snl
3. Compile
4. 



