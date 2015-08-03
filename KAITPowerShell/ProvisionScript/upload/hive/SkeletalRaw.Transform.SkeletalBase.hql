DROP TABLE IF EXISTS SkeletonTrackRaw;
CREATE EXTERNAL TABLE SkeletonTrackRaw(
	raw_json STRING
)
STORED AS TEXTFILE LOCATION 'wasb://telemetry@${hiveconf:RawDataStorageAccount}.blob.core.windows.net/skeletal/';

DROP TABLE IF EXISTS SkeletonTrack;
CREATE TABLE SkeletonTrack
(
	TrackingId BIGINT,
	KinectDeviceId BIGINT,
	Longitude FLOAT,
	Latitude FLOAT,
	Timestamp TIMESTAMP,
	TimeInterval STRING,
	HourOfDay TINYINT,
	Joint STRING,
	X FLOAT,
	Y FLOAT,
	Z FLOAT,
	Orientation FLOAT
);

INSERT INTO TABLE SkeletonTrack
SELECT	CAST(BaseJSON.TrackingId AS BIGINT) AS TrackingId,
		CAST(BaseJSON.KinectDeviceId AS BIGINT) AS KinectDeviceId,
		CAST(Coords.Longitude AS FLOAT) AS Longitude,
		CAST(Coords.Latitude AS FLOAT) AS Latitude,
		CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP) AS Timestamp,
		CASE
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 8) 
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 8 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 23) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':15:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 23 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 38) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':30:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 38 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 53)
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':45:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 53)
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
		END AS TimeInterval,
		HOUR(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) AS HourOfDay,
		'SpineBase' AS Joint,
		CAST(JointInfo.X AS FLOAT) AS X,
		CAST(JointInfo.Y AS FLOAT) AS Y,
		CAST(JointInfo.Z AS FLOAT) AS Z,
		CAST(JointInfo.Orientation AS FLOAT) AS Orientation
FROM SkeletonTrackRaw st
		LATERAL VIEW JSON_TUPLE(st.raw_json,'KinectDeviceId','Location','TrackingId','TimeStamp','Joints') BaseJSON
			AS KinectDeviceId, Location, TrackingId, Timestamp, JointCollection
		LATERAL VIEW JSON_TUPLE(BaseJSON.Location,'Longitude','Latitude') Coords
			AS Longitude, Latitude
		LATERAL VIEW JSON_TUPLE(BaseJSON.JointCollection,'SpineBase') OneJoint
			AS ThisJoint
		LATERAL VIEW JSON_TUPLE(OneJoint.ThisJoint,'Orientation','X','Y','Z') JointInfo
			AS Orientation, X, Y, Z;

INSERT INTO TABLE SkeletonTrack
SELECT	CAST(BaseJSON.TrackingId AS BIGINT) AS TrackingId,
		CAST(BaseJSON.KinectDeviceId AS BIGINT) AS KinectDeviceId,
		CAST(Coords.Longitude AS FLOAT) AS Longitude,
		CAST(Coords.Latitude AS FLOAT) AS Latitude,
		CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP) AS Timestamp,
		CASE
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 8) 
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 8 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 23) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':15:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 23 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 38) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':30:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 38 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 53)
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':45:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 53)
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
		END AS TimeInterval,
		HOUR(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) AS HourOfDay,
		'SpineMid' AS Joint,
		CAST(JointInfo.X AS FLOAT) AS X,
		CAST(JointInfo.Y AS FLOAT) AS Y,
		CAST(JointInfo.Z AS FLOAT) AS Z,
		CAST(JointInfo.Orientation AS FLOAT) AS Orientation
FROM SkeletonTrackRaw st
		LATERAL VIEW JSON_TUPLE(st.raw_json,'KinectDeviceId','Location','TrackingId','TimeStamp','Joints') BaseJSON
			AS KinectDeviceId, Location, TrackingId, Timestamp, JointCollection
		LATERAL VIEW JSON_TUPLE(BaseJSON.Location,'Longitude','Latitude') Coords
			AS Longitude, Latitude
		LATERAL VIEW JSON_TUPLE(BaseJSON.JointCollection,'SpineMid') OneJoint
			AS ThisJoint
		LATERAL VIEW JSON_TUPLE(OneJoint.ThisJoint,'Orientation','X','Y','Z') JointInfo
			AS Orientation, X, Y, Z;

INSERT INTO TABLE SkeletonTrack
SELECT	CAST(BaseJSON.TrackingId AS BIGINT) AS TrackingId,
		CAST(BaseJSON.KinectDeviceId AS BIGINT) AS KinectDeviceId,
		CAST(Coords.Longitude AS FLOAT) AS Longitude,
		CAST(Coords.Latitude AS FLOAT) AS Latitude,
		CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP) AS Timestamp,
		CASE
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 8) 
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 8 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 23) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':15:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 23 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 38) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':30:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 38 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 53)
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':45:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 53)
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
		END AS TimeInterval,
		HOUR(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) AS HourOfDay,
		'Neck' AS Joint,
		CAST(JointInfo.X AS FLOAT) AS X,
		CAST(JointInfo.Y AS FLOAT) AS Y,
		CAST(JointInfo.Z AS FLOAT) AS Z,
		CAST(JointInfo.Orientation AS FLOAT) AS Orientation
