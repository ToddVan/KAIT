DROP VIEW IF EXISTS InteractionsWithinOneMetersView;
CREATE VIEW InteractionsWithinOneMetersView
AS
SELECT *
FROM InteractionsFull
WHERE Z < 1 AND ABS(X) < 1;


DROP VIEW IF EXISTS InteractionsWithinTwoMetersView;
CREATE VIEW InteractionsWithinTwoMetersView
AS
SELECT *
FROM InteractionsFull
WHERE Z < 2 AND ABS(X) < 2;


DROP VIEW IF EXISTS InteractionsWithinThreeMetersView;
CREATE VIEW InteractionsWithinThreeMetersView
AS
SELECT *
FROM InteractionsFull
WHERE Z < 3 AND ABS(X) < 3;


DROP VIEW IF EXISTS InteractionsWithinFourMetersView;
CREATE VIEW InteractionsWithinFourMetersView
AS
SELECT *
FROM InteractionsFull
WHERE Z < 4 AND ABS(X) < 4;


-- Returns all skeleton tracking frames that look at the display (Within 10 degrees left or right of display)
DROP VIEW IF EXISTS SkeletonsLookingAtDisplayView;
CREATE VIEW SkeletonsLookingAtDisplayView
AS
SELECT 	s.TrackingId,
		s.Timestamp,
		s.HourOfDay,
        s.Joint,
		s.X,
		s.Y,
		s.Z,
		s.Orientation
FROM SkeletonTrack s
WHERE (ABS(s.Orientation) < (1/90)*10) AND s.Joint IN('Head','SpineMid','HandLeft','HandRight') AND s.Orientation <> 0;

