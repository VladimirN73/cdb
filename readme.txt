Clone-Tool

 Help info see in .\cdb.ConsoleApp\_help.txt

####################################################### 
######		Open Points (Todos) 			###########
####################################################### 
-------------------------------------------------------
Open Points (Todos)
-------------------------------------------------------
*. port it to .net5
*. Feature - Transfer Data for some tables. Do not re-create the target DB. Example: Transfer only SWIP Tables from Test to Int.
*. ConnectionString for Prod DB - do not use Admin-user, consider to use only a Read-user, to exclude risk of DB damage
*. Feature - ignore Command
   ExecuteCommand: ALTER INDEX ALL ON [dbo].[V_ValidationRequirementPlatformVariantCrossJoin] REBUILD
   weil das Kommando schlaegt immer fehl und verbraucht viel Zeit (über 5 Minuten) 
*. Wie soll die App reagieren falls unbekannte Parameter/Optionen angegeben werden (Abbrechen vs Ignorieren) 
*. Wie soll die App reagieren falls unbekannte Tabellen in Parameter 'skipTables' und 'restoreTables' angegeben sind
*. Wie soll die App reagieren falls unbekannte Skripte in Parameter 'finalScripts' angegeben sind
*. Feature - re-use available Schema-File (parameter -reuseSchema ?)
*. parameter 'initScripts' - scripts before db modifications, to check for example if the maintanence mode is activated
*. Exclusively lock the target DB to enable clone on dbs for automated tests
*. Feature - check the result of script - usually "select 1 from ...". Use a #temp table?....


NOT IMPLEMENTED IN THIS VERSION
* merge
* partialtransfer
* decript parameters
* ReplaceVariablesInFinalScripts

-------------------------------------------------------
####################################################### 
######				History 				###########
####################################################### 
-------------------------------------------------------
08.04.21
 add initial DI and Configuration
 
06.04.21 
 review project. Todo - port it to .net5. Review architecture and project structure

09.12.20 
 push to github

06.10.20 deactivate some step to enable it for OCC&TeaBox projects

03.11.19
 Add Variable #{TargetDB}#, so that we can create global.Mandant using the script SQL_Create_global_Mandant.sql.
 The idea is to replace "restoretables=globalMandant" by "finalScripts=SQL_Create_global_Mandant.sql"
 Why? During the Clone-Process an error can happen and so the target DB can be broken and the restore not possible
26.05.19
 Enable parallel execution => generierte Dateien (Schema, Backup-Sripte, ..) haben eine ID-Nummer, 
 somit kann das DB-Tool mehrmals parallel gestartet werden.
09.03.19
 Extend the Property 'skipTable' in App.Config - include Session and SessionValue
 Adapt the function 'PrintParameters' - print the content of the Lists (restoretables, updateScripts, etc) 
02.03.19
 Add SimpleTextProcessor - to replace Variables. #{SourceDB}#
11.02.19
 Add fragment 'SET DATEFORMAT YMD' into backup_restore scripts
 to avoid the issue 'Bei der Konvertierung eines nvarchar-Datentyps in einen datetime-Datentyp liegt der Wert außerhalb des gültigen Bereichs.'
08.02.19
 Add re-try for GenerateTables, to fix (workaround) the time-out issue:
 Ermittle CREATE Table 'SolisImportEkAenderung'
 Fehler/Error:Microsoft.SqlServer.Management.Smo.FailedOperationException: 
 Script failed for Table 'dbo.SolisImportEkAenderung'.  
 ---> Microsoft.SqlServer.Management.Common.ExecutionFailureException:  An exception occurred while executing a Transact-SQL statement or batch. 
 ---> System.Data.SqlClient.SqlException: Execution Timeout Expired.  The timeout period elapsed prior to completion of the operation or the server is not responding. 
 ---> System.ComponentModel.Win32Exception: The wait operation timed out
04.02.19
 Exit with Error if 'RestoreBackup' or 'RestoreBackupForMerge' are failed
21.12.18
 add scripts SQL_Adapt_Clone_Prod.sql and SQL_Create_TV3.sql
