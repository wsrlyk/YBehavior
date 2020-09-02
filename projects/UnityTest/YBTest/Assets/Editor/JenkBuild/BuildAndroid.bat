@echo off

::-----------Config-----------::
set UNITY_PATH=D:\install\Unity\Editor\Unity.exe
set PROJECT_PATH=E:\res\DemoProject
::-----------Config-----------::


echo ****** Unity to apk *******

echo %PROJECT_PATH%

"%UNITY_PATH%" -projectPath "%PROJECT_PATH%" -executeMethod JenkinsBuild.BuildAndroid -quit -batchmode

echo ****** build finish ********

pause