FROM SkeletonTrackRaw st
		LATERAL VIEW JSON_TUPLE(st.raw_json,'KinectDeviceId','Location','TrackingId','TimeStamp','Joints') BaseJSON
			AS KinectDeviceId, Location, TrackingId, Timestamp, JointCollection
		LATERAL VIEW JSON_TUPLE(BaseJSON.Location,'Longitude','Latitude') Coords
			AS Longitude, Latitude
		LATERAL VIEW JSON_TUPLE(BaseJSON.JointCollection,'Neck') OneJoint
			AS ThisJoint
		LATERAL VIEW JSON_TUPLE(OneJoint.ThisJoint,'Orientation','X','Y','Z') JointInfo
			AS Orientation, X, Y, Z;

INSERT INTO TABLE SkeletonTrack
SELECT	CAST(BaseJSON.TrackingId AS BIGINT) AS TrackingId,
		CAST(BaseJSON.KinectDeviceId AS BIGINT) AS KinectDeviceId,
		CAST(Coords.Longitude AS FLOAT) AS Longitude,
		CAST(Coords.Latitude AS FLOAT) AS Latitude,
		CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP) AS Timestamp,
		CASE
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 8) 
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 8 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 23) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':15:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 23 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 38) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':30:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 38 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 53)
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':45:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 53)
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
		END AS TimeInterval,
		HOUR(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) AS HourOfDay,
		'Head' AS Joint,
		CAST(JointInfo.X AS FLOAT) AS X,
		CAST(JointInfo.Y AS FLOAT) AS Y,
		CAST(JointInfo.Z AS FLOAT) AS Z,
		CAST(JointInfo.Orientation AS FLOAT) AS Orientation
FROM SkeletonTrackRaw st
		LATERAL VIEW JSON_TUPLE(st.raw_json,'KinectDeviceId','Location','TrackingId','TimeStamp','Joints') BaseJSON
			AS KinectDeviceId, Location, TrackingId, Timestamp, JointCollection
		LATERAL VIEW JSON_TUPLE(BaseJSON.Location,'Longitude','Latitude') Coords
			AS Longitude, Latitude
		LATERAL VIEW JSON_TUPLE(BaseJSON.JointCollection,'Head') OneJoint
			AS ThisJoint
		LATERAL VIEW JSON_TUPLE(OneJoint.ThisJoint,'Orientation','X','Y','Z') JointInfo
			AS Orientation, X, Y, Z;

INSERT INTO TABLE SkeletonTrack
SELECT	CAST(BaseJSON.TrackingId AS BIGINT) AS TrackingId,
		CAST(BaseJSON.KinectDeviceId AS BIGINT) AS KinectDeviceId,
		CAST(Coords.Longitude AS FLOAT) AS Longitude,
		CAST(Coords.Latitude AS FLOAT) AS Latitude,
		CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP) AS Timestamp,
		CASE
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 8) 
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 8 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 23) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':15:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 23 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 38) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':30:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 38 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 53)
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':45:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 53)
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
		END AS TimeInterval,
		HOUR(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) AS HourOfDay,
		'ShoulderLeft' AS Joint,
		CAST(JointInfo.X AS FLOAT) AS X,
		CAST(JointInfo.Y AS FLOAT) AS Y,
		CAST(JointInfo.Z AS FLOAT) AS Z,
		CAST(JointInfo.Orientation AS FLOAT) AS Orientation
FROM SkeletonTrackRaw st
		LATERAL VIEW JSON_TUPLE(st.raw_json,'KinectDeviceId','Location','TrackingId','TimeStamp','Joints') BaseJSON
			AS KinectDeviceId, Location, TrackingId, Timestamp, JointCollection
		LATERAL VIEW JSON_TUPLE(BaseJSON.Location,'Longitude','Latitude') Coords
			AS Longitude, Latitude
		LATERAL VIEW JSON_TUPLE(BaseJSON.JointCollection,'ShoulderLeft') OneJoint
			AS ThisJoint
		LATERAL VIEW JSON_TUPLE(OneJoint.ThisJoint,'Orientation','X','Y','Z') JointInfo
			AS Orientation, X, Y, Z;

INSERT INTO TABLE SkeletonTrack
SELECT	CAST(BaseJSON.TrackingId AS BIGINT) AS TrackingId,
		CAST(BaseJSON.KinectDeviceId AS BIGINT) AS KinectDeviceId,
		CAST(Coords.Longitude AS FLOAT) AS Longitude,
		CAST(Coords.Latitude AS FLOAT) AS Latitude,
		CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP) AS Timestamp,
		CASE
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 8) 
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 8 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 23) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':15:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 23 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 38) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':30:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 38 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 53)
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':45:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 53)
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
		END AS TimeInterval,
		HOUR(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) AS HourOfDay,
		'ElbowLeft' AS Joint,
		CAST(JointInfo.X AS FLOAT) AS X,
		CAST(JointInfo.Y AS FLOAT) AS Y,
		CAST(JointInfo.Z AS FLOAT) AS Z,
		CAST(JointInfo.Orientation AS FLOAT) AS Orientation
FROM SkeletonTrackRaw st
		LATERAL VIEW JSON_TUPLE(st.raw_json,'KinectDeviceId','Location','TrackingId','TimeStamp','Joints') BaseJSON
			AS KinectDeviceId, Location, TrackingId, Timestamp, JointCollection
		LATERAL VIEW JSON_TUPLE(BaseJSON.Location,'Longitude','Latitude') Coords
			AS Longitude, Latitude
		LATERAL VIEW JSON_TUPLE(BaseJSON.JointCollection,'ElbowLeft') OneJoint
			AS ThisJoint
		LATERAL VIEW JSON_TUPLE(OneJoint.ThisJoint,'Orientation','X','Y','Z') JointInfo
			AS Orientation, X, Y, Z;

