#!/bin/sh

mono .nuget/NuGet.exe install NUnit.Runners -Version 2.6.3 -OutputDirectory packages

runTest(){
    mono packages/NUnit.Runners.2.6.3/tools/nunit-console.exe -noxml -nodots -labels $@
   if [ $? -ne 0 ]
   then   
     exit 1
   fi
}

runTest log4stash.Tests/bin/Debug/log4stash.Tests.dll -exclude=Performance

exit $?
