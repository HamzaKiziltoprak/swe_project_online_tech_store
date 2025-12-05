import { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import './styles/App.css';
import { Routes, Route, Link } from 'react-router-dom';
import Products from './pages/Products';
import LoginPage from './pages/login';
import RegisterPage from './pages/register';

function Home() {
    return (
        <div>
            <h2>Home Page</h2>
            <p>Welcome to the home page!</p>
        </div>
    );
}

function About() {
    return (
        <div>
            <h2>About Page</h2>
            <p>Learn more about us!</p>
        </div>
    );
}

function Contact() {
    return (
        <div>
            <h2>Contact Page</h2>
            <p>Get in touch with us!</p>
        </div>
    );
}

function App() {
    const { t, i18n } = useTranslation();
    const [theme, setTheme] = useState(() => {
        // Initialize theme from localStorage or default to 'light'
        const savedTheme = localStorage.getItem('theme');
        return savedTheme || 'light';
    });

    useEffect(() => {
        // On theme change, update the class on the body and save to localStorage
        document.body.className = '';
        document.body.classList.add(theme);
        localStorage.setItem('theme', theme);
    }, [theme]);

    const toggleTheme = () => {
        setTheme(prevTheme => prevTheme === 'light' ? 'dark' : 'light');
    };

    const toggleLanguage = () => {
        const newLang = i18n.language === 'tr' ? 'en' : 'tr';
        i18n.changeLanguage(newLang);
    };

    return (
        <div className="app-container">
            <header className="app-header">
            <h1 className="logo"><Link to="/">{t('Online Tech Store')}</Link></h1>
                <nav>
                    <ul className='nav-links'>
                        <li><Link to="/">{t('Home')}</Link></li>
                        <li><Link to="/products">{t('Products')}</Link></li>
                        <li><Link to="/about">{t('About')}</Link></li>
                        <li><Link to="/contact">{t('Contact')}</Link></li>
                        <li><Link to="/login">{t('Sign In')}</Link></li>
                    </ul>
                </nav>
                <button className="theme-toggle-button" onClick={toggleTheme}>
                {theme === 'light' ? '🌙' : '☀️'}
                </button>
                <button className='language-toggle-button' onClick={toggleLanguage}>
                    {t('toggle_language')}
                </button>
            </header>
            <main>
                <Routes>
                    <Route path="/" element={<Home />} />
                    <Route path="/products" element={<Products />} />
                    <Route path="/about" element={<About />} />
                    <Route path="/contact" element={<Contact />} />
                    <Route path="/login" element={<LoginPage />} />
                    <Route path="/register" element={<RegisterPage />} />
                </Routes>
            </main>
        </div>
    );
}

export default App;