INSERT INTO TABLE SkeletonTrack
SELECT	CAST(BaseJSON.TrackingId AS BIGINT) AS TrackingId,
		CAST(BaseJSON.KinectDeviceId AS BIGINT) AS KinectDeviceId,
		CAST(Coords.Longitude AS FLOAT) AS Longitude,
		CAST(Coords.Latitude AS FLOAT) AS Latitude,
		CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP) AS Timestamp,
		CASE
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 8) 
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 8 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 23) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':15:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 23 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 38) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':30:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 38 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 53)
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':45:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 53)
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
		END AS TimeInterval,
		HOUR(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) AS HourOfDay,
		'WristLeft' AS Joint,
		CAST(JointInfo.X AS FLOAT) AS X,
		CAST(JointInfo.Y AS FLOAT) AS Y,
		CAST(JointInfo.Z AS FLOAT) AS Z,
		CAST(JointInfo.Orientation AS FLOAT) AS Orientation
FROM SkeletonTrackRaw st
		LATERAL VIEW JSON_TUPLE(st.raw_json,'KinectDeviceId','Location','TrackingId','TimeStamp','Joints') BaseJSON
			AS KinectDeviceId, Location, TrackingId, Timestamp, JointCollection
		LATERAL VIEW JSON_TUPLE(BaseJSON.Location,'Longitude','Latitude') Coords
			AS Longitude, Latitude
		LATERAL VIEW JSON_TUPLE(BaseJSON.JointCollection,'WristLeft') OneJoint
			AS ThisJoint
		LATERAL VIEW JSON_TUPLE(OneJoint.ThisJoint,'Orientation','X','Y','Z') JointInfo
			AS Orientation, X, Y, Z;

INSERT INTO TABLE SkeletonTrack
SELECT	CAST(BaseJSON.TrackingId AS BIGINT) AS TrackingId,
		CAST(BaseJSON.KinectDeviceId AS BIGINT) AS KinectDeviceId,
		CAST(Coords.Longitude AS FLOAT) AS Longitude,
		CAST(Coords.Latitude AS FLOAT) AS Latitude,
		CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP) AS Timestamp,
		CASE
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 8) 
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 8 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 23) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':15:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 23 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 38) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':30:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 38 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 53)
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':45:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 53)
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
		END AS TimeInterval,
		HOUR(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) AS HourOfDay,
		'HandLeft' AS Joint,
		CAST(JointInfo.X AS FLOAT) AS X,
		CAST(JointInfo.Y AS FLOAT) AS Y,
		CAST(JointInfo.Z AS FLOAT) AS Z,
		CAST(JointInfo.Orientation AS FLOAT) AS Orientation
FROM SkeletonTrackRaw st
		LATERAL VIEW JSON_TUPLE(st.raw_json,'KinectDeviceId','Location','TrackingId','TimeStamp','Joints') BaseJSON
			AS KinectDeviceId, Location, TrackingId, Timestamp, JointCollection
		LATERAL VIEW JSON_TUPLE(BaseJSON.Location,'Longitude','Latitude') Coords
			AS Longitude, Latitude
		LATERAL VIEW JSON_TUPLE(BaseJSON.JointCollection,'HandLeft') OneJoint
			AS ThisJoint
		LATERAL VIEW JSON_TUPLE(OneJoint.ThisJoint,'Orientation','X','Y','Z') JointInfo
			AS Orientation, X, Y, Z;

INSERT INTO TABLE SkeletonTrack
SELECT	CAST(BaseJSON.TrackingId AS BIGINT) AS TrackingId,
		CAST(BaseJSON.KinectDeviceId AS BIGINT) AS KinectDeviceId,
		CAST(Coords.Longitude AS FLOAT) AS Longitude,
		CAST(Coords.Latitude AS FLOAT) AS Latitude,
		CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP) AS Timestamp,
		CASE
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 8) 
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 8 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 23) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':15:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 23 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 38) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':30:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 38 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 53)
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':45:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 53)
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
		END AS TimeInterval,
		HOUR(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) AS HourOfDay,
		'ShoulderRight' AS Joint,
		CAST(JointInfo.X AS FLOAT) AS X,
		CAST(JointInfo.Y AS FLOAT) AS Y,
		CAST(JointInfo.Z AS FLOAT) AS Z,
		CAST(JointInfo.Orientation AS FLOAT) AS Orientation
FROM SkeletonTrackRaw st
		LATERAL VIEW JSON_TUPLE(st.raw_json,'KinectDeviceId','Location','TrackingId','TimeStamp','Joints') BaseJSON
			AS KinectDeviceId, Location, TrackingId, Timestamp, JointCollection
		LATERAL VIEW JSON_TUPLE(BaseJSON.Location,'Longitude','Latitude') Coords
			AS Longitude, Latitude
		LATERAL VIEW JSON_TUPLE(BaseJSON.JointCollection,'ShoulderRight') OneJoint
			AS ThisJoint
		LATERAL VIEW JSON_TUPLE(OneJoint.ThisJoint,'Orientation','X','Y','Z') JointInfo
			AS Orientation, X, Y, Z;

