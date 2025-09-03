# CMakeList.txt : CMake project for BrainMirrorSDK, include source and define
# project specific logic here.
#
cmake_minimum_required (VERSION 3.8)

# Enable Hot Reload for MSVC compilers if supported.
if (POLICY CMP0141)
    cmake_policy(SET CMP0141 NEW)
    set(CMAKE_MSVC_DEBUG_INFORMATION_FORMAT "$<IF:$<AND:$<C_COMPILER_ID:MSVC>,$<CXX_COMPILER_ID:MSVC>>,$<$<CONFIG:Debug,RelWithDebInfo>:EditAndContinue>,$<$<CONFIG:Debug,RelWithDebInfo>:ProgramDatabase>>")
endif()

project ("BrainMirrorSDK")

# 包含头文件目录
include_directories(${CMAKE_SOURCE_DIR}/lib)
include_directories(${CMAKE_SOURCE_DIR}/demo)

# Add source to this project's DLL.
add_library(BrainMirrorSDK SHARED
    "BrainMonitorWrapper.cpp"
    "BrainMonitorWrapper.h"
    "lib/ble_device.h"
    "lib/brnpro_if.h"
    "BrainMonitorSDK.def"
)

# 设置运行时库为动态链接
set_property(TARGET BrainMirrorSDK PROPERTY
    MSVC_RUNTIME_LIBRARY "MultiThreadedDLL$<$<CONFIG:Debug>:Debug>")

# 链接静态库和系统库
target_link_libraries(BrainMirrorSDK PRIVATE 
    ${CMAKE_CURRENT_SOURCE_DIR}/lib/jfsdklib.lib
    ws2_32
    winmm
    setupapi
    advapi32
    user32
    kernel32
)

if (CMAKE_VERSION VERSION_GREATER 3.12)
    set_property(TARGET BrainMirrorSDK PROPERTY CXX_STANDARD 20)
endif()

# 设置DLL导出
set_target_properties(BrainMirrorSDK PROPERTIES
    WINDOWS_EXPORT_ALL_SYMBOLS ON
)

# 添加预处理器定义
target_compile_definitions(BrainMirrorSDK PRIVATE BRAINMIRRORWRAPPER_EXPORTS)