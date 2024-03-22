/**
 * Name: main.ts
 * Description: This is the main entry point for the app.
 */

import 'vite/modulepreload-polyfill';
/* @refresh reload */
import { render } from 'solid-js/web'

import './index.css'
import App from './App'

const root = document.getElementById('app')

render(() => <App />, root!)