INSERT INTO TABLE SkeletonTrack
SELECT	CAST(BaseJSON.TrackingId AS BIGINT) AS TrackingId,
		CAST(BaseJSON.KinectDeviceId AS BIGINT) AS KinectDeviceId,
		CAST(Coords.Longitude AS FLOAT) AS Longitude,
		CAST(Coords.Latitude AS FLOAT) AS Latitude,
		CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP) AS Timestamp,
		CASE
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 8) 
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 8 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 23) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':15:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 23 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 38) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':30:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 38 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 53)
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':45:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 53)
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
		END AS TimeInterval,
		HOUR(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) AS HourOfDay,
		'ElbowRight' AS Joint,
		CAST(JointInfo.X AS FLOAT) AS X,
		CAST(JointInfo.Y AS FLOAT) AS Y,
		CAST(JointInfo.Z AS FLOAT) AS Z,
		CAST(JointInfo.Orientation AS FLOAT) AS Orientation
FROM SkeletonTrackRaw st
		LATERAL VIEW JSON_TUPLE(st.raw_json,'KinectDeviceId','Location','TrackingId','TimeStamp','Joints') BaseJSON
			AS KinectDeviceId, Location, TrackingId, Timestamp, JointCollection
		LATERAL VIEW JSON_TUPLE(BaseJSON.Location,'Longitude','Latitude') Coords
			AS Longitude, Latitude
		LATERAL VIEW JSON_TUPLE(BaseJSON.JointCollection,'ElbowRight') OneJoint
			AS ThisJoint
		LATERAL VIEW JSON_TUPLE(OneJoint.ThisJoint,'Orientation','X','Y','Z') JointInfo
			AS Orientation, X, Y, Z;

INSERT INTO TABLE SkeletonTrack
SELECT	CAST(BaseJSON.TrackingId AS BIGINT) AS TrackingId,
		CAST(BaseJSON.KinectDeviceId AS BIGINT) AS KinectDeviceId,
		CAST(Coords.Longitude AS FLOAT) AS Longitude,
		CAST(Coords.Latitude AS FLOAT) AS Latitude,
		CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP) AS Timestamp,
		CASE
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 8) 
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 8 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 23) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':15:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 23 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 38) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':30:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 38 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 53)
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':45:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 53)
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
		END AS TimeInterval,
		HOUR(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) AS HourOfDay,
		'WristRight' AS Joint,
		CAST(JointInfo.X AS FLOAT) AS X,
		CAST(JointInfo.Y AS FLOAT) AS Y,
		CAST(JointInfo.Z AS FLOAT) AS Z,
		CAST(JointInfo.Orientation AS FLOAT) AS Orientation
FROM SkeletonTrackRaw st
		LATERAL VIEW JSON_TUPLE(st.raw_json,'KinectDeviceId','Location','TrackingId','TimeStamp','Joints') BaseJSON
			AS KinectDeviceId, Location, TrackingId, Timestamp, JointCollection
		LATERAL VIEW JSON_TUPLE(BaseJSON.Location,'Longitude','Latitude') Coords
			AS Longitude, Latitude
		LATERAL VIEW JSON_TUPLE(BaseJSON.JointCollection,'WristRight') OneJoint
			AS ThisJoint
		LATERAL VIEW JSON_TUPLE(OneJoint.ThisJoint,'Orientation','X','Y','Z') JointInfo
			AS Orientation, X, Y, Z;

INSERT INTO TABLE SkeletonTrack
SELECT	CAST(BaseJSON.TrackingId AS BIGINT) AS TrackingId,
		CAST(BaseJSON.KinectDeviceId AS BIGINT) AS KinectDeviceId,
		CAST(Coords.Longitude AS FLOAT) AS Longitude,
		CAST(Coords.Latitude AS FLOAT) AS Latitude,
		CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP) AS Timestamp,
		CASE
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 8) 
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 8 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 23) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':15:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 23 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 38) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':30:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 38 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 53)
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':45:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 53)
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
		END AS TimeInterval,
		HOUR(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) AS HourOfDay,
		'HandRight' AS Joint,
		CAST(JointInfo.X AS FLOAT) AS X,
		CAST(JointInfo.Y AS FLOAT) AS Y,
		CAST(JointInfo.Z AS FLOAT) AS Z,
		CAST(JointInfo.Orientation AS FLOAT) AS Orientation
FROM SkeletonTrackRaw st
		LATERAL VIEW JSON_TUPLE(st.raw_json,'KinectDeviceId','Location','TrackingId','TimeStamp','Joints') BaseJSON
			AS KinectDeviceId, Location, TrackingId, Timestamp, JointCollection
		LATERAL VIEW JSON_TUPLE(BaseJSON.Location,'Longitude','Latitude') Coords
			AS Longitude, Latitude
		LATERAL VIEW JSON_TUPLE(BaseJSON.JointCollection,'HandRight') OneJoint
			AS ThisJoint
		LATERAL VIEW JSON_TUPLE(OneJoint.ThisJoint,'Orientation','X','Y','Z') JointInfo
			AS Orientation, X, Y, Z;

INSERT INTO TABLE SkeletonTrack
SELECT	CAST(BaseJSON.TrackingId AS BIGINT) AS TrackingId,
		CAST(BaseJSON.KinectDeviceId AS BIGINT) AS KinectDeviceId,
		CAST(Coords.Longitude AS FLOAT) AS Longitude,
		CAST(Coords.Latitude AS FLOAT) AS Latitude,
		CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP) AS Timestamp,
		CASE
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 8) 
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 8 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 23) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':15:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 23 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 38) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':30:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 38 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 53)
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':45:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 53)
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
		END AS TimeInterval,
		HOUR(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) AS HourOfDay,
		'HipLeft' AS Joint,
		CAST(JointInfo.X AS FLOAT) AS X,
		CAST(JointInfo.Y AS FLOAT) AS Y,
		CAST(JointInfo.Z AS FLOAT) AS Z,
		CAST(JointInfo.Orientation AS FLOAT) AS Orientation
