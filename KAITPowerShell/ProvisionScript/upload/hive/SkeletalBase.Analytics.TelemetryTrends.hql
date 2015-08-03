DROP TABLE IF EXISTS TelemetryTrends;
CREATE TABLE TelemetryTrends
(
	KinectDeviceId BIGINT,
	Day STRING,
	Hour INT,
	TimeInterval STRING,
	PersonCount INT,
	AvgX FLOAT,
	AvgY FLOAT,
	AvgZ FLOAT,
	AvgOrientation FLOAT
);

INSERT INTO TABLE TelemetryTrends
SELECT 	st.KinectDeviceId AS KinectDeviceId,
		TO_DATE(CONCAT(SUBSTR(CAST(st.Timestamp AS STRING),0,10), ' ',SUBSTR(CAST(st.Timestamp AS STRING),12,8))) AS Day,
		HOUR(st.Timestamp) AS Hour,
		st.TimeInterval AS TimeInterval,
		COALESCE(COUNT(DISTINCT st.TrackingId),0L) AS PersonCount,
		AVG(st.X) AS AvgX,
		AVG(st.Y) AS AvgY,
		AVG(st.Z) AS AvgZ,
		AVG(st.Orientation) AS AvgOrientation
FROM SkeletonTrack st
WHERE st.Joint='Head'
GROUP BY st.KinectDeviceId, TO_DATE(CONCAT(SUBSTR(CAST(st.Timestamp AS STRING),0,10), ' ',SUBSTR(CAST(st.Timestamp AS STRING),12,8))), HOUR(st.Timestamp), st.TimeInterval;
