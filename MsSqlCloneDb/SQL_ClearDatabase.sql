            /* Drop table constraints which are nor FK, nor PK constraints */
            PRINT 'Dropping non FK/non PK table constraints'
            DECLARE @name VARCHAR(128)
            DECLARE @SQL VARCHAR(254)
            DECLARE @schema VARCHAR(128)
            DECLARE @constraint VARCHAR(254)
            
            SELECT @name = (SELECT TOP 1 '[' + RTRIM(TABLE_SCHEMA) + '].[' +  + RTRIM(TABLE_NAME) + ']'      
                              FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS 
                             WHERE constraint_catalog=DB_NAME() 
                               AND NOT CONSTRAINT_TYPE IN ('FOREIGN KEY', 'PRIMARY KEY')
                          ORDER BY TABLE_NAME)
            SELECT @schema = (SELECT TOP 1 TABLE_SCHEMA 
                                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
                               WHERE constraint_catalog=DB_NAME() 
                                 AND CONSTRAINT_TYPE <> 'FOREIGN KEY' 
                            ORDER BY TABLE_NAME)
            
            WHILE @name is not null
            BEGIN
                PRINT 'Dropping non FK/non PK table Constraints on table ' + @name
                SELECT @constraint = (SELECT TOP 1 CONSTRAINT_NAME 
                                        FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS 
                                       WHERE constraint_catalog=DB_NAME() 
                                         AND NOT CONSTRAINT_TYPE IN ('FOREIGN KEY', 'PRIMARY KEY') 
                                         AND '[' + RTRIM(TABLE_SCHEMA) + '].[' +  + RTRIM(TABLE_NAME) + ']' = @name 
                                    ORDER BY CONSTRAINT_NAME) 
            
                WHILE @constraint IS NOT NULL 
                BEGIN 
                
                    SELECT @SQL = 'ALTER TABLE ' + @name +' DROP CONSTRAINT ' + RTRIM(@constraint) 
                    EXEC (@SQL) 
                    PRINT 'Dropped non FK Constraint: ' + @constraint + ' on ' + @name 
                    SET @constraint=NULL
                    SELECT @constraint = (SELECT TOP 1 CONSTRAINT_NAME 
                                            FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS 
                                           WHERE constraint_catalog=DB_NAME() 
                                             AND NOT CONSTRAINT_TYPE IN ('FOREIGN KEY', 'PRIMARY KEY') 
                                             AND CONSTRAINT_NAME <> @constraint 
                                             AND '[' + RTRIM(TABLE_SCHEMA) + '].[' +  + RTRIM(TABLE_NAME) + ']' = @name 
                                        ORDER BY CONSTRAINT_NAME) 
                END -- WHILE @constraint IS NOT NULL
                
                SET @name=NULL
                SELECT @name = (SELECT TOP 1 '[' + RTRIM(TABLE_SCHEMA) + '].[' +  + RTRIM(TABLE_NAME) + ']' 
                                  FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS 
                                 WHERE constraint_catalog=DB_NAME() 
                                   AND NOT CONSTRAINT_TYPE IN ('FOREIGN KEY', 'PRIMARY KEY') 
                              ORDER BY TABLE_NAME)
                SELECT @schema = (SELECT TOP 1 TABLE_SCHEMA 
                                    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS 
                                   WHERE constraint_catalog=DB_NAME() 
                                     AND NOT CONSTRAINT_TYPE IN ('FOREIGN KEY', 'PRIMARY KEY') 
                                   ORDER BY TABLE_NAME)
            END -- WHILE @name is not null            
            
GO
            
            /* Drop all non-system stored procs */
            PRINT 'Dropping Procedures'
            DECLARE @name VARCHAR(128)
            DECLARE @SQL VARCHAR(254)
            DECLARE @schema VARCHAR(128)
            
            SELECT @name = (SELECT TOP 1 [name] 
                              FROM sysobjects 
                             WHERE [type] = 'P' 
                               AND category = 0 
                          ORDER BY [name])
            SELECT @schema = (SELECT TOP 1 sys.schemas.name 
                                FROM sys.objects  
                          INNER JOIN sys.schemas 
                                  ON sys.objects.schema_id = sys.schemas.schema_id
                                 AND sys.objects.name = @name)
                                 
            WHILE @name is not null
            BEGIN
                SELECT @SQL = 'DROP PROCEDURE [' + RTRIM(@schema) +'].[' + RTRIM(@name) +']' 
                EXEC (@SQL) 
                PRINT 'Dropped Procedure: ' + @schema +'.' + @name 
                SET @name=NULL
                SELECT @name = (SELECT TOP 1 [name] 
                                  FROM sysobjects 
                                 WHERE [type] = 'P' 
                                   AND category = 0 
                              ORDER BY [name]) 
                SELECT @schema = (SELECT TOP 1 sys.schemas.name 
                                    FROM  sys.objects  
                              INNER JOIN sys.schemas 
                                      ON sys.objects.schema_id = sys.schemas.schema_id
                                     AND sys.objects.name = @name)
            END -- WHILE @name is not null
            