FROM SkeletonTrackRaw st
		LATERAL VIEW JSON_TUPLE(st.raw_json,'KinectDeviceId','Location','TrackingId','TimeStamp','Joints') BaseJSON
			AS KinectDeviceId, Location, TrackingId, Timestamp, JointCollection
		LATERAL VIEW JSON_TUPLE(BaseJSON.Location,'Longitude','Latitude') Coords
			AS Longitude, Latitude
		LATERAL VIEW JSON_TUPLE(BaseJSON.JointCollection,'HipLeft') OneJoint
			AS ThisJoint
		LATERAL VIEW JSON_TUPLE(OneJoint.ThisJoint,'Orientation','X','Y','Z') JointInfo
			AS Orientation, X, Y, Z;

INSERT INTO TABLE SkeletonTrack
SELECT	CAST(BaseJSON.TrackingId AS BIGINT) AS TrackingId,
		CAST(BaseJSON.KinectDeviceId AS BIGINT) AS KinectDeviceId,
		CAST(Coords.Longitude AS FLOAT) AS Longitude,
		CAST(Coords.Latitude AS FLOAT) AS Latitude,
		CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP) AS Timestamp,
		CASE
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 8) 
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 8 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 23) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':15:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 23 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 38) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':30:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 38 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 53)
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':45:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 53)
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
		END AS TimeInterval,
		HOUR(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) AS HourOfDay,
		'KneeLeft' AS Joint,
		CAST(JointInfo.X AS FLOAT) AS X,
		CAST(JointInfo.Y AS FLOAT) AS Y,
		CAST(JointInfo.Z AS FLOAT) AS Z,
		CAST(JointInfo.Orientation AS FLOAT) AS Orientation
FROM SkeletonTrackRaw st
		LATERAL VIEW JSON_TUPLE(st.raw_json,'KinectDeviceId','Location','TrackingId','TimeStamp','Joints') BaseJSON
			AS KinectDeviceId, Location, TrackingId, Timestamp, JointCollection
		LATERAL VIEW JSON_TUPLE(BaseJSON.Location,'Longitude','Latitude') Coords
			AS Longitude, Latitude
		LATERAL VIEW JSON_TUPLE(BaseJSON.JointCollection,'KneeLeft') OneJoint
			AS ThisJoint
		LATERAL VIEW JSON_TUPLE(OneJoint.ThisJoint,'Orientation','X','Y','Z') JointInfo
			AS Orientation, X, Y, Z;

INSERT INTO TABLE SkeletonTrack
SELECT	CAST(BaseJSON.TrackingId AS BIGINT) AS TrackingId,
		CAST(BaseJSON.KinectDeviceId AS BIGINT) AS KinectDeviceId,
		CAST(Coords.Longitude AS FLOAT) AS Longitude,
		CAST(Coords.Latitude AS FLOAT) AS Latitude,
		CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP) AS Timestamp,
		CASE
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 8) 
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 8 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 23) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':15:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 23 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 38) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':30:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 38 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 53)
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':45:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 53)
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
		END AS TimeInterval,
		HOUR(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) AS HourOfDay,
		'AnkleLeft' AS Joint,
		CAST(JointInfo.X AS FLOAT) AS X,
		CAST(JointInfo.Y AS FLOAT) AS Y,
		CAST(JointInfo.Z AS FLOAT) AS Z,
		CAST(JointInfo.Orientation AS FLOAT) AS Orientation
FROM SkeletonTrackRaw st
		LATERAL VIEW JSON_TUPLE(st.raw_json,'KinectDeviceId','Location','TrackingId','TimeStamp','Joints') BaseJSON
			AS KinectDeviceId, Location, TrackingId, Timestamp, JointCollection
		LATERAL VIEW JSON_TUPLE(BaseJSON.Location,'Longitude','Latitude') Coords
			AS Longitude, Latitude
		LATERAL VIEW JSON_TUPLE(BaseJSON.JointCollection,'AnkleLeft') OneJoint
			AS ThisJoint
		LATERAL VIEW JSON_TUPLE(OneJoint.ThisJoint,'Orientation','X','Y','Z') JointInfo
			AS Orientation, X, Y, Z;

INSERT INTO TABLE SkeletonTrack
SELECT	CAST(BaseJSON.TrackingId AS BIGINT) AS TrackingId,
		CAST(BaseJSON.KinectDeviceId AS BIGINT) AS KinectDeviceId,
		CAST(Coords.Longitude AS FLOAT) AS Longitude,
		CAST(Coords.Latitude AS FLOAT) AS Latitude,
		CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP) AS Timestamp,
		CASE
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 8) 
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 8 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 23) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':15:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 23 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 38) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':30:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 38 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 53)
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':45:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 53)
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
		END AS TimeInterval,
		HOUR(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) AS HourOfDay,
		'FootLeft' AS Joint,
		CAST(JointInfo.X AS FLOAT) AS X,
		CAST(JointInfo.Y AS FLOAT) AS Y,
		CAST(JointInfo.Z AS FLOAT) AS Z,
		CAST(JointInfo.Orientation AS FLOAT) AS Orientation
