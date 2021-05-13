
-- eine temporäre Tabelle erstellen
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'temp_constraintstatus') AND type in (N'U'))
BEGIN 
 DROP TABLE temp_constraintstatus
END

CREATE TABLE temp_constraintstatus ( 
[Table_name] varchar(200), 
[Constraint_name] varchar(200), 
[Where] varchar(200) 
) 
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'temp_constraintstatus') AND type in (N'U'))
BEGIN 
 DROP TABLE temp_constraintstatus
END

