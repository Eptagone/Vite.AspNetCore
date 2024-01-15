/**
 * Name: main.ts
 * Description: This is the main entry point for the app.
 */

import 'vite/modulepreload-polyfill';
import { createRoot } from "react-dom/client";
import './index.css';
import App from "./App";

const appElement = document.querySelector<HTMLDivElement>('#app')!;


const root = createRoot(appElement);
root.render(<App />);
