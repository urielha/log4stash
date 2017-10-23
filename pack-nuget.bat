rmdir /S /Y bin
%windir%\Microsoft.NET\Framework\v4.0.30319\msbuild.exe src\log4stash.sln /t:Clean,Rebuild /p:Configuration=Release /fileLogger

cd %~dp0src\log4stash.Tests\bin\Release
%~dp0src\packages\NUnit.ConsoleRunner.3.7.0\tools\nunit3-console.exe --noresult --labels=On log4stash.Tests.dll

cd %~dp0

copy LICENSE bin
copy readme.txt bin

src\.nuget\NuGet.exe pack log4stash.nuspec -Basepath bin
