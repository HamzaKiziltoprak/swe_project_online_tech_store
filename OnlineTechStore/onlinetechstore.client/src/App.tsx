import { useState, useEffect } from 'react';
import './App.css';

function App() {
    const [theme, setTheme] = useState('light');

    useEffect(() => {
        // On theme change, update the class on the body
        document.body.className = '';
        document.body.classList.add(theme);
    }, [theme]);

    const toggleTheme = () => {
        setTheme(prevTheme => prevTheme === 'light' ? 'dark' : 'light');
    };

    return (
        <div className="app-container">
            <header className="app-header">
                <h1 className='logo'>Online Tech Store</h1>
                <nav>
                    <ul className='nav-links'>
                        <li><a href="#home">Home</a></li>
                        <li><a href="#products">Products</a></li>
                        <li><a href="#about">About</a></li>
                        <li><a href="#contact">Contact</a></li>
                        <li><a href="#login">Sign In</a></li>
                    </ul>
                </nav>
                <button className="theme-toggle-button" onClick={toggleTheme}>
                {theme === 'light' ? '🌙' : '☀️'}
                </button>
            </header>
            <main>
                
            </main>
        </div>
    );
}

export default App;