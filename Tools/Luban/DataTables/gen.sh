#!/bin/bash

WORKSPACE=..
LUBAN_DLL=$WORKSPACE/Luban/Luban.dll
CONF_ROOT=.

dotnet $LUBAN_DLL \
    -t all \
    -c cs-newtonsoft-json \
    -d json \
    --conf $CONF_ROOT/luban.conf \
    -x outputDataDir=output_data \
    -x outputCodeDir=output_code