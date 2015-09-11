#Kinect Ambient Intelligence Toolkit (KAIT)
KAIT was designed to simplify the development of context aware applications using the Kinect. The ultimate goal for KAIT is to support the creation of solutions that don't require direct user interaction but rather understand what the user is doing and deliver services to them transparently.

A simple example of KAIT's capabilities is a digital sign in a lobby. Rather than having a static content loop KAIT makes it easy to have the sign deliver content based on "who" the user is and where they are in the physical environment. But don't think of KAIT as just a digital sign solution. She's far more capable then that!
At her core KAIT is designed to extend the Kinect capabilities to support the acquisition of player demographics (age and gender), face recognition, voice, and player location content triggers as well as assisting in maintaining player state between interactions. 

This version of KAIT makes use of NEC’s industry leading facial analytics services. The KAIT team and NEC partnered to make using NEC services as easy as adding a Nuget file to a Visual Studio project. But KAIT + NEC provides more than just a Nuget as KAIT deals with issues related to image sampling cross multiple Kinect frames, correlation of biometric data to Kinect skeletons and sessions state (When do we say someone has REALY left the player space).

KAIT also goes beyond the Kinect and provides the pluming and scripts necessary to extend its services to Azure (Azure components are NOT required for client side interaction). These components allow guest experiences to be built where business and interaction logic are maintained in the cloud. It also provides KAIT developers with visibility into how people are interacting with KAIT experiences by providing telemetry capture and easy connection to Power BI dashboard!
KAIT's Azure services turn Kinect into an IoT sensor. 

KAIT produces 3 telemetry streams: Skeletal, Demographics and Interaction. These streams are delivered via Azure Event Hub and processed by Azure Stream Analytics (ASA) where you can implement advanced business rules. For example business logic could be implemented in ASA using KAIT’s data streams to notify guest services when a visitor has been “seen” in the same location for a period of time.

This all sounds very complex but "Don't Panic" KAIT contains all the scripts need to create the cloud plumbing!

But we get ahead of ourselves!

First things first. Let’s download and get KAIT working!
