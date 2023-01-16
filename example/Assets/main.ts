/**
 * Name: main.ts
 * Description: This is the main entry point for the app.
 */

import 'vite/modulepreload-polyfill';
import { setupCounter } from './counter';
import './style.scss';

setupCounter(document.querySelector<HTMLButtonElement>('#counter')!);
