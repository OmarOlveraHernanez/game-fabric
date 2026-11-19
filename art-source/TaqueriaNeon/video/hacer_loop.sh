#!/bin/bash
# uso: bash hacer_loop.sh original.mp4 puesto_vivo.mp4
set -e
IN=$1; OUT=$2; F=1.5
ffmpeg -v error -y -i "$IN" -vf "setpts=PTS/0.6,minterpolate=fps=30:mi_mode=mci:mc_mode=aobmc:vsbmc=1" -an -c:v libx264 -crf 14 -preset fast lento.mp4
D=$(ffprobe -v error -show_entries format=duration -of csv=p=0 lento.mp4)
OFF=$(python3 -c "print($D-2*$F)")
ffmpeg -v error -y -i lento.mp4 -i lento.mp4 -filter_complex "[0:v]trim=start=$F,setpts=PTS-STARTPTS[a];[1:v]trim=end=$F,setpts=PTS-STARTPTS[b];[a][b]xfade=transition=fade:duration=$F:offset=$OFF,format=yuv420p[v]" -map "[v]" -an -c:v libx264 -profile:v baseline -level 4.0 -pix_fmt yuv420p -r 30 -g 30 -bf 0 -crf 23 -preset slow -movflags +faststart "$OUT"
rm lento.mp4
