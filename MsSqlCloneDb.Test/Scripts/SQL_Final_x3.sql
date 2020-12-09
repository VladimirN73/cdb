
-- -----------------------------------------------------------------
-- Set Auto-Increment
--
-- Ticket: ICLX-8874
-- -----------------------------------------------------------------
DECLARE @counter int;
SET @counter = (SELECT TOP 1 TaskAusfuehrungId FROM [Tasks].[TaskAusfuehrungHistory] ORDER BY TaskAusfuehrungId DESC);
IF @counter IS NOT NULL BEGIN SET @counter=@counter + 1; DBCC CHECKIDENT('[Tasks].[TaskAusfuehrung]', RESEED, @counter);END;

GO

-- -----------------------------------------------------------------
-- Check/Fix Constraints
--
-- Ticket: ICLX-8825
-- -----------------------------------------------------------------

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

-- Die Informationen über verletzen Contraints sammeln
INSERT INTO temp_constraintstatus EXEC ('DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS' ) 


-- Verletze Contraints abbarbeiten/korrigieren
declare myCursor cursor for
 SELECT [Table_Name], [where] FROM temp_constraintstatus 
 DECLARE @tableName  VARCHAR(max)
 DECLARE @tableWhere VARCHAR(max)
 DECLARE @SQL        VARCHAR(max)
            
 open myCursor
 fetch next from myCursor into @tableName, @tableWhere
 
 WHILE @@FETCH_STATUS = 0 
 BEGIN
  SELECT @SQL = 'DELETE FROM ' + @tableName +' WHERE ' + @tableWhere
  EXEC (@SQL) 
  fetch next from myCursor into @tableName, @tableWhere
 END 
close myCursor
deallocate myCursor

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'temp_constraintstatus') AND type in (N'U'))
BEGIN 
 DROP TABLE temp_constraintstatus
END

