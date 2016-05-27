rmdir /S bin
%windir%\Microsoft.NET\Framework\v4.0.30319\msbuild.exe src\log4stash.sln /t:Clean,Rebuild /p:Configuration=Release /fileLogger
%~dp0src\packages\NUnit.Runners.2.6.3\tools\nunit-console.exe -noxml -nodots -labels %~dp0src\log4stash.Tests\bin\Release\log4stash.Tests.dll

copy LICENSE bin
copy readme.txt bin

src\.nuget\NuGet.exe pack log4stash.nuspec -Basepath bin
