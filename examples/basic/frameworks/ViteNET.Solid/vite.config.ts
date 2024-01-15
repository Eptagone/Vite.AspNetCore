/**
 * Name: vite.config.ts
 * Description: Vite configuration file
 */

import { UserConfig, defineConfig } from 'vite';
import solid from 'vite-plugin-solid';
import { spawn } from 'child_process';
import fs from 'fs';
import path from 'path';

// Get base folder for certificates.
const baseFolder =
	process.env.APPDATA !== undefined && process.env.APPDATA !== ''
		? `${process.env.APPDATA}/ASP.NET/https`
		: `${process.env.HOME}/.aspnet/https`;

// Generate the certificate name using the NPM package name
const certificateName = process.env.npm_package_name;

// Define certificate filepath
const certFilePath = path.join(baseFolder, `${certificateName}.pem`);
// Define key filepath
const keyFilePath = path.join(baseFolder, `${certificateName}.key`);

// Pattern for CSS files
const cssPattern = /\.css$/;
// Pattern for image files
const imagePattern = /\.(png|jpe?g|gif|svg|webp|avif)$/;

// Export Vite configuration
export default defineConfig(async () => {
	// Ensure the certificate and key exist
	if (!fs.existsSync(certFilePath) || !fs.existsSync(keyFilePath)) {
		// Wait for the certificate to be generated
		await new Promise<void>((resolve) => {
			spawn('dotnet', [
				'dev-certs',
				'https',
				'--export-path',
				certFilePath,
				'--format',
				'Pem',
				'--no-password',
			], { stdio: 'inherit', })
				.on('exit', (code) => {
					resolve();
					if (code) {
						process.exit(code);
					}
				});
		});
	};

	// Define Vite configuration
	const config: UserConfig = {
		appType: 'custom',
		root: 'Assets',
		publicDir: 'public',
		build: {
			manifest: true,
			emptyOutDir: true,
			outDir: '../wwwroot',
			assetsDir: '',
			rollupOptions: {
				input: 'Assets/main.tsx',
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
			https: {
				cert: certFilePath,
				key: keyFilePath
			}
		},
		plugins: [
			solid()
		],
		optimizeDeps: {
			include: []
		}
	}

	return config;
});