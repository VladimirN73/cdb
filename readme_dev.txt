Clone-Tool

Help info see in .\cdb.ConsoleApp\_help.txt

github: https://github.com/VladimirN73/cdb

-------------------------------------------------------
Open Points (see also issues in github)
-------------------------------------------------------
*. iTest - ClearDatabase - ensure Exception if Clear.sql is not found
*. Feature - Transfer Data for some tables. Do not re-create the target DB. Example: Transfer only SWIP Tables from Test to Int.
*. ConnectionString for Prod DB - do not use Admin-user, consider to use only a Read-user, to exclude risk of DB damage
*. Feature - ignore Command, due to some command (like ALTER INDEX ALL ON [dbo].[V_xxx]) are not important but take a lot of tame  
*. Feature - re-use available Schema-File (parameter -reuseSchema ?)
*. parameter 'initScripts' - scripts before db modifications, to check for example if the maintanence mode is activated
*. Exclusively lock the target DB to enable clone on dbs for automated tests
*. Feature - check the result of script - usually "select 1 from ...". Use a #temp table?....
*. merge
*. partialtransfer
*. decript parameters
*. ReplaceVariablesInFinalScripts

-------------------------------------------------------
####################################################### 
######				History 				###########
####################################################### 
-------------------------------------------------------
19.05.21
merged to main-branch

08.04.21
add initial DI and Configuration
 
06.04.21 
start to port it to .net5. Review architecture and project structure
