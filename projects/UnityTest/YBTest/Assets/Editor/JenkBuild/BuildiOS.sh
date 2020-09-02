#!/bin/sh

#-----------Config-----------#
UNITY_PATH=/Applications/Unity/Unity.app/Contents/MacOS/Unity

PROJECT_PATH=/Users/user/Documents/res_ios/CFDemoProject/

CODE_SIGN_IDENTITY="iPhone Distribution: XXX Company Limited"

PROVISIONING_PROFILE_NAME="YBUniversal"

#-----------Config-----------#

echo "将Unity导出成Xcode工程"

echo "ProjectPath:"/$PROJECT_PATH

$UNITY_PATH -projectPath $PROJECT_PATH -executeMethod JenkinsBuild.BuildIOS -logFile /dev/stdout -quit -batchmode

echo "Xcode工程生成完毕"

cd ${PROJECT_PATH}

cd IOS/dnasset/

xcodebuild clean 

xcodebuild archive -project Unity-iPhone.xcodeproj -scheme Unity-iPhone -archivePath Unity-iPhone.xcarchive CODE_SIGN_IDENTITY="$CODE_SIGN_IDENTITY"

xcodebuild -exportArchive -archivePath Unity-iPhone.xcarchive -exportPath $dnasset.ipa -exportFormat ipa CODE_SIGN_IDENTITY=$CODE_SIGN_IDENTITY -exportProvisioningProfile "$PROVISIONING_PROFILE_NAME"

echo "ipa完毕"

open .