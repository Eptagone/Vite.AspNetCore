/**
 * Name: vite.config.ts
 * Description: Vite configuration file
 *
 * @see https://vitejs.dev/guide/backend-integration.html
 * @see https://tailwindcss.com/docs/guides/vite
 */

import { defineConfig } from "vite";
import tailwindcss from "tailwindcss";
import autoprefixer from "autoprefixer";

export default defineConfig({
    appType: 'custom',
    base: '/dist/',
    publicDir: false,
    root: 'Assets',
    build: {
        manifest: true,
        emptyOutDir: true,
        outDir: '../wwwroot/dist',
        rollupOptions: {
            input: [
                'Assets/styles/site.css',
                'Assets/app.ts'
            ],
        }
    },
    css: {
        postcss: {
            plugins: [
                tailwindcss(),
                autoprefixer(),
            ],
        },
    },
});
