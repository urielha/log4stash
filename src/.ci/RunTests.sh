#!/bin/sh

mono src/.nuget/NuGet.exe install NUnit.Runners -Version 2.6.3 -OutputDirectory src/packages

runTest(){
    mono src/packages/NUnit.Runners.2.6.3/tools/nunit-console.exe -noxml -nodots -labels $@
   if [ $? -ne 0 ]
   then   
     exit 1
   fi
}

runTest src/log4stash.IntegrationTests/bin/Debug/log4stash.IntegrationTests.dll -exclude=Performance
runTest src/log4stash.UnitTests/bin/Debug/log4stash.UnitTests.dll -exclude=Performance

exit $?
