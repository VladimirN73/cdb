REM =====================================================================================
REM Example of the Batch to clone the remote ICLx_Clone_Prod to ICLx_Local_Prod
REM
REM Pre-Requirements
REM 1. Es gibt die Datenbank smuc2299.muc\SQLEXPR2014,8885, Initial Catalog=ICLx_Clone_Prod
REM 2. Es gibt die Datenbank localhost, Initial Catalog=ICLx_Local_Prod
REM    ALTER DATABASE ICLx_Local_Prod
REM    SET ALLOW_SNAPSHOT_ISOLATION ON
REM    ALTER DATABASE ICLx_Local_Prod SET compatibility_level = 110
REM
REM Before start
REM * recheck DB Source connection string
REM
REM Final steps
REM * ...
REM =====================================================================================

set start_all=%time%

MsSqlCloneDb.exe  ^
 -dbSource="Data Source=smuc2299.muc\SQLEXPR2014,8885;Network Library=DBMSSOCN;Initial Catalog=ICLx_Clone_Prod;  Max Pool Size=10;Persist Security Info=True;User ID=ICLx_Admin;Password=1qay2wsx;MultipleActiveResultSets=False" ^
 -dbTarget="Data Source=DANIELICKES1049\SQLEXPRESS;                            Initial Catalog=ICLx_Local_Master;Max Pool Size=10;Persist Security Info=True;User ID=ICLx_Admin;Password=1qay2wsx;MultipleActiveResultSets=False" ^
 -skipTables="Job,JobHistory,IndicatorRawData,TaskAusfuehrung,TaskAusfuehrungQueue,TaskAusfuehrungHistory,TaskMeldungDatensatz,TaskMeldungDatensatzVorkommnis,TaskMeldungDatensatzVorkommnisDetail,TaskMeldungVorgang" ^
 -restoreTables="" ^
 -mergeTables="" ^
 -mergeScripts="" ^
 -finalScripts="SQL_FixDatabase.sql"
                  

REM Copy SQLs and Log into separate folder
set logfolder="Trace_%date:~6,4%_%date:~3,2%_%date:~0,2%_%time:~0,2%%time:~3,2%_CloneProd_on_Local_Prod"
set logfolder=%logfolder: =0%
echo %logfolder%
mkdir %logfolder%

copy .\*.sql .\%logfolder%
copy .\*.log .\%logfolder%
del .\Trace.log

				  
set end_all=%time%
echo STARTTIME: %start_all%
echo ENDTIME  : %end_all%

pause