#!/bin/bash

#set -x
thisdir=$(pwd)
publishroot=$(realpath -s $thisdir/Releases)

publishRuntime() {
   outdir=$publishroot/$(basename $1)
   dotnet publish -o $outdir -p:PublishSingleFile=true --self-contained -r $2 --framework net8.0 --configuration Release
   
}
publish() {
   printf 'publish '$1'\n'
   cd $thisdir/$1
   publishRuntime $1 "linux-x64"
   publishRuntime $1 "win-x64"
   cd $thisdir
}

if [ ! -d "$DIRECTORY" ]; then
  mkdir "$publishroot"
fi

publish "PdfResizerSharp"

set +x
printf 'done\n'
sleep 5

