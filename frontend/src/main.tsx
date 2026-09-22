import React from 'react';
import ReactDOM from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import { CssBaseline, ThemeProvider, createTheme } from '@mui/material';
import App from './App';
import './styles.css';
const theme = createTheme({ palette: { primary: { main: '#126b69' }, background: { default: '#f3f6f7' } }, typography: { fontFamily: 'Inter, Segoe UI, sans-serif' }, shape: { borderRadius: 12 } });
ReactDOM.createRoot(document.getElementById('root')!).render(<React.StrictMode><ThemeProvider theme={theme}><CssBaseline/><BrowserRouter><App/></BrowserRouter></ThemeProvider></React.StrictMode>);
