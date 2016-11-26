rmdir /S /Y bin
%windir%\Microsoft.NET\Framework\v4.0.30319\msbuild.exe src\log4stash.sln /t:Clean,Rebuild /p:Configuration=Release /fileLogger

copy LICENSE bin
copy readme.txt bin

src\.nuget\NuGet.exe pack log4stash.nuspec -Basepath bin
