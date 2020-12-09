
set exefolder=./bin/debug
REM climatedb.database.windows.net;Initial Catalog=ClimateSqlDB;Persist Security Info=False;User ID=climatedbadmin;Password=jnfpe8rzhWGHw9fz209zt52ttg+;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
set dbSource="Data Source=climatedb.database.windows.net;Initial Catalog=ClimateSqlDB;Persist Security Info=False;User ID=climatedbadmin;Password=jnfpe8rzhWGHw9fz209zt52ttg+;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
set dbTarget="Data Source=localhost;Initial Catalog=climo;Persist Security Info=True;User ID=ICLx_Admin;Password=1qay2wsx;MultipleActiveResultSets=False"

@echo exefolder     : %exefolder%
@echo dbSource      : %dbSource% 
@echo dbTarget      : %dbTarget% 

pause

cd ./%exefolder%

MsSqlCloneDb.exe ^
 -dbSource=%dbSource% ^
 -dbTarget=%dbTarget% ^
 -restoreTables="" ^
 -mergeTables="" ^
 -mergeScripts="" ^
 -finalScripts=""
 
pause