FROM SkeletonTrackRaw st
		LATERAL VIEW JSON_TUPLE(st.raw_json,'KinectDeviceId','Location','TrackingId','TimeStamp','Joints') BaseJSON
			AS KinectDeviceId, Location, TrackingId, Timestamp, JointCollection
		LATERAL VIEW JSON_TUPLE(BaseJSON.Location,'Longitude','Latitude') Coords
			AS Longitude, Latitude
		LATERAL VIEW JSON_TUPLE(BaseJSON.JointCollection,'FootLeft') OneJoint
			AS ThisJoint
		LATERAL VIEW JSON_TUPLE(OneJoint.ThisJoint,'Orientation','X','Y','Z') JointInfo
			AS Orientation, X, Y, Z;

INSERT INTO TABLE SkeletonTrack
SELECT	CAST(BaseJSON.TrackingId AS BIGINT) AS TrackingId,
		CAST(BaseJSON.KinectDeviceId AS BIGINT) AS KinectDeviceId,
		CAST(Coords.Longitude AS FLOAT) AS Longitude,
		CAST(Coords.Latitude AS FLOAT) AS Latitude,
		CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP) AS Timestamp,
		CASE
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 8) 
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 8 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 23) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':15:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 23 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 38) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':30:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 38 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 53)
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':45:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 53)
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
		END AS TimeInterval,
		HOUR(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) AS HourOfDay,
		'HipRight' AS Joint,
		CAST(JointInfo.X AS FLOAT) AS X,
		CAST(JointInfo.Y AS FLOAT) AS Y,
		CAST(JointInfo.Z AS FLOAT) AS Z,
		CAST(JointInfo.Orientation AS FLOAT) AS Orientation
FROM SkeletonTrackRaw st
		LATERAL VIEW JSON_TUPLE(st.raw_json,'KinectDeviceId','Location','TrackingId','TimeStamp','Joints') BaseJSON
			AS KinectDeviceId, Location, TrackingId, Timestamp, JointCollection
		LATERAL VIEW JSON_TUPLE(BaseJSON.Location,'Longitude','Latitude') Coords
			AS Longitude, Latitude
		LATERAL VIEW JSON_TUPLE(BaseJSON.JointCollection,'HipRight') OneJoint
			AS ThisJoint
		LATERAL VIEW JSON_TUPLE(OneJoint.ThisJoint,'Orientation','X','Y','Z') JointInfo
			AS Orientation, X, Y, Z;

INSERT INTO TABLE SkeletonTrack
SELECT	CAST(BaseJSON.TrackingId AS BIGINT) AS TrackingId,
		CAST(BaseJSON.KinectDeviceId AS BIGINT) AS KinectDeviceId,
		CAST(Coords.Longitude AS FLOAT) AS Longitude,
		CAST(Coords.Latitude AS FLOAT) AS Latitude,
		CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP) AS Timestamp,
		CASE
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 8) 
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 8 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 23) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':15:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 23 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 38) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':30:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 38 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 53)
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':45:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 53)
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
		END AS TimeInterval,
		HOUR(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) AS HourOfDay,
		'KneeRight' AS Joint,
		CAST(JointInfo.X AS FLOAT) AS X,
		CAST(JointInfo.Y AS FLOAT) AS Y,
		CAST(JointInfo.Z AS FLOAT) AS Z,
		CAST(JointInfo.Orientation AS FLOAT) AS Orientation
FROM SkeletonTrackRaw st
		LATERAL VIEW JSON_TUPLE(st.raw_json,'KinectDeviceId','Location','TrackingId','TimeStamp','Joints') BaseJSON
			AS KinectDeviceId, Location, TrackingId, Timestamp, JointCollection
		LATERAL VIEW JSON_TUPLE(BaseJSON.Location,'Longitude','Latitude') Coords
			AS Longitude, Latitude
		LATERAL VIEW JSON_TUPLE(BaseJSON.JointCollection,'KneeRight') OneJoint
			AS ThisJoint
		LATERAL VIEW JSON_TUPLE(OneJoint.ThisJoint,'Orientation','X','Y','Z') JointInfo
			AS Orientation, X, Y, Z;

INSERT INTO TABLE SkeletonTrack
SELECT	CAST(BaseJSON.TrackingId AS BIGINT) AS TrackingId,
		CAST(BaseJSON.KinectDeviceId AS BIGINT) AS KinectDeviceId,
		CAST(Coords.Longitude AS FLOAT) AS Longitude,
		CAST(Coords.Latitude AS FLOAT) AS Latitude,
		CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP) AS Timestamp,
		CASE
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 8) 
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 8 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 23) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':15:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 23 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 38) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':30:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 38 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 53)
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':45:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 53)
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
		END AS TimeInterval,
		HOUR(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) AS HourOfDay,
		'AnkleRight' AS Joint,
		CAST(JointInfo.X AS FLOAT) AS X,
		CAST(JointInfo.Y AS FLOAT) AS Y,
		CAST(JointInfo.Z AS FLOAT) AS Z,
		CAST(JointInfo.Orientation AS FLOAT) AS Orientation