GO
            
            /* Drop all views */
            PRINT 'Dropping Views'
            DECLARE @name VARCHAR(128)
            DECLARE @SQL VARCHAR(254)
            DECLARE @schema VARCHAR(128)
            
            SELECT @name = (SELECT TOP 1 [name] 
                              FROM sysobjects 
                             WHERE [type] = 'V' 
                               AND category = 0 
                          ORDER BY [name])
            SELECT @schema = (SELECT TOP 1 sys.schemas.name 
                                FROM  sys.objects 
                          INNER JOIN sys.schemas 
                                  ON sys.objects.schema_id = sys.schemas.schema_id
                                 AND sys.objects.name = @name)
            
            WHILE @name IS NOT NULL
            BEGIN
                IF NOT EXISTS(SELECT 1
                                FROM sys.sql_dependencies dep
                               WHERE dep.class_desc = 'OBJECT_OR_COLUMN_REFERENCE_SCHEMA_BOUND'
                                 AND dep.referenced_major_id = (SELECT id 
                                                                  FROM sysobjects so
                                                                 WHERE so.[type] = 'V' 
                                                                   AND so.category = 0 
                                                                   AND so.name = @name))
                BEGIN
                    SELECT @SQL = 'DROP VIEW [' + RTRIM(@schema) +'].[' + RTRIM(@name) +']' 
                    EXEC (@SQL) 
                    PRINT 'Dropped View: ' + @name 
                    SET @name=NULL
                    SELECT @name = (SELECT TOP 1 [name] 
                                      FROM sysobjects 
                                     WHERE [type] = 'V' 
                                       AND category = 0 
                                  ORDER BY [name]) 
                    
                END
                ELSE BEGIN
                   PRINT 'Skipped View: ' + @name 
                   SELECT @name = (SELECT TOP 1 [name] 
                                      FROM sysobjects 
                                     WHERE [type] = 'V' 
                                       AND category = 0 
                                       AND [name] > @name
                                  ORDER BY [name]) 
                    
                END

                SELECT @schema = (SELECT TOP 1 sys.schemas.name 
                                    FROM sys.objects  
                              INNER JOIN sys.schemas 
                                      ON sys.objects.schema_id = sys.schemas.schema_id
                                     AND sys.objects.name = @name)
            END -- WHILE @name IS NOT NULL
            
GO
            
	/* Drop all functions */
	/* ****************************************************************
	Da die Fuktionen von anderen Funktionen abhändig sein können (Beispiel: ' ErwarteterLieferterminCalculator' hängt von 'AddWorkdaysCalculator' ab)
	so können die Funktionen nicht gelöschtwerden
	Um das Problem umzugehen, wird nun doppelte Schleife benutzt
	*****************************************************************  */
            PRINT 'Dropping Functions'
			declare namesCursor cursor for
				SELECT [name] 
                              FROM sysobjects 
                             WHERE [type] IN (N'FN', N'IF', N'TF', N'FS', N'FT') 
                               AND category = 0 
                          ORDER BY [name]
            DECLARE @name VARCHAR(128)
            DECLARE @SQL VARCHAR(254)
            DECLARE @schema VARCHAR(128)
            
			open namesCursor
			fetch next from namesCursor into @name
            SELECT @schema = (SELECT TOP 1 sys.schemas.name 
                                FROM sys.objects  
                          INNER JOIN sys.schemas 
                                  ON sys.objects.schema_id = sys.schemas.schema_id
                                 AND sys.objects.name = @name)
            WHILE @name IS NOT NULL
            BEGIN
            SELECT @SQL = 'DROP FUNCTION [' + RTRIM(@schema) +'].[' + RTRIM(@name) +']' 
				begin try
					EXEC (@SQL)
				end try
				begin catch
					print 'Error while dropping Function: ' + @name + ': ' + ERROR_MESSAGE()
				end catch
                PRINT 'Dropped Function: ' + @name 
                SET @name=NULL
				fetch next from namesCursor into @name
				if @name is null 
				begin
					close namesCursor
					open namesCursor
					fetch next from namesCursor into @name
				end

                 SELECT @schema = (SELECT TOP 1 sys.schemas.name 
                                    FROM sys.objects  
                              INNER JOIN sys.schemas 
                                      ON sys.objects.schema_id = sys.schemas.schema_id
                                     AND sys.objects.name = @name)
            END -- WHILE @name IS NOT NULL
 			close namesCursor
			deallocate namesCursor
            
