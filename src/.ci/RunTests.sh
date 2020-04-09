#!/bin/sh


runTest(){
    mono src/packages/NUnit.ConsoleRunner.3.7.0/tools/nunit3-console.exe $@
   if [ $? -ne 0 ]
   then   
     exit 1
   fi
}

runTest src/log4stash.IntegrationTests/bin/Debug/log4stash.IntegrationTests.dll
runTest src/log4stash.UnitTests/bin/Debug/log4stash.UnitTests.dll

exit $?
