LOCAL_PATH:= $(call my-dir)
#PROJECT_PATH:= $(call my-dir)/..
PROJECT_PATH:= $(call my-dir)/../../..

include $(CLEAR_VARS)

LOCAL_C_INCLUDES :=	$(PROJECT_PATH)/include/

#LOCAL_SRC_FILES:= $(PROJECT_PATH)/JNIHelper.cpp 

#traverse all the directory and subdirectory
define walk
  $(wildcard $(1)) $(foreach e, $(wildcard $(1)/*), $(call walk, $(e)))
endef

#find all the file recursively under jni/
ALLFILES = $(call walk, $(PROJECT_PATH)/src/YBehavior)
FILE_LIST := $(filter %.cpp, $(ALLFILES))

LOCAL_SRC_FILES := $(FILE_LIST)

#LOCAL_SRC_FILES := $(FILE_LIST:$(LOCAL_PATH)/%=%)

LOCAL_CPP_FEATURES += exceptions
LOCAL_CFLAGS := -DSHARP
LOCAL_MODULE:= YBehavior

include $(BUILD_SHARED_LIBRARY)

#$(call import-module,android/native_app_glue)
#$(call import-module,android/cpufeatures)