19.12.18
 add script 'SQL_Delete_Schema_global.sql'
06.12.18
 Rename final scripts into SQL_Final_xxx.sql, so the configuration still simple
23.11.18
  Create MsSqlCloneDb.Lib, move the 'core' functionality from MsSqlCloneDb to MsSqlCloneDb.Lib
26.09.18
  return Error if an update-script throws an exception
24.09.18 
  parameter 'updateScripts' is implemented
  Version is shown in Console and Windows mode
  WinForm is improved - resize behaviour
04.09.18
  working on feature 'to clone only if the dbSource is provided'
24.08.18
  remove parameters db[Target|Source]Connectionstring
23.08.18 
  add processing skiptables="*"
  add VersionInfo.cs
  simplify logging
  add 'return ExitCode' so the ExitCode can be evaluated in BATCH processing
  remove files SQL_4_1_050_*
  remove files SQL_4_1_110_*
  remove some users from SQL_Activate_Master

-------------------------------------------------------
####################################################### 
######		Open Points (Todos) 			###########
####################################################### 
-------------------------------------------------------
Open Points (Todos)
-------------------------------------------------------
 x. ConnectionString for Prod DB - do not use Admin-user, consider to use only a Read-user, to exclude risk of DB damage
 x. Issue with Date covert
    INSERT [global].[Mandant] ([MandantId], [Key], [Name], [ExpiresOn], [IsActive], [ConnectionString], [_CreatedBy], [_CreateDate], [_ModifiedBy], [_ModifyDate]) VALUES (11, N'DEV', N'DB_Master', NULL, 1, N'Data Source=localhost;Initial Catalog=DB_Master;Max Pool Size=10;Persist Security Info=True;User ID=DB_Admin;Password=***;MultipleActiveResultSets=true;', N'INITIAL', CAST(N'2018-09-25 14:47:58.450' AS DateTime), N'INITIAL', CAST(N'2018-09-25 14:47:58.450' AS DateTime))
	Meldung: Bei der Konvertierung eines nvarchar-Datentyps in einen datetime-Datentyp liegt der Wert außerhalb des gültigen Bereichs.
 x. Feature - ignore Command
    ExecuteCommand: ALTER INDEX ALL ON [dbo].[V_ValidationRequirementPlatformVariantCrossJoin] REBUILD
	weil das Kommando schlaegt immer fehl und verbraucht viel Zeit (über 5 Minuten) 
 1. done- Password in dbSourceConnectionString und dbTargetConnectionString verschlüsseln
 2. Wie soll die App reagieren falls unbekannte Parameter/Optionen angegeben werden (Abbrechen vs Ignorieren) 
 3. Wie soll die App reagieren falls unbekannte Tabellen in Parameter 'skipTables' und 'restoreTables' angegeben sind
 4. Wie soll die App reagieren falls unbekannte Skripte in Parameter 'finalScripts' angegeben sind
 5. done - Rewrite/remove Connection to PROD-DB (DB-9750)
 6. done - parameter 'updateScripts' - to apply update scripts
 7. done - run Update Scripts (DB-9440) eventuell vor dem Merge um den Merge zu ermöglichen
    Auch 'restoreTables' benötigen die UpdateScripts ...
 8. done - Feature - create only Schema-File (parameter -schemaOnly ? or just do not provide target?)
 9. Feature - re-use available Schema-File (parameter -resuseSchema ?)
10. parameter 'initScripts' - scripts before db modifications, to check for example if the maintanence mode is activated
11. done - finalScripts - modify the connection strings if cloned from Prod-Db 
12. Exclusively lock the target DB to enable clone on dbs for automated tests
13. Feature - check the result of script - usually "select 1 from ...". Use a #temp table?....
14. Feature - nur bestimmte Tabellen übertragen, z.B. Performance ... re-use parameter -skipTables with negotiation?
15. Feature - nur eine Tabellen portionsweise übertragen, z.B. Performance  ab ID=xxx
16. Remove dependencies/references to DB