GO
            
            /* Drop all Foreign Key constraints */
            PRINT 'Dropping FK Constraints'
            DECLARE @name VARCHAR(128)
            DECLARE @schema VARCHAR(128)
            DECLARE @constraint VARCHAR(254)
            DECLARE @SQL VARCHAR(254)
            
            SELECT @name = (SELECT TOP 1 '[' + RTRIM(TABLE_SCHEMA) + '].[' +  + RTRIM(TABLE_NAME) + ']' 
                              FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS 
                             WHERE constraint_catalog=DB_NAME() 
                               AND CONSTRAINT_TYPE = 'FOREIGN KEY' 
                          ORDER BY TABLE_NAME)
            SELECT @schema = (SELECT TOP 1 TABLE_SCHEMA 
                                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS 
                               WHERE constraint_catalog=DB_NAME() 
                                 AND CONSTRAINT_TYPE = 'FOREIGN KEY' 
                            ORDER BY TABLE_NAME)
            WHILE @name is not null
            BEGIN
                PRINT 'Dropping FK Constraints on table ' + @name
                SELECT @constraint = (SELECT TOP 1 CONSTRAINT_NAME 
                                        FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS 
                                       WHERE constraint_catalog=DB_NAME() 
                                         AND CONSTRAINT_TYPE = 'FOREIGN KEY' 
                                         AND '[' + RTRIM(TABLE_SCHEMA) + '].[' +  + RTRIM(TABLE_NAME) + ']' = @name 
                                    ORDER BY CONSTRAINT_NAME) 
            
                WHILE @constraint IS NOT NULL 
                BEGIN 
                    SELECT @SQL = 'ALTER TABLE ' + @name +' DROP CONSTRAINT [' + RTRIM(@constraint) + ']'
                    EXEC (@SQL) 
                    PRINT 'Dropped FK Constraint: ' + @constraint + ' on ' + @name 
                    SET @constraint=NULL
                    SELECT @constraint = (SELECT TOP 1 CONSTRAINT_NAME 
                                            FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS 
                                           WHERE constraint_catalog=DB_NAME() 
                                             AND CONSTRAINT_TYPE = 'FOREIGN KEY' 
                                             AND CONSTRAINT_NAME <> @constraint 
                                             AND '[' + RTRIM(TABLE_SCHEMA) + '].[' +  + RTRIM(TABLE_NAME) + ']' = @name 
                                        ORDER BY CONSTRAINT_NAME) 
                END -- WHILE @constraint IS NOT NULL 
                
                SET @name=NULL
                SELECT @name = (SELECT TOP 1 '[' + RTRIM(TABLE_SCHEMA) + '].[' +  + RTRIM(TABLE_NAME) + ']' 
                                  FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS 
                                 WHERE constraint_catalog=DB_NAME() 
                                   AND CONSTRAINT_TYPE = 'FOREIGN KEY' 
                              ORDER BY TABLE_NAME)
                SELECT @schema = (SELECT TOP 1 TABLE_SCHEMA 
                                    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS 
                                   WHERE constraint_catalog=DB_NAME() 
                                     AND CONSTRAINT_TYPE = 'FOREIGN KEY' 
                                ORDER BY TABLE_NAME)
            END -- WHILE @name is not null
            
GO
            
            /* Drop all tables */
            PRINT 'Dropping Tables'
            
            DECLARE @name VARCHAR(128)
            DECLARE @SQL VARCHAR(254)
            DECLARE @schema VARCHAR(128)
            
            SELECT @name = (SELECT TOP 1 '[' + RTRIM(SCHEMA_NAME(schema_id)) + '].[' + RTRIM([name]) + ']' 
                              FROM sys.tables 
                             WHERE type_desc='USER_TABLE' 
                          ORDER BY [name])
            SELECT @schema = (SELECT TOP 1 sys.schemas.name 
                                FROM sys.objects
                          INNER JOIN sys.schemas 
                                  ON sys.objects.schema_id = sys.schemas.schema_id
                                 AND sys.objects.name = @name)
            
            WHILE @name IS NOT NULL
            BEGIN
                SELECT @SQL = 'DROP TABLE ' + RTRIM(@name) 
                EXEC (@SQL) 
                PRINT 'Dropped Table: ' + @schema + '.' + @name 
                SELECT @name = (SELECT TOP 1 '[' + RTRIM(SCHEMA_NAME(schema_id)) + '].[' + RTRIM([name]) + ']' 
                                  FROM sys.tables 
                                 WHERE type_desc='USER_TABLE' 
                              ORDER BY [name])
                SELECT @schema = (SELECT TOP 1 sys.schemas.name 
                                    FROM sys.objects 
                              INNER JOIN sys.schemas 
                                      ON sys.objects.schema_id = sys.schemas.schema_id
                                     AND sys.objects.name = @name)
            END
            
GO
            