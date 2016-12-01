echo Doing DLL copy... >> ..\deployment.log
dir ..\wwwroot\MediatorLib
dir ..\..\wwwroot\MediatorLib
copy ..\wwwroot\MediatorLib\bin\Release\MediatorLib.dll ..\wwwroot\messages\bin\MediatorLib.dll
