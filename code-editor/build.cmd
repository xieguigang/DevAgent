@echo off

npx esbuild src/main.ts --bundle --minify --outfile=dist/editor.bundle.js --format=iife