FROM SkeletonTrackRaw st
		LATERAL VIEW JSON_TUPLE(st.raw_json,'KinectDeviceId','Location','TrackingId','TimeStamp','Joints') BaseJSON
			AS KinectDeviceId, Location, TrackingId, Timestamp, JointCollection
		LATERAL VIEW JSON_TUPLE(BaseJSON.Location,'Longitude','Latitude') Coords
			AS Longitude, Latitude
		LATERAL VIEW JSON_TUPLE(BaseJSON.JointCollection,'AnkleRight') OneJoint
			AS ThisJoint
		LATERAL VIEW JSON_TUPLE(OneJoint.ThisJoint,'Orientation','X','Y','Z') JointInfo
			AS Orientation, X, Y, Z;

INSERT INTO TABLE SkeletonTrack
SELECT	CAST(BaseJSON.TrackingId AS BIGINT) AS TrackingId,
		CAST(BaseJSON.KinectDeviceId AS BIGINT) AS KinectDeviceId,
		CAST(Coords.Longitude AS FLOAT) AS Longitude,
		CAST(Coords.Latitude AS FLOAT) AS Latitude,
		CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP) AS Timestamp,
		CASE
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 8) 
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 8 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 23) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':15:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 23 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 38) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':30:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 38 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 53)
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':45:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 53)
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
		END AS TimeInterval,
		HOUR(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) AS HourOfDay,
		'FootRight' AS Joint,
		CAST(JointInfo.X AS FLOAT) AS X,
		CAST(JointInfo.Y AS FLOAT) AS Y,
		CAST(JointInfo.Z AS FLOAT) AS Z,
		CAST(JointInfo.Orientation AS FLOAT) AS Orientation
FROM SkeletonTrackRaw st
		LATERAL VIEW JSON_TUPLE(st.raw_json,'KinectDeviceId','Location','TrackingId','TimeStamp','Joints') BaseJSON
			AS KinectDeviceId, Location, TrackingId, Timestamp, JointCollection
		LATERAL VIEW JSON_TUPLE(BaseJSON.Location,'Longitude','Latitude') Coords
			AS Longitude, Latitude
		LATERAL VIEW JSON_TUPLE(BaseJSON.JointCollection,'FootRight') OneJoint
			AS ThisJoint
		LATERAL VIEW JSON_TUPLE(OneJoint.ThisJoint,'Orientation','X','Y','Z') JointInfo
			AS Orientation, X, Y, Z;

INSERT INTO TABLE SkeletonTrack
SELECT	CAST(BaseJSON.TrackingId AS BIGINT) AS TrackingId,
		CAST(BaseJSON.KinectDeviceId AS BIGINT) AS KinectDeviceId,
		CAST(Coords.Longitude AS FLOAT) AS Longitude,
		CAST(Coords.Latitude AS FLOAT) AS Latitude,
		CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP) AS Timestamp,
		CASE
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 8) 
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 8 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 23) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':15:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 23 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 38) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':30:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 38 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 53)
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':45:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 53)
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
		END AS TimeInterval,
		HOUR(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) AS HourOfDay,
		'SpineShoulder' AS Joint,
		CAST(JointInfo.X AS FLOAT) AS X,
		CAST(JointInfo.Y AS FLOAT) AS Y,
		CAST(JointInfo.Z AS FLOAT) AS Z,
		CAST(JointInfo.Orientation AS FLOAT) AS Orientation
FROM SkeletonTrackRaw st
		LATERAL VIEW JSON_TUPLE(st.raw_json,'KinectDeviceId','Location','TrackingId','TimeStamp','Joints') BaseJSON
			AS KinectDeviceId, Location, TrackingId, Timestamp, JointCollection
		LATERAL VIEW JSON_TUPLE(BaseJSON.Location,'Longitude','Latitude') Coords
			AS Longitude, Latitude
		LATERAL VIEW JSON_TUPLE(BaseJSON.JointCollection,'SpineShoulder') OneJoint
			AS ThisJoint
		LATERAL VIEW JSON_TUPLE(OneJoint.ThisJoint,'Orientation','X','Y','Z') JointInfo
			AS Orientation, X, Y, Z;

INSERT INTO TABLE SkeletonTrack
SELECT	CAST(BaseJSON.TrackingId AS BIGINT) AS TrackingId,
		CAST(BaseJSON.KinectDeviceId AS BIGINT) AS KinectDeviceId,
		CAST(Coords.Longitude AS FLOAT) AS Longitude,
		CAST(Coords.Latitude AS FLOAT) AS Latitude,
		CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP) AS Timestamp,
		CASE
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 8) 
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 8 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 23) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':15:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 23 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 38) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':30:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 38 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 53)
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':45:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 53)
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
		END AS TimeInterval,
		HOUR(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) AS HourOfDay,
		'HandTipLeft' AS Joint,
		CAST(JointInfo.X AS FLOAT) AS X,
		CAST(JointInfo.Y AS FLOAT) AS Y,
		CAST(JointInfo.Z AS FLOAT) AS Z,
		CAST(JointInfo.Orientation AS FLOAT) AS Orientation
FROM SkeletonTrackRaw st
		LATERAL VIEW JSON_TUPLE(st.raw_json,'KinectDeviceId','Location','TrackingId','TimeStamp','Joints') BaseJSON
			AS KinectDeviceId, Location, TrackingId, Timestamp, JointCollection
		LATERAL VIEW JSON_TUPLE(BaseJSON.Location,'Longitude','Latitude') Coords
			AS Longitude, Latitude
		LATERAL VIEW JSON_TUPLE(BaseJSON.JointCollection,'HandTipLeft') OneJoint
			AS ThisJoint
		LATERAL VIEW JSON_TUPLE(OneJoint.ThisJoint,'Orientation','X','Y','Z') JointInfo
			AS Orientation, X, Y, Z;

INSERT INTO TABLE SkeletonTrack
SELECT	CAST(BaseJSON.TrackingId AS BIGINT) AS TrackingId,
		CAST(BaseJSON.KinectDeviceId AS BIGINT) AS KinectDeviceId,
		CAST(Coords.Longitude AS FLOAT) AS Longitude,
		CAST(Coords.Latitude AS FLOAT) AS Latitude,
		CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP) AS Timestamp,
		CASE
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 8) 
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 8 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 23) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':15:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 23 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 38) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':30:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 38 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 53)
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':45:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 53)
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
		END AS TimeInterval,
		HOUR(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) AS HourOfDay,
		'ThumbLeft' AS Joint,
		CAST(JointInfo.X AS FLOAT) AS X,
		CAST(JointInfo.Y AS FLOAT) AS Y,
		CAST(JointInfo.Z AS FLOAT) AS Z,
		CAST(JointInfo.Orientation AS FLOAT) AS Orientation
FROM SkeletonTrackRaw st
		LATERAL VIEW JSON_TUPLE(st.raw_json,'KinectDeviceId','Location','TrackingId','TimeStamp','Joints') BaseJSON
			AS KinectDeviceId, Location, TrackingId, Timestamp, JointCollection
		LATERAL VIEW JSON_TUPLE(BaseJSON.Location,'Longitude','Latitude') Coords
			AS Longitude, Latitude
		LATERAL VIEW JSON_TUPLE(BaseJSON.JointCollection,'ThumbLeft') OneJoint
			AS ThisJoint
		LATERAL VIEW JSON_TUPLE(OneJoint.ThisJoint,'Orientation','X','Y','Z') JointInfo
			AS Orientation, X, Y, Z;

INSERT INTO TABLE SkeletonTrack
SELECT	CAST(BaseJSON.TrackingId AS BIGINT) AS TrackingId,
		CAST(BaseJSON.KinectDeviceId AS BIGINT) AS KinectDeviceId,
		CAST(Coords.Longitude AS FLOAT) AS Longitude,
		CAST(Coords.Latitude AS FLOAT) AS Latitude,
		CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP) AS Timestamp,
		CASE
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 8) 
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 8 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 23) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':15:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 23 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 38) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':30:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 38 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 53)
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':45:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 53)
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
		END AS TimeInterval,
		HOUR(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) AS HourOfDay,
		'HandTipRight' AS Joint,
		CAST(JointInfo.X AS FLOAT) AS X,
		CAST(JointInfo.Y AS FLOAT) AS Y,
		CAST(JointInfo.Z AS FLOAT) AS Z,
		CAST(JointInfo.Orientation AS FLOAT) AS Orientation
FROM SkeletonTrackRaw st
		LATERAL VIEW JSON_TUPLE(st.raw_json,'KinectDeviceId','Location','TrackingId','TimeStamp','Joints') BaseJSON
			AS KinectDeviceId, Location, TrackingId, Timestamp, JointCollection
		LATERAL VIEW JSON_TUPLE(BaseJSON.Location,'Longitude','Latitude') Coords
			AS Longitude, Latitude
		LATERAL VIEW JSON_TUPLE(BaseJSON.JointCollection,'HandTipRight') OneJoint
			AS ThisJoint
		LATERAL VIEW JSON_TUPLE(OneJoint.ThisJoint,'Orientation','X','Y','Z') JointInfo
			AS Orientation, X, Y, Z;

INSERT INTO TABLE SkeletonTrack
SELECT	CAST(BaseJSON.TrackingId AS BIGINT) AS TrackingId,
		CAST(BaseJSON.KinectDeviceId AS BIGINT) AS KinectDeviceId,
		CAST(Coords.Longitude AS FLOAT) AS Longitude,
		CAST(Coords.Latitude AS FLOAT) AS Latitude,
		CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP) AS Timestamp,
		CASE
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 8) 
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 8 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 23) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':15:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 23 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 38) 
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':30:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 38 
				AND MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) < 53)
					THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':45:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) >= 53)
				THEN CONCAT(SUBSTR(BaseJSON.Timestamp,12,2), ':00:00')
		END AS TimeInterval,
		HOUR(CAST(CONCAT(SUBSTR(BaseJSON.Timestamp,0,10), ' ',SUBSTR(BaseJSON.Timestamp,12,10)) AS TIMESTAMP)) AS HourOfDay,
		'ThumbRight' AS Joint,
		CAST(JointInfo.X AS FLOAT) AS X,
		CAST(JointInfo.Y AS FLOAT) AS Y,
		CAST(JointInfo.Z AS FLOAT) AS Z,
		CAST(JointInfo.Orientation AS FLOAT) AS Orientation
FROM SkeletonTrackRaw st
		LATERAL VIEW JSON_TUPLE(st.raw_json,'KinectDeviceId','Location','TrackingId','TimeStamp','Joints') BaseJSON
			AS KinectDeviceId, Location, TrackingId, Timestamp, JointCollection
		LATERAL VIEW JSON_TUPLE(BaseJSON.Location,'Longitude','Latitude') Coords
			AS Longitude, Latitude
		LATERAL VIEW JSON_TUPLE(BaseJSON.JointCollection,'ThumbRight') OneJoint
			AS ThisJoint
		LATERAL VIEW JSON_TUPLE(OneJoint.ThisJoint,'Orientation','X','Y','Z') JointInfo
			AS Orientation, X, Y, Z;
