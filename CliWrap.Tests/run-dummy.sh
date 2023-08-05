#!/usr/bin/env bash

BASEDIR=$(dirname "$0")
dotnet "$BASEDIR/CliWrap.Tests.Dummy.dll" "$@"