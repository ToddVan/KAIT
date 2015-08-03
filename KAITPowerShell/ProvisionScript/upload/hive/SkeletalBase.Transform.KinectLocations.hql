DROP TABLE IF EXISTS KinectLocations;
CREATE TABLE KinectLocations(
	KinectDeviceId BIGINT,
	Longitude FLOAT,
	Latitude FLOAT
);

INSERT INTO TABLE KinectLocations
SELECT st.KinectDeviceId, st.Longitude, st.Latitude
FROM SkeletonTrack st
GROUP BY st.KinectDeviceId, st.Longitude, st.Latitude;
