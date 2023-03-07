cmake_minimum_required(VERSION 2.8.12)

option(WINDOWS_BUILD "Build on windows")
option(LINUX_BUILD "Build on linux")
option(DEBUG "Debug mode")

set(APP_NAME id3BiosealSample)
project(${APP_NAME})

# Macros
macro(postBuildCopyDirectory target dst_dir)
    set(dirs_list ${ARGN})
    foreach (src_dir ${dirs_list})
        add_custom_command(
                TARGET ${target} POST_BUILD
                COMMAND ${CMAKE_COMMAND} -E copy_directory
                ${src_dir}
                ${dst_dir})
    endforeach ()
endmacro()
# End macros

set(ID3BIOSEAL_SDK_ROOT_DIR ${CMAKE_CURRENT_LIST_DIR}/../sdk/)

include_directories(${ID3BIOSEAL_SDK_ROOT_DIR}include)

if (LINUX_BUILD)
    set(LINK_DIR ${ID3BIOSEAL_SDK_ROOT_DIR}bin/linux/x64)
elseif (WINDOWS_BUILD)
    set(LINK_DIR ${ID3BIOSEAL_SDK_ROOT_DIR}bin/windows/x64)
    add_compile_definitions("_CRT_SECURE_NO_WARNINGS")
endif ()
if (DEBUG)
    add_compile_definitions("DEBUG")
endif()
# If building for another target (Linux x64 GPU, Raspberry Pi 4, etc) add an option here and fill in the correct path to the binaries

link_directories(${LINK_DIR})

add_executable(${APP_NAME} id3BiosealSample.cpp)
target_link_libraries(${APP_NAME} id3Bioseal)
if (LINUX_BUILD)
    target_link_libraries(${APP_NAME} stdc++fs)
endif()
set(TO_COPY ${LINK_DIR})
postBuildCopyDirectory(${APP_NAME} $<TARGET_FILE_DIR:${APP_NAME}> ${TO_COPY})
