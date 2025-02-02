/**
 * Name: vite.config.ts
 * Description: Vite configuration file
 */

import { defineConfig } from 'vite';

// Pattern for CSS files
const cssPattern = /\.css$/;
// Pattern for image files
const imagePattern = /\.(png|jpe?g|gif|svg|webp|avif)$/;

// Define Vite configuration
export default defineConfig({
    appType: 'custom',
    publicDir: false,
    build: {
        manifest: true,
        emptyOutDir: true,
        outDir: '../wwwroot',
        assetsDir: '',
        rollupOptions: {
            input: 'src/main.ts',
            output: {
                // Save entry files to the appropriate folder
                entryFileNames: 'js/[name].[hash].js',
                // Save chunk files to the js folder
                chunkFileNames: 'js/[name]-chunk.js',
                // Save asset files to the appropriate folder
                assetFileNames: (info) => {
                    if (info.name) {
                        // If the file is a CSS file, save it to the css folder
                        if (cssPattern.test(info.name)) {
                            return 'css/[name][extname]';
                        }
                        // If the file is an image file, save it to the images folder
                        if (imagePattern.test(info.name)) {
                            return 'images/[name][extname]';
                        }

                        // If the file is any other type of file, save it to the assets folder
                        return 'assets/[name][extname]';
                    } else {
                        // If the file name is not specified, save it to the output directory
                        return '[name][extname]';
                    }
                },
            }
        },
    },
    server: {
        strictPort: true,
    },
    css: {
        preprocessorOptions: {
            scss: {
                api: "modern-compiler",
            },
        },
    },
});
