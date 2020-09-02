@echo off

::-----------Config-----------::
set UNITY_PATH=D:\install\Unity\Editor\Unity.exe
set PROJECT_PATH=E:\res\DemoProject
::-----------Config-----------::


echo ******** Unity to Win32 ********

echo ProjectPath:%PROJECT_PATH%

"%UNITY_PATH%" -projectPath "%PROJECT_PATH%" -executeMethod JenkinsBuild.BuildWin32 -quit -batchmode

echo *********** Build finish *